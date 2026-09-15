using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

/// <summary>Board report (Informe de Junta) — mock list + detail (table of contents).</summary>
public interface IBoardReportService
{
    IReadOnlyList<BoardReportListItemModel> GetAll();
    BoardReportDetailViewModel? GetDetail(int id);
}

public sealed class BoardReportService : IBoardReportService
{
    private static readonly List<BoardReportListItemModel> Reports = new()
    {
        new() { Id = 1, Period = "2026 Enero - Abril", GeneratedDate = new DateTime(2026, 6, 30), Status = "Enviado", PageCount = 42 },
        new() { Id = 2, Period = "2025 Septiembre - Diciembre", GeneratedDate = new DateTime(2026, 2, 28), Status = "Generado", PageCount = 38 },
        new() { Id = 3, Period = "2025 Mayo - Agosto", GeneratedDate = new DateTime(2025, 10, 15), Status = "Generado", PageCount = 40 },
        new() { Id = 4, Period = "2026 Mayo - Agosto", GeneratedDate = new DateTime(2026, 7, 10), Status = "Borrador", PageCount = 12 },
    };

    public IReadOnlyList<BoardReportListItemModel> GetAll() => Reports;

    public BoardReportDetailViewModel? GetDetail(int id)
    {
        var report = Reports.FirstOrDefault(r => r.Id == id);
        if (report is null) return null;

        return new BoardReportDetailViewModel
        {
            Id = report.Id,
            Period = report.Period,
            GeneratedDate = report.GeneratedDate,
            Status = report.Status,
            TotalAtypicalEventPhotos = 14,
            Sections = new List<BoardReportSectionModel>
            {
                new() { Name = "Matrícula", Description = "Comportamiento histórico y distribución por carrera" },
                new() { Name = "Aprovechamiento", Description = "Índices de aprobación y reprobación por periodo" },
                new() { Name = "Titulación", Description = "Egresados titulados por generación y modalidad" },
                new() { Name = "Becas", Description = "Becas asignadas por tipo y fuente de financiamiento" },
                new() { Name = "Infraestructura", Description = "Estado de instalaciones, equipamiento y espacios" },
                new() { Name = "Eventos Protocolarios", Description = "Ceremonias, firmas de convenio y visitas institucionales" },
            },
        };
    }
}
