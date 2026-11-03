namespace UPZMG.Implementations.DataCapture;

/// <summary>One row of the staff listing.</summary>
public sealed record EmployeeListItem(
    Guid Id,
    int EmployeeNumber,
    string FullName,
    string Email,
    string EducationLevel,
    DateOnly FirstHiredDate,
    bool HasDisability,
    bool IsTeacher);
