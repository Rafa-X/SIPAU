namespace UPZMG.Implementations.DataCapture;

/// <summary>One row of the captured-teachers listing.</summary>
public sealed record TeacherListItem(
    Guid Id,
    int EmployeeNumber,
    string FullName,
    string Email,
    string EducationLevel,
    DateOnly FirstHiredDate,
    bool HasDisability);
