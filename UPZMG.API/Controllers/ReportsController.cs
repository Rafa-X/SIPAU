using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Models;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Reports;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("reports")]
[Authorize(Roles = AreaRoles)]
public class ReportsController(
    ReportTemplatesRepository reportTemplatesRepository,
    ReportsRepository reportsRepository) : ControllerBase
{
    // Every institutional area captures the official formats it is responsible for.
    private const string AreaRoles = "Admin,Control Escolar,Servicios Estudiantiles,Recursos Humanos,Direccion Academica,Recursos Materiales y Servicios Generales";

    // Far above the largest official format; stops a runaway payload.
    private const int MaxAnswers = 20_000;

    // The owner always comes from the token, never from the request body.
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;

    [HttpGet("templates")]
    // Every template on disk, flagged when it cannot be captured
    public async Task<ActionResult<List<ReportTemplateItem>>> GetTemplates() =>
        Ok(await reportTemplatesRepository.GetTemplatesAsync());

    [HttpGet("templates/{reportType}/{version}")]
    // One template, with catalogue-driven rows and options filled in
    public async Task<ActionResult<ReportTemplateDefinition>> GetTemplate(string reportType, string version)
    {
        var template = await reportTemplatesRepository.GetTemplateAsync(reportType, version);
        return template is null ? NotFound() : Ok(template);
    }

    [HttpGet("templates/{reportType}/{version}/source")]
    [Authorize(Roles = "Admin")]
    // The template file as it was authored, for the template editor
    public async Task<ActionResult<ReportTemplateSource>> GetTemplateSource(string reportType, string version)
    {
        var source = await reportTemplatesRepository.GetSourceAsync(reportType, version);
        return source is null ? NotFound() : Ok(new ReportTemplateSource(reportType, version, source));
    }

    // Both template writes answer 200 even when they refuse: a template that does
    // not validate yet is part of authoring it, and the editor needs to read why.
    [HttpPost("templates/save")]
    [Authorize(Roles = "Admin")]
    // Create or edit a template; it is parsed and validated before it is written
    public async Task<ActionResult<TemplateSaveResult>> SaveTemplate([FromBody] SaveReportTemplate request) =>
        Ok(await reportTemplatesRepository.SaveTemplateAsync(request));

    [HttpPost("templates/delete")]
    [Authorize(Roles = "Admin")]
    // Remove a template that nobody has captured with
    public async Task<ActionResult<TemplateSaveResult>> DeleteTemplate([FromBody] DeleteReportTemplate request) =>
        Ok(await reportTemplatesRepository.DeleteTemplateAsync(request));

    [HttpGet("mine")]
    // Captures of the signed-in user, newest first
    public async Task<ActionResult<List<ReportListItem>>> GetMine() =>
        Ok(await reportsRepository.GetMineAsync(CurrentUserId));

    [HttpGet("{reportId:guid}")]
    // One capture of the signed-in user, with its answers
    public async Task<ActionResult<ReportDetail>> GetReport(Guid reportId)
    {
        var report = await reportsRepository.GetForUserAsync(reportId, CurrentUserId);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpPost("save")]
    // Save a draft or submit a capture; a submission is re-checked against its template
    public async Task<IActionResult> SaveReport([FromBody] SaveReport request)
    {
        var userId = CurrentUserId;
        if (userId == Guid.Empty) return Unauthorized();

        if (request.Status is not (ReportStatus.draft or ReportStatus.submitted))
            return BadRequest("Status must be draft or submitted.");

        if (request.Answers.Count > MaxAnswers)
            return BadRequest("Too many answers.");

        var template = await reportTemplatesRepository.GetTemplateAsync(request.ReportType, request.Version);
        if (template is null) return NotFound("Template not found.");

        var result = await reportsRepository.SaveAsync(request, userId, template);
        return result.Outcome switch
        {
            SaveReportOutcome.Created => Created($"/reports/{result.Report!.Id}", result.Report),
            SaveReportOutcome.Updated => Ok(result.Report),
            SaveReportOutcome.Locked => Conflict("The report was already submitted."),
            SaveReportOutcome.Incomplete => UnprocessableEntity(result.Problems),
            _ => NotFound(),
        };
    }
}
