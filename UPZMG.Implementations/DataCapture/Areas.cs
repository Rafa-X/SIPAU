using System.ComponentModel.DataAnnotations;
using UPZMG.Shared.Enums;

namespace UPZMG.Implementations.DataCapture;

// ── Infrastructure: school spaces ───────────────────────────────────────────

public sealed class CreateFacility
{
    public required Guid RequesterKey { get; set; }

    [Required(ErrorMessage = "Indica el nombre del espacio."), MaxLength(50)]
    public required string Name { get; set; }

    public SchoolFacilityType Type { get; set; }

    [Range(0, 100000, ErrorMessage = "La superficie no puede ser negativa.")]
    public double SquareMeters { get; set; }

    [Range(0, 2000, ErrorMessage = "La capacidad debe estar entre 0 y 2000.")]
    public int Capacity { get; set; }

    [Range(0, 2000, ErrorMessage = "El número de equipos no puede ser negativo.")]
    public int ComputersInOperation { get; set; }

    public bool InUse { get; set; } = true;
    public bool HasInternet { get; set; }
    public bool HasComputers { get; set; }
    public bool SupportDisabilities { get; set; }
    public List<string> Problems { get; set; } = [];
}

public sealed record FacilityListItem(
    Guid Id,
    string Name,
    string Type,
    double SquareMeters,
    int Capacity,
    int ComputersInOperation,
    bool InUse,
    bool HasInternet,
    bool HasComputers,
    bool SupportDisabilities,
    int GroupsThisPeriod);

// ── Outreach: linkage projects ──────────────────────────────────────────────

public sealed class CreateOutreachProject
{
    public required Guid RequesterKey { get; set; }

    [Required(ErrorMessage = "Indica la organización."), MaxLength(200)]
    public required string OrganizationName { get; set; }

    [Required(ErrorMessage = "Indica el sector."), MaxLength(50)]
    public required string Sector { get; set; }

    [Required(ErrorMessage = "Indica el tamaño de la organización."), MaxLength(50)]
    public required string CompanySize { get; set; }

    [Required(ErrorMessage = "Indica el tipo de actividad."), MaxLength(50)]
    public required string ActivityType { get; set; }

    [Required(ErrorMessage = "Indica el nombre del proyecto."), MaxLength(200)]
    public required string ProjectName { get; set; }

    public required Guid PeriodId { get; set; }

    /// <summary>Optional: when it matches a registered student, the project is linked to them.</summary>
    [MaxLength(20)]
    public string? StudentEnrollmentNumber { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [Range(0, 5000, ErrorMessage = "Las horas acreditadas deben estar entre 0 y 5000.")]
    public int AccreditedHours { get; set; }
}

public sealed record OutreachListItem(
    Guid Id,
    string OrganizationName,
    string Sector,
    string CompanySize,
    string ActivityType,
    string ProjectName,
    string Period,
    string? StudentName,
    string? StudentEnrollmentNumber,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int AccreditedHours);

// ── Finance: annual summary ─────────────────────────────────────────────────

public sealed class SaveAnnualFinance
{
    public required Guid RequesterKey { get; set; }

    [Range(2000, 2100, ErrorMessage = "Indica un ejercicio fiscal válido.")]
    public int FiscalYear { get; set; }

    [Range(0, 999999999999)] public decimal FederalIncome { get; set; }
    [Range(0, 999999999999)] public decimal StateIncome { get; set; }
    [Range(0, 999999999999)] public decimal OwnIncomeEnrollment { get; set; }
    [Range(0, 999999999999)] public decimal OwnIncomeServices { get; set; }

    [Range(0, 999999999999)] public decimal PayrollExpense { get; set; }
    [Range(0, 999999999999)] public decimal MaterialsExpense { get; set; }
    [Range(0, 999999999999)] public decimal ServicesExpense { get; set; }
    [Range(0, 999999999999)] public decimal InvestmentExpense { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>One fiscal year as the finance screen lists it. Totals are computed, never captured.</summary>
public sealed record FinanceListItem(
    Guid Id,
    int FiscalYear,
    decimal FederalIncome,
    decimal StateIncome,
    decimal OwnIncomeEnrollment,
    decimal OwnIncomeServices,
    decimal PayrollExpense,
    decimal MaterialsExpense,
    decimal ServicesExpense,
    decimal InvestmentExpense,
    string? Notes,
    DateTime LastModified)
{
    public decimal TotalIncome => FederalIncome + StateIncome + OwnIncomeEnrollment + OwnIncomeServices;

    public decimal TotalExpense => PayrollExpense + MaterialsExpense + ServicesExpense + InvestmentExpense;

    public decimal Balance => TotalIncome - TotalExpense;
}
