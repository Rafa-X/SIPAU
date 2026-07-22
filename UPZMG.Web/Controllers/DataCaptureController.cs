using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UPZMG.Web.Models;

namespace UPZMG.Web.Controllers;

/// <summary>
/// Data capture module. Presents one capture form per institutional area
/// (Alumnos, Docentes, Infraestructura, Extensión, Finanzas) plus a bulk
/// import option. All submissions are mocked — thgen, nothing is persisted yet
/// </summary>
public class DataCaptureController : SigieBaseController
{
    private const string ActivePeriodLabel = "Ciclo 2025-2026 (Cuatrimestre 1)";

    public IActionResult Index()
    {
        SetUserViewData();

        // Build demo ViewModel (to be replaced with real DB/API queries later)
        var model = new DataCaptureIndexViewModel
        {
            ActivePeriod = ActivePeriodLabel,
            Areas = new List<CaptureAreaModel>
            {
                new() { Id = 1, AreaName = "Alumnos", ResponsiblePerson = "Lic. Ana Martínez", Percentage = 82, Status = "En progreso", LastUpdated = DateTime.Now.AddDays(-1), Icon = "🎓", IsCaptureAvailable = true, ActionName = nameof(Students) },
                new() { Id = 2, AreaName = "Docentes / RRHH", ResponsiblePerson = "Lic. Carlos López", Percentage = 100, Status = "Completo", LastUpdated = DateTime.Now.AddDays(-3), Icon = "👩‍🏫", IsCaptureAvailable = true, ActionName = nameof(Teachers) },
                new() { Id = 3, AreaName = "Infraestructura", ResponsiblePerson = "Ing. Roberto Díaz", Percentage = 45, Status = "En progreso", LastUpdated = DateTime.Now.AddDays(-5), Icon = "🏫", IsCaptureAvailable = true, ActionName = nameof(Infrastructure) },
                new() { Id = 4, AreaName = "Extensión y Vinculación", ResponsiblePerson = "Mtro. Pedro Sánchez", Percentage = 30, Status = "Pendiente", LastUpdated = DateTime.Now.AddDays(-8), Icon = "🤝", IsCaptureAvailable = true, ActionName = nameof(Extension) },
                new() { Id = 5, AreaName = "Finanzas", ResponsiblePerson = "C.P. María García", Percentage = 100, Status = "Completo", LastUpdated = DateTime.Now.AddDays(-2), Icon = "💰", IsCaptureAvailable = true, ActionName = nameof(Finance) },
            }
        };

        if (TempData["SuccessMessage"] is string successMessage)
        {
            ViewData["SuccessMessage"] = successMessage;
        }

        return View(model);
    }

    // === Alumnos ===

    [HttpGet]
    public IActionResult Students()
    {
        SetUserViewData();
        return View(new StudentFormViewModel
        {
            Careers = GetMockCareers(),
            Periods = GetMockPeriods()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Students(StudentFormViewModel model)
    {
        // Mock submit: nothing is persisted, we just simulate success and redirect back to Index.
        TempData["SuccessMessage"] = $"Alumno {model.FirstName} {model.LastName} guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // === Docentes / RRHH ===

    [HttpGet]
    public IActionResult Teachers()
    {
        SetUserViewData();
        return View(new TeacherFormViewModel
        {
            Areas = GetMockInternalAreas()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Teachers(TeacherFormViewModel model)
    {
        TempData["SuccessMessage"] = $"Docente {model.FirstName} {model.LastName} guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // === Infraestructura ===

    [HttpGet]
    public IActionResult Infrastructure()
    {
        SetUserViewData();
        return View(new InfrastructureFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Infrastructure(InfrastructureFormViewModel model)
    {
        TempData["SuccessMessage"] = $"Espacio físico '{model.SpaceName}' guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // === Extensión y Vinculación ===

    [HttpGet]
    public IActionResult Extension()
    {
        SetUserViewData();
        return View(new ExtensionFormViewModel
        {
            Periods = GetMockPeriods()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Extension(ExtensionFormViewModel model)
    {
        TempData["SuccessMessage"] = $"Proyecto de vinculación '{model.ProjectName}' guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // === Finanzas ===

    [HttpGet]
    public IActionResult Finance()
    {
        SetUserViewData();
        return View(new FinanceFormViewModel { FiscalYear = DateTime.Now.Year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Finance(FinanceFormViewModel model)
    {
        TempData["SuccessMessage"] = $"Resumen financiero {model.FiscalYear} guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // === Bulk import (delimited text file) ===

    [HttpGet]
    public IActionResult BulkImport()
    {
        SetUserViewData();
        return View(new BulkImportViewModel
        {
            Areas = GetMockCaptureAreas(),
            Delimiters = GetMockDelimiters()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BulkImport(BulkImportViewModel model)
    {
        // Mock parse: report a fake row count instead of actually reading the file.
        var fileName = model.File?.FileName ?? "archivo.txt";
        TempData["SuccessMessage"] = $"Archivo '{fileName}' procesado: 128 registros importados al área seleccionada.";
        return RedirectToAction(nameof(Index));
    }

    // === Mock dropdown catalogs (to be replaced with real DB/API queries later) ===

    private static List<SelectListItem> GetMockCareers() => new()
    {
        new SelectListItem("Ingeniería en Sistemas Computacionales", "1"),
        new SelectListItem("Ingeniería Industrial", "2"),
        new SelectListItem("Ingeniería en Biotecnología", "3"),
        new SelectListItem("Licenciatura en Administración y Gestión Empresarial", "4"),
    };

    private static List<SelectListItem> GetMockPeriods() => new()
    {
        new SelectListItem("2025-2026 / Cuatrimestre 1", "1", true),
        new SelectListItem("2025-2026 / Cuatrimestre 2", "2"),
        new SelectListItem("2024-2025 / Cuatrimestre 3", "3"),
    };

    private static List<SelectListItem> GetMockInternalAreas() => new()
    {
        new SelectListItem("Rectoría", "1"),
        new SelectListItem("Dirección Académica", "2"),
        new SelectListItem("Servicios Escolares", "3"),
        new SelectListItem("Recursos Humanos", "4"),
        new SelectListItem("Planeación", "5"),
    };

    private static List<SelectListItem> GetMockCaptureAreas() => new()
    {
        new SelectListItem("Alumnos", "students"),
        new SelectListItem("Docentes / RRHH", "teachers"),
        new SelectListItem("Infraestructura", "infrastructure"),
        new SelectListItem("Extensión y Vinculación", "extension"),
        new SelectListItem("Finanzas", "finance"),
    };

    private static List<SelectListItem> GetMockDelimiters() => new()
    {
        new SelectListItem("Coma ( , )", ","),
        new SelectListItem("Punto y coma ( ; )", ";"),
        new SelectListItem("Tabulación ( Tab )", "tab"),
        new SelectListItem("Barra vertical ( | )", "|"),
    };
}
