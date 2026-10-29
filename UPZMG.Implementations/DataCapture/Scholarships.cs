using System.ComponentModel.DataAnnotations;
using UPZMG.Shared.Enums;

namespace UPZMG.Implementations.DataCapture;

// ── Scholarships ────────────────────────────────────────────────────────────

public sealed class CreateScholarship
{
    public required Guid RequesterKey { get; set; }

    [Required(ErrorMessage = "Indica la matrícula del alumno."), MaxLength(20)]
    public required string StudentEnrollmentNumber { get; set; }

    public required Guid ScolarshipProgramId { get; set; }
    public required Guid PeriodId { get; set; }

    public FinantialSource FinantialSource { get; set; }

    // Typed as decimal: with integer bounds the check converted the amount to Int32 and an amount
    // above two thousand million ended in a server error instead of this message.
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "El monto debe estar entre 0 y 999,999,999.")]
    public decimal Amount { get; set; }

    [MaxLength(100)]
    public string? ScolarshipObjective { get; set; }

    public DateOnly? AssignedDate { get; set; }
}

public sealed record ScholarshipListItem(
    Guid Id,
    string StudentName,
    string StudentEnrollmentNumber,
    string Career,
    string Program,
    string Source,
    decimal Amount,
    string Period,
    string? Objective,
    DateOnly? AssignedDate);

/// <summary>Grants of the period grouped by where the money comes from; feeds the chart.</summary>
/// <param name="Students">Students with a grant from each source; a student with two sources is in both.</param>
/// <param name="Scope">Which period the figures belong to, or that they are the whole history.</param>
/// <param name="Beneficiaries">
/// Students with at least one grant in the scope, counted once. Adding the per-source numbers up
/// counted a student with two sources twice, which read as more beneficiaries than there are.
/// </param>
public sealed record ScholarshipsBySource(
    IReadOnlyList<string> Sources,
    IReadOnlyList<int> Students,
    IReadOnlyList<decimal> Amounts,
    string Scope,
    int Beneficiaries)
{
    /// <summary>Grants awarded, which is what the per-source numbers add up to.</summary>
    public int Grants => Students.Sum();

    public decimal TotalAmount => Amounts.Sum();
}

// ── Titulation ──────────────────────────────────────────────────────────────

public sealed class CreateTitulation
{
    public required Guid RequesterKey { get; set; }

    [Required(ErrorMessage = "Indica la matrícula del alumno."), MaxLength(20)]
    public required string StudentEnrollmentNumber { get; set; }

    public required Guid TitulationAssessorId { get; set; }

    public TitulationModality Modality { get; set; }

    public DateOnly CreditsEgressDate { get; set; }
    public DateOnly CertificateTitulationDate { get; set; }
    public DateOnly TitleExpeditionDate { get; set; }

    public bool HasProfessionalLicence { get; set; }

    [MaxLength(20)]
    public string? ProfessionalLicenceNumber { get; set; }
}

public sealed record TitulationListItem(
    Guid Id,
    string StudentName,
    string StudentEnrollmentNumber,
    string Career,
    string Modality,
    string Assessor,
    DateOnly CreditsEgressDate,
    DateOnly TitleExpeditionDate,
    bool HasProfessionalLicence,
    string? ProfessionalLicenceNumber);

/// <summary>
/// Titulation as the SII reads it: graduates of a cohort against those of them who already
/// hold a title. It is a rate over people who finished, never a count typed by hand.
/// </summary>
public sealed record TitulationSummary(
    int Graduates,
    int Titled,
    IReadOnlyList<TitulationByModality> ByModality)
{
    public double TitulationRate => Graduates == 0 ? 0 : Math.Round(100.0 * Titled / Graduates, 2);
}

public sealed record TitulationByModality(string Modality, int Count);
