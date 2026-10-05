using UPZMG.Implementations.Dashboard;
using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Intelligence;
using UPZMG.Implementations.Reports;

namespace UPZMG.Implementations.BoardReport;

/// <summary>One official format as the report reads it: its state and the answers that were sent.</summary>
public sealed record BoardReportCapture(
    string ReportType,
    string DisplayName,
    string Status,
    DateTime LastModified,
    string? CapturedBy,
    ReportFormState State)
{
    public bool IsSubmitted => Status == "submitted";
}

public sealed record BoardStaffMember(string Gender, int EducationLevel, bool IsTeacher);

public sealed record BoardFacility(string Name, string Type, int Capacity, bool InUse);

/// <summary>One linkage project as the outreach area registered it.</summary>
public sealed record BoardOutreachProject(string Sector, string ActivityType, string Organization, int AccreditedHours, bool HasStudent);

/// <summary>The annual financial summary the administration captured for a fiscal year.</summary>
public sealed record BoardFinanceSummary(
    int FiscalYear,
    decimal FederalIncome,
    decimal StateIncome,
    decimal OwnIncomeEnrollment,
    decimal OwnIncomeServices,
    decimal PayrollExpense,
    decimal MaterialsExpense,
    decimal ServicesExpense,
    decimal InvestmentExpense)
{
    public decimal TotalIncome => FederalIncome + StateIncome + OwnIncomeEnrollment + OwnIncomeServices;
    public decimal TotalExpense => PayrollExpense + MaterialsExpense + ServicesExpense + InvestmentExpense;
}

/// <summary>Everything the document is built from. Loaded by the repository, consumed by the builder.</summary>
public sealed record BoardReportInputs(
    string Cycle,
    DashboardSummary Dashboard,
    AcademicHistory History,
    EnrollmentForecast Forecast,
    DropoutModelReport? Dropout,
    IReadOnlyList<AnomalyAlert> Anomalies,
    IReadOnlyList<BoardStaffMember> Staff,
    IReadOnlyList<BoardFacility> Facilities,
    IReadOnlyList<BoardOutreachProject> Outreach,
    ScholarshipsBySource? Scholarships,
    TitulationSummary? Titulation,
    BoardFinanceSummary? Finance,
    IReadOnlyList<BoardReportCapture> Captures,
    IReadOnlyDictionary<string, string> Narratives,
    string Status,
    DateTime? SubmittedAtUtc);
