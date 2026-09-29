using UPZMG.Shared.Enums;

using System.ComponentModel.DataAnnotations;

namespace UPZMG.Persistence.Models;

/// <summary>
/// Academic module.
/// Represents student's titulation process in the academic system.
/// </summary>

public class TitulationProcess
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; } //fk
    public Guid CareerId { get; set; } //fk
    public Guid TitulationAssessorId { get; set; } //fk
    public DateOnly CreditsEgressDate { get; set; }
    public DateOnly CertificateTitulationDate { get; set; }
    public DateOnly TitleExpeditionDate { get; set; }
    public bool HasProfessionalLicence { get; set; }
    /// <summary>How the student graduated. It was named ProfessionalLicenceNumber while the
    /// table was empty, but its type always said modality; the name now matches what it holds.</summary>
    public TitulationModality Modality { get; set; }

    /// <summary>Cédula profesional, when it has already been issued.</summary>
    [MaxLength(20)]
    public string? ProfessionalLicenceNumber { get; set; }
}