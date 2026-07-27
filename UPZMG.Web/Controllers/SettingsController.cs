using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UPZMG.Web.Models.Settings;

namespace UPZMG.Web.Controllers;

/// <summary>
/// Settings module (admin only). Manages institutional master data, academic
/// periods, and the configurable indicators catalog. All writes are mocked.
/// </summary>
[Authorize(Roles = "Admin")]
public class SettingsController : SigieBaseController
{
    public IActionResult Index()
    {
        SetUserViewData();
        return View();
    }

    // === Institutional configuration ===

    [HttpGet]
    public IActionResult Institution()
    {
        SetUserViewData();

        // Pre-filled with current mock values (edit-existing use case).
        var model = new InstitutionSettingsViewModel
        {
            InstitutionName = "Universidad Politécnica de la Zona Metropolitana de Guadalajara",
            InstitutionKey = "UPZMG",
            CampusName = "Campus Tlajomulco",
            CctKey = "14EPO0001X",
            RectorName = "Dr. Hassem Macías",
            Sostenimiento = "Publico_Estatal",
            Phone = "33 1234 5678",
            Website = "https://upzmg.edu.mx",
            ResponsibleName = "Mtro. Francisco (Subdirección de Planeación)",
            ResponsiblePosition = "Subdirector de Planeación, Evaluación y Calidad",
            ResponsibleEmail = "planeacion@upzmg.edu.mx",
            Street = "Av. Universidad",
            StreetNumber = "1000",
            Neighborhood = "Centro",
            Municipality = "Tlajomulco de Zúñiga",
            State = "Jalisco",
            PostalCode = "45640",
            SostenimientoOptions = GetSostenimientoOptions()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Institution(InstitutionSettingsViewModel model)
    {
        TempData["SettingsStatus"] = "Configuración institucional guardada correctamente.";
        return RedirectToAction(nameof(Institution));
    }

    // === Academic periods ===

    [HttpGet]
    public IActionResult Periods()
    {
        SetUserViewData();

        var model = new PeriodsSettingsViewModel
        {
            Periods = new List<PeriodItemModel>
            {
                new() { Code = "2025-2026-1", SchoolCycle = "2025-2026", StartDate = new DateTime(2025, 9, 1), EndDate = new DateTime(2025, 12, 20), IsActive = true },
                new() { Code = "2024-2025-3", SchoolCycle = "2024-2025", StartDate = new DateTime(2025, 5, 5), EndDate = new DateTime(2025, 8, 15), IsActive = false },
                new() { Code = "2024-2025-2", SchoolCycle = "2024-2025", StartDate = new DateTime(2025, 1, 13), EndDate = new DateTime(2025, 4, 25), IsActive = false },
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddPeriod(string code, string schoolCycle)
    {
        TempData["SettingsStatus"] = $"Periodo '{code}' agregado correctamente.";
        return RedirectToAction(nameof(Periods));
    }

    // === Configurable indicators ===

    [HttpGet]
    public IActionResult Indicators()
    {
        SetUserViewData();

        var model = new IndicatorsSettingsViewModel
        {
            Indicators = new List<IndicatorItemModel>
            {
                new() { ReportSheet = "911.9A — Matrícula", Name = "Alumnos de nuevo ingreso", DataType = "Número", IsActive = true },
                new() { ReportSheet = "911.9A — Matrícula", Name = "Índice de aprovechamiento", DataType = "Porcentaje", IsActive = true },
                new() { ReportSheet = "SII — Alumnos", Name = "Alumnos con beca federal", DataType = "Número", IsActive = true },
                new() { ReportSheet = "Anexo TIC", Name = "Computadoras con internet", DataType = "Número", IsActive = true },
                new() { ReportSheet = "SII — Alumnos", Name = "Alumnos que trabajan", DataType = "Booleano", IsActive = false },
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddIndicator(string reportSheet, string name, string dataType)
    {
        TempData["SettingsStatus"] = $"Indicador '{name}' agregado a la hoja '{reportSheet}'.";
        return RedirectToAction(nameof(Indicators));
    }

    private static List<SelectListItem> GetSostenimientoOptions() => new()
    {
        new SelectListItem("Público Federal", "Publico_Federal"),
        new SelectListItem("Público Estatal", "Publico_Estatal"),
        new SelectListItem("Privado", "Privado"),
    };
}
