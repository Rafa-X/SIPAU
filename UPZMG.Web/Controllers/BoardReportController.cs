using Microsoft.AspNetCore.Mvc;
using UPZMG.Web.Models;

namespace UPZMG.Web.Controllers;

/// <summary>
/// Board report module — the standardized 60-100 page report presented to the
/// board of directors ("Informe de Junta Directiva"). List and detail views.
/// Not to be confused with <see cref="ReportsController"/>, which captures the
/// official SEP/SII statistical formats (911).
/// </summary>
public class BoardReportController : SigieBaseController
{
    // Mock in-memory catalog (to be replaced with real DB/API queries later)
    private static readonly List<BoardReportListItemModel> MockReports = new()
    {
        new() { Id = 1, Period = "2025-2026 / Cuatrimestre 1", GeneratedDate = new DateTime(2026, 6, 30), Status = "Enviado", PageCount = 42 },
        new() { Id = 2, Period = "2024-2025 / Cuatrimestre 3", GeneratedDate = new DateTime(2026, 2, 28), Status = "Generado", PageCount = 38 },
        new() { Id = 3, Period = "2024-2025 / Cuatrimestre 2", GeneratedDate = new DateTime(2025, 10, 15), Status = "Generado", PageCount = 40 },
        new() { Id = 4, Period = "2025-2026 / Cuatrimestre 2", GeneratedDate = new DateTime(2026, 7, 10), Status = "Borrador", PageCount = 12 },
    };

    public IActionResult Index()
    {
        SetUserViewData();

        var model = new BoardReportIndexViewModel
        {
            Reports = MockReports
        };

        return View(model);
    }

    public IActionResult Detail(int id)
    {
        SetUserViewData();

        var report = MockReports.FirstOrDefault(i => i.Id == id) ?? MockReports[0];

        var model = new BoardReportDetailViewModel
        {
            Id = report.Id,
            Period = report.Period,
            GeneratedDate = report.GeneratedDate,
            Status = report.Status,
            TotalAtypicalEventPhotos = 14,
            Sections = new List<BoardReportSectionModel>
            {
                new() { Name = "Matrícula", Description = "Comportamiento histórico y distribución por carrera", Icon = "🎓" },
                new() { Name = "Aprovechamiento", Description = "Índices de aprobación y reprobación por periodo", Icon = "📈" },
                new() { Name = "Titulación", Description = "Egresados titulados por generación y modalidad", Icon = "🏆" },
                new() { Name = "Becas", Description = "Becas asignadas por tipo y fuente de financiamiento", Icon = "💳" },
                new() { Name = "Infraestructura", Description = "Estado de instalaciones, equipamiento y espacios", Icon = "🏫" },
                new() { Name = "Eventos Protocolarios", Description = "Ceremonias, firmas de convenio y visitas institucionales", Icon = "🎤" },
            }
        };

        return View(model);
    }
}
