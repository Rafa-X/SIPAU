using MudBlazor;

namespace UPZMG.Web.Components.Shared;

/// <summary>
/// Single source of truth for mapping domain status/severity labels to MudBlazor
/// colors. Centralizing this keeps every list, grid and card visually consistent
/// and means status→color changes happen in one place.
/// </summary>
public static class SigieUi
{
    /// <summary>Capture/completion percentage → progress bar color.</summary>
    public static Color ProgressColor(int percentage) => percentage switch
    {
        100     => Color.Success,
        >= 50   => Color.Warning,
        _       => Color.Error,
    };

    /// <summary>Any module's status label → chip color (covers all modules' vocabularies).</summary>
    public static Color StatusColor(string status) => status switch
    {
        "Completo" or "Firmada" or "Enviado"   => Color.Success,
        "Generado" or "En revisión gobierno"   => Color.Info,
        "En progreso"                          => Color.Warning,
        _                                      => Color.Default, // Pendiente / Pendiente de firmas / Borrador
    };

    /// <summary>Predicted risk level → color.</summary>
    public static Color RiskColor(string level) => level switch
    {
        "Alto"  => Color.Error,
        "Medio" => Color.Warning,
        _       => Color.Success,
    };

    /// <summary>Alert "kind" string (info/warning/success/error/danger) → MudBlazor severity.</summary>
    public static Severity AlertSeverity(string kind) => kind switch
    {
        "error" or "danger" => Severity.Error,
        "warning"           => Severity.Warning,
        "success"           => Severity.Success,
        _                   => Severity.Info,
    };
}
