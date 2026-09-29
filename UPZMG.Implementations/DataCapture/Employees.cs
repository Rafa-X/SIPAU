namespace UPZMG.Implementations.DataCapture;

using UPZMG.Shared.Enums;

public class CreateEmployee
{
    public required Guid RequesterKey { get; set; }
    public required int EmployeeNumber { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Rfc { get; set; }
    public required string Curp { get; set; }
    public required Gender Gender { get; set; }
    public DateOnly BirthDate { get; set; }
    public EducationLevel HighestEducationLevel { get; set; }
    public TitleStatus TitleStatus { get; set; }
    public List<string> IndigenousLanguages { get; set; } = [];
    public bool HasDisability { get; set; }
    public int ExternalXPYears { get; set; }
    public DateOnly FirstHiredDate { get; set; }
    public bool IsTeacher { get; set; }
}

/// <summary>A correction to an employee already captured.</summary>
public sealed class UpdateEmployee : CreateEmployee
{
    public required Guid Id { get; set; }
}