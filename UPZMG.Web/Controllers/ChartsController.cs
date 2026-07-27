using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using UPZMG.Web.Models;

namespace UPZMG.Web.Controllers;

/// <summary>
/// Renders the institutional statistics charts page with Chart.js data (WIP)
/// </summary>
public class ChartsController : SigieBaseController
{
    public IActionResult Index()
    {
        var (displayName, initials) = SetUserViewData();

        // Build demo ViewModel (to be replaced with real DB/API queries later)
        var historicalPeriods = new List<string> { "2022-1", "2022-2", "2023-1", "2023-2", "2024-1", "2024-2", "2025-1", "2025-2" };
        var projectedPeriods = new List<string> { "2026-1", "2026-2" };
        var historicalValues = new List<int> { 890, 940, 1005, 1060, 1120, 1150, 1198, 1245 };
        var projectedValues = new List<int> { 1300, 1350 };

        // "Enrollment" dataset: real values for historical periods, null for projected ones
        var historicalEnrollment = historicalValues.Select(v => (int?)v).ToList();
        historicalEnrollment.AddRange(projectedPeriods.Select(_ => (int?)null));

        // "Projection" dataset: null until the last historical period (connects the dashed line),
        // then the two forecasted values
        var projectedEnrollment = new List<int?>();
        projectedEnrollment.AddRange(historicalPeriods.Skip(1).Select(_ => (int?)null));
        projectedEnrollment.Add(historicalValues[^1]);
        projectedEnrollment.AddRange(projectedValues.Select(v => (int?)v));

        var model = new ChartsViewModel
        {
            EnrollmentPeriods = historicalPeriods.Concat(projectedPeriods).ToList(),
            HistoricalEnrollment = historicalEnrollment,
            ProjectedEnrollment = projectedEnrollment,
            // more and more mock data....
            Careers = new List<string> { "Ing. Sistemas Computacionales", "Ing. Industrial", "Ing. Biotecnología", "Lic. Admón. y Gestión Empresarial" },
            StudentsByCareer = new List<int> { 420, 355, 210, 260 },

            AchievementPeriods = new List<string> { "2024-1", "2024-2", "2025-1", "2025-2" },
            ApprovalRate = new List<decimal> { 84.2m, 85.6m, 86.9m, 87.5m },
            FailureRate = new List<decimal> { 15.8m, 14.4m, 13.1m, 12.5m },

            ScholarshipTypes = new List<string> { "Excelencia Académica", "Manutención (Gob. Estatal)", "Deportiva", "PRONABES / Federal" },
            ScholarshipsAssigned = new List<int> { 180, 260, 65, 340 },

            UserDisplayName = displayName,
            UserInitials = initials
        };

        // Pass user info to layout's sidebar
        ViewData["UserName"] = model.UserDisplayName;
        ViewData["UserInitials"] = model.UserInitials;

        return View(model);
    }
}
