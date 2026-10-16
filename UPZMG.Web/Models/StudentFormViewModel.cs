using UPZMG.Shared.Enums;

namespace UPZMG.Web.Models;

/// <summary>
/// Form state for capturing or correcting a student. It mirrors CreateStudent, with the
/// nullable dates and strings a form needs before it is validated.
/// </summary>
public class StudentFormViewModel
{
    public string EnrollmentNumber { get; set; } = string.Empty;
    public string Curp { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public Gender Gender { get; set; }
    public Guid CareerId { get; set; }
    public Guid PeriodIdStart { get; set; }
    public InscriptionType InscriptionType { get; set; } = InscriptionType.NuevoIngreso;
    public StudentStatus Status { get; set; } = StudentStatus.Activo;
    public IReadOnlyCollection<string> IndigenousLanguages { get; set; } = [];
    public EnglishLevel EnglishLevel { get; set; }
    public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.Soltero;
    public int NumberOfChildren { get; set; } = 0;
    public bool Works { get; set; }
    public string BirthCountry { get; set; } = string.Empty;
    public string BirthState { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
