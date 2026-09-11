using MudBlazor;

namespace UPZMG.Web.Components.Shared;

/// <summary>
/// Single source of truth for mapping domain status/severity labels to MudBlazor
/// colors. Centralizing this keeps every list, grid and card visually consistent
/// and means status→color changes happen in one place.
/// </summary>
public static class SigieUi
{
    public static Color ProgressColor(int percentage) => percentage >= 100
        ? Color.Success
        : Color.Warning;

    public static Color StatusColor(string status) => status switch
    {
        "Completo" or "Firmada" or "Enviado" => Color.Success,

        "En progreso" or "En revisión gobierno" or "Pendiente de firmas" => Color.Warning,

        "Generado" => Color.Info,

        _ => Color.Default,
    };

    public static Color RiskColor(string level) => level switch
    {
        "Alto" => Color.Warning,
        _      => Color.Primary,
    };

    public static Color BalanceColor(bool balances) => balances ? Color.Default : Color.Error;

    public static Severity AlertSeverity(string kind) => kind switch
    {
        "error" or "danger" => Severity.Error,
        "warning"           => Severity.Warning,
        "success"           => Severity.Success,
        _                   => Severity.Info,
    };
}
