using UPZMG.Persistence.Models;

namespace UPZMG.Web.Components.Shared;

/// <summary>Wording shared by the report pages, so a status reads the same everywhere.</summary>
public static class ReportUi
{
    public static string StatusLabel(ReportStatus status) => status switch
    {
        ReportStatus.submitted => "Enviado",
        ReportStatus.approved => "Aprobado",
        ReportStatus.rejected => "Rechazado",
        _ => "Borrador",
    };

    /// <summary>Modifier class for <c>.sipau-report-status</c>.</summary>
    public static string StatusClass(ReportStatus status) => status switch
    {
        ReportStatus.draft => "is-draft",
        ReportStatus.rejected => "is-rejected",
        _ => "is-sent",
    };

    /// <summary>Report timestamps are stored in UTC; people read them in local time.</summary>
    public static string Local(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public static string Plural(int count, string one, string many) => $"{count} {(count == 1 ? one : many)}";
}
