using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.BoardReport;
using UPZMG.Implementations.Reports;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// Reads everything the board report consolidates and stores the only part the university
/// writes by hand: the narrative blocks.
///
/// The narrative lives in the same `reports` table as the official captures, as a row of type
/// <see cref="NarrativeReportType"/> whose version is the cycle. That keeps one place for
/// "what an area wrote for a period", including the rule that a submitted document is frozen.
/// </summary>
public class BoardReportRepository(
    AppDBContext db,
    ReportTemplatesRepository reportTemplatesRepository,
    DashboardRepository dashboardRepository,
    IntelligenceRepository intelligenceRepository,
    ScholarshipsRepository scholarshipsRepository)
{
    public const string NarrativeReportType = "InformeJunta";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // The frozen document is stored and read back with the same options the API answers with.
    // Named floating-point literals are allowed so a rate that came out as NaN is stored as it
    // is instead of making the submission fail.
    private static readonly JsonSerializerOptions SnapshotOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    /// <summary>One entry per cycle the official formats exist for, newest first.</summary>
    public async Task<List<BoardReportListItem>> GetCyclesAsync()
    {
        var templates = await reportTemplatesRepository.GetTemplatesAsync();
        var cycles = templates.Select(t => t.Version).Distinct().OrderByDescending(v => v).ToList();
        if (cycles.Count == 0) return [];

        var captures = await db.Reports
            .AsNoTracking()
            .Where(r => r.ReportType != NarrativeReportType && cycles.Contains(r.TemplateVersion!))
            .Select(r => new { r.ReportType, r.TemplateVersion, r.Status, r.LastModified })
            .ToListAsync();

        var documents = await db.Reports
            .AsNoTracking()
            .Where(r => r.ReportType == NarrativeReportType)
            .Select(r => new { r.TemplateVersion, r.Status, r.LastModified, r.AnswersJson })
            .ToListAsync();

        var items = new List<BoardReportListItem>();
        foreach (var cycle in cycles)
        {
            var expected = templates.Count(t => t.Version == cycle);
            var sent = captures.Count(c => c.TemplateVersion == cycle && c.Status == nameof(ReportStatus.submitted));
            var document = documents.FirstOrDefault(d => d.TemplateVersion == cycle);

            var written = document is null ? 0 : ReadNarratives(document.AnswersJson).Count;

            items.Add(new BoardReportListItem(
                cycle,
                document?.Status ?? BoardReportStatus.Draft,
                document?.LastModified,
                sent,
                expected,
                PendingNarratives: Math.Max(0, NarrativeBlockCount - written)));
        }

        return items;
    }

    /// <summary>
    /// The full document for a cycle; null when no official format exists for it.
    ///
    /// A submitted report is returned exactly as it was submitted. Everything it consolidates —
    /// enrolment, the risk model, becas, finanzas — goes on changing after the Junta receives
    /// it, so rebuilding it from today's data would quietly rewrite a document that was already
    /// sent. A draft is always rebuilt, which is the point of a draft.
    /// </summary>
    public async Task<BoardReportDocument?> GetDocumentAsync(string cycle)
    {
        var frozen = await db.Reports
            .AsNoTracking()
            .Where(r => r.ReportType == NarrativeReportType
                && r.TemplateVersion == cycle
                && r.Status == nameof(ReportStatus.submitted))
            .Select(r => r.SnapshotJson)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(frozen) && ReadSnapshot(frozen) is { } stored) return stored;

        var inputs = await ReadInputsAsync(cycle);
        return inputs is null ? null : BoardReportBuilder.Build(inputs, DateTime.UtcNow);
    }

    /// <summary>
    /// Reads everything the document is built from. <paramref name="asSubmittedAtUtc"/> builds it
    /// as the submitted version, which is what gets frozen; otherwise the stored state decides.
    /// </summary>
    private async Task<BoardReportInputs?> ReadInputsAsync(string cycle, DateTime? asSubmittedAtUtc = null)
    {
        var templates = await reportTemplatesRepository.GetTemplatesAsync();
        var expected = templates.Where(t => t.Version == cycle).ToList();
        if (expected.Count == 0) return null;

        var rows = await db.Reports
            .AsNoTracking()
            .Where(r => r.TemplateVersion == cycle)
            .Select(r => new { r.Id, r.ReportType, r.Status, r.LastModified, r.AnswersJson, r.CreatedBy })
            .ToListAsync();

        var authors = await AuthorsAsync(rows.Select(r => r.CreatedBy));

        var captures = new List<BoardReportCapture>();
        foreach (var item in expected.OrderBy(t => t.ReportType))
        {
            var template = await reportTemplatesRepository.GetTemplateAsync(item.ReportType, cycle);
            if (template is null) continue;

            // Each area captures its own formats, so there can be several rows of one format.
            // Taking whichever came back first made the consolidated figures depend on the order
            // the database happened to return: the sent capture is the official one, and among
            // equals the most recently touched.
            var matching = rows
                .Where(r => r.ReportType == item.ReportType)
                .OrderByDescending(r => r.Status == nameof(ReportStatus.submitted))
                .ThenByDescending(r => r.LastModified)
                .ThenBy(r => r.Id)
                .ToList();

            var row = matching.FirstOrDefault();
            var answers = row is null ? [] : ReadNarratives(row.AnswersJson);

            captures.Add(new BoardReportCapture(
                item.ReportType,
                item.DisplayName,
                row?.Status ?? "pending",
                row?.LastModified ?? default,
                row?.CreatedBy is { } id ? authors.GetValueOrDefault(id) : null,
                new ReportFormState(template, answers.ToDictionary(a => a.Key, a => (string?)a.Value)),
                OtherCaptures: Math.Max(0, matching.Count - 1)));
        }

        var document = rows.FirstOrDefault(r => r.ReportType == NarrativeReportType);
        var narratives = document is null ? [] : ReadNarratives(document.AnswersJson);

        var submittedAt = asSubmittedAtUtc
            ?? (document?.Status == nameof(ReportStatus.submitted) ? document.LastModified : null);

        return new BoardReportInputs(
            Cycle: cycle,
            Dashboard: await dashboardRepository.GetSummaryAsync(),
            History: await intelligenceRepository.GetHistoryAsync(),
            Forecast: await intelligenceRepository.GetForecastAsync(2),
            Dropout: await intelligenceRepository.GetDropoutReportAsync(),
            Anomalies: await intelligenceRepository.GetAnomaliesAsync(),
            Staff: await StaffAsync(),
            Facilities: await FacilitiesAsync(),
            Outreach: await OutreachAsync(),
            Scholarships: await scholarshipsRepository.GetScholarshipsBySourceAsync(),
            Titulation: await scholarshipsRepository.GetTitulationSummaryAsync(),
            Finance: await FinanceAsync(),
            Captures: captures,
            Narratives: narratives,
            Status: submittedAt is null ? BoardReportStatus.Draft : BoardReportStatus.Submitted,
            SubmittedAtUtc: submittedAt);
    }

    /// <summary>Stores (or clears) the text of one narrative block.</summary>
    public async Task<BoardReportSaveResult> SaveNarrativeAsync(SaveBoardNarrative request, Guid userId)
    {
        var templates = await reportTemplatesRepository.GetTemplatesAsync();
        if (!templates.Any(t => t.Version == request.Cycle))
            return BoardReportSaveResult.Refused("No hay formatos oficiales de ese ciclo.");

        var row = await db.Reports
            .FirstOrDefaultAsync(r => r.ReportType == NarrativeReportType && r.TemplateVersion == request.Cycle);

        if (row is { Status: nameof(ReportStatus.submitted) })
            return BoardReportSaveResult.Refused("El informe ya se envió; para editarlo hay que generar uno nuevo.");

        // Any key used to be stored, and a block the document does not have was kept forever.
        if (await ReadInputsAsync(request.Cycle) is not { } inputs
            || !BoardReportBuilder.Build(inputs, DateTime.UtcNow).Chapters
                .SelectMany(chapter => chapter.Blocks)
                .Any(block => block.Kind == BoardReportBlockKinds.Narrative && block.BlockKey == request.BlockKey))
            return BoardReportSaveResult.Refused("Ese bloque no existe en el informe de este ciclo.");

        var narratives = row is null ? [] : ReadNarratives(row.AnswersJson);

        if (string.IsNullOrWhiteSpace(request.Text)) narratives.Remove(request.BlockKey);
        else narratives[request.BlockKey] = request.Text.Trim();

        var now = DateTime.UtcNow;
        if (row is null)
        {
            db.Reports.Add(new Models.Reports
            {
                Id = Guid.NewGuid(),
                CreatedBy = userId,
                ReportType = NarrativeReportType,
                TemplateVersion = request.Cycle,
                Status = nameof(ReportStatus.draft),
                AnswersJson = JsonSerializer.Serialize(narratives),
                DateCreated = now,
                LastModified = now,
            });
        }
        else
        {
            row.AnswersJson = JsonSerializer.Serialize(narratives);
            row.LastModified = now;
        }

        try
        {
            await db.SaveGuardingUniquenessAsync("Otro usuario está redactando el informe de este ciclo. Vuelve a abrirlo.");
        }
        catch (InvalidOperationException ex)
        {
            return BoardReportSaveResult.Refused(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return BoardReportSaveResult.Refused("El informe cambió mientras se guardaba. Vuelve a abrirlo.");
        }

        return BoardReportSaveResult.Ok();
    }

    /// <summary>
    /// Freezes the document: the narrative can no longer be edited, and the figures it was
    /// submitted with are stored with it so a later reading shows the document that was sent.
    /// </summary>
    public async Task<BoardReportSaveResult> SubmitAsync(SubmitBoardReport request, Guid userId)
    {
        var row = await db.Reports
            .FirstOrDefaultAsync(r => r.ReportType == NarrativeReportType && r.TemplateVersion == request.Cycle);

        if (row is { Status: nameof(ReportStatus.submitted) })
            return BoardReportSaveResult.Refused("El informe ya se había enviado.");

        var now = DateTime.UtcNow;
        var inputs = await ReadInputsAsync(request.Cycle, asSubmittedAtUtc: now);
        if (inputs is null) return BoardReportSaveResult.Refused("No hay formatos oficiales de ese ciclo.");

        var document = BoardReportBuilder.Build(inputs, now);
        if (document.Missing.Count > 0)
        {
            var names = string.Join(", ", document.Missing.Select(m => m.DisplayName));
            return BoardReportSaveResult.Refused($"Faltan formatos por enviar: {names}.");
        }

        var snapshot = JsonSerializer.Serialize(document, SnapshotOptions);

        if (row is null)
        {
            db.Reports.Add(new Models.Reports
            {
                Id = Guid.NewGuid(),
                CreatedBy = userId,
                ReportType = NarrativeReportType,
                TemplateVersion = request.Cycle,
                Status = nameof(ReportStatus.submitted),
                AnswersJson = JsonSerializer.Serialize(inputs.Narratives),
                SnapshotJson = snapshot,
                DateCreated = now,
                LastModified = now,
            });
        }
        else
        {
            row.Status = nameof(ReportStatus.submitted);
            row.SnapshotJson = snapshot;
            row.LastModified = now;
        }

        try
        {
            // One document per cycle, enforced by a filtered unique index: two areas pressing
            // send at the same time used to create two.
            await db.SaveGuardingUniquenessAsync("Otro usuario envió el informe de este ciclo al mismo tiempo.");
        }
        catch (InvalidOperationException ex)
        {
            return BoardReportSaveResult.Refused(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return BoardReportSaveResult.Refused("El informe cambió mientras se enviaba. Vuelve a abrirlo.");
        }

        return BoardReportSaveResult.Ok();
    }

    // ── Reading the rest of the institution ─────────────────────────────────

    private async Task<List<BoardStaffMember>> StaffAsync() =>
        await db.Employees
            .AsNoTracking()
            .Select(e => new BoardStaffMember(e.Gender.ToString(), (int)e.HighestEducationLevel, e.IsTeacher))
            .ToListAsync();

    /// <summary>Projects the outreach area registered, whatever period they belong to.</summary>
    private async Task<List<BoardOutreachProject>> OutreachAsync() =>
        await db.OutreachProjects
            .AsNoTracking()
            .Select(p => new BoardOutreachProject(p.Sector, p.ActivityType, p.OrganizationName,
                p.AccreditedHours, p.StudentId != null))
            .ToListAsync();

    /// <summary>
    /// The most recent fiscal year the administration captured, never one still in the future:
    /// a year that has not happened cannot be the exercise the Junta is being informed about.
    /// </summary>
    private async Task<BoardFinanceSummary?> FinanceAsync()
    {
        var currentYear = DateTime.UtcNow.Year;

        return await db.AnnualFinances
            .AsNoTracking()
            .Where(f => f.FiscalYear <= currentYear)
            .OrderByDescending(f => f.FiscalYear)
            .Select(f => new BoardFinanceSummary(f.FiscalYear, f.FederalIncome, f.StateIncome,
                f.OwnIncomeEnrollment, f.OwnIncomeServices, f.PayrollExpense, f.MaterialsExpense,
                f.ServicesExpense, f.InvestmentExpense))
            .FirstOrDefaultAsync();
    }

    private async Task<List<BoardFacility>> FacilitiesAsync() =>
        await db.Set<SchoolFacility>()
            .AsNoTracking()
            .Select(f => new BoardFacility(f.Name, f.Type.ToString(), f.Capacity, f.InUse))
            .ToListAsync();

    private async Task<Dictionary<Guid, string>> AuthorsAsync(IEnumerable<Guid?> ids)
    {
        var owners = ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (owners.Count == 0) return [];

        return await db.SystemUsers
            .AsNoTracking()
            .Where(u => owners.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? string.Empty);
    }

    /// <summary>
    /// The frozen document, or null when it cannot be read. A snapshot written by an older
    /// version is better reported as unreadable than silently replaced by today's figures, so
    /// the caller falls back to rebuilding only when there is nothing stored at all.
    /// </summary>
    private static BoardReportDocument? ReadSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<BoardReportDocument>(json, SnapshotOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, string> ReadNarratives(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    // The builder writes this many narrative blocks; the listing reports how many are still suggested.
    private const int NarrativeBlockCount = 10;
}
