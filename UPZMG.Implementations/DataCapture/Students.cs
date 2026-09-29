namespace UPZMG.Implementations.DataCapture;

using UPZMG.Shared.Enums;

public class CreateStudent
{
    public required Guid RequesterKey { get; set; }
    public required string EnrollmentNumber { get; set; }
    public required string CURP { get; set; }
    public required string Name { get; set; }
    public required string MiddleName { get; set; }
    public required string LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public Gender Gender { get; set; }
    public List<string> IndigenousLanguages { get; set; } = [];
    public string? PostalCode { get; set; }
    public EnglishLevel EnglishLevel { get; set; }
    public MaritalStatus MaritalStatus { get; set; }
    public int NumberOfChildren { get; set; }
    public bool Works { get; set; }
    public required string Address { get; set; }
    public required Guid CareerId { get; set; }
    public required Guid PeriodIdStart { get; set; } 
    public InscriptionType InscriptionType { get; set; }
    public StudentStatus Status { get; set; } = StudentStatus.Activo;
    public string? BirthCountry { get; set; }
    public string? BirthState { get; set; }
}

/// <summary>
/// A correction to a student already captured. It carries the same fields as the capture:
/// an area that can register a student can fix a typo in it without a second form.
/// </summary>
public sealed class UpdateStudent : CreateStudent
{
    public required Guid Id { get; set; }
}

/// <summary>Search + pagination filters for the students query listing.</summary>
public sealed class StudentsListRequest
{
    public required Guid RequesterKey { get; set; }
    public string? Name { get; set; }
    public string? EnrollmentNumber { get; set; }
    public Guid? CareerId { get; set; }
    public Guid? PeriodId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class StudentListItem
{
    public Guid Id { get; set; }
    public string EnrollmentNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Career { get; set; }
    public string? Period { get; set; }
    public StudentStatus Status { get; set; }
}

public sealed class StudentsListResponse
{
    public IReadOnlyList<StudentListItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
}