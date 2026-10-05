namespace UPZMG.Persistence.Models;

/// <summary>
/// A linkage project: the agreement with an organisation and the student who serves it.
///
/// The 911 and SII formats ask for these counts by sector and by activity, so they are stored
/// one row per project instead of as a total: a total cannot be broken down again, and the
/// figure the university reports has to be traceable to the projects behind it.
/// </summary>
public class OutreachProject
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; } //fk

    public required string OrganizationName { get; set; }
    public required string Sector { get; set; }          // Público, Privado, Social
    public required string CompanySize { get; set; }     // Micro, Pequeña, Mediana, Grande
    public required string ActivityType { get; set; }    // Servicio social, Prácticas, Proyecto
    public required string ProjectName { get; set; }

    /// <summary>Set when the enrolment number matched a registered student.</summary>
    public Guid? StudentId { get; set; } //fk
    public string? StudentEnrollmentNumber { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int AccreditedHours { get; set; }

    public Guid CreatedBy { get; set; } //fk to systemUser
    public DateTime DateCreated { get; set; }
}
