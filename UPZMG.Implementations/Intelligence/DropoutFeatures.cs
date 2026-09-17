using System.Globalization;

namespace UPZMG.Implementations.Intelligence;

/// <summary>One input of the dropout model, as documented to the user.</summary>
public sealed record FeatureInfo(string Name, string Definition);

/// <summary>
/// The dropout model's inputs, measured on the last graded period before the outcome
/// (the latest one for active students). Only rates and traits, never totals: for a
/// finished trajectory, "subjects taken" or "periods enrolled" encode how long the
/// student lasted, which is the outcome itself.
/// </summary>
public static class DropoutFeatures
{
    public const string Average = "Promedio";
    public const string FailureRate = "Reprobación %";
    public const string RetakesPerPeriod = "Extraordinarios por periodo";
    public const string EntryAge = "Edad al ingreso";
    public const string Works = "Trabaja";

    public static readonly IReadOnlyList<FeatureInfo> All =
    [
        new(Average, "Media de las calificaciones de su último periodo con calificaciones (0 a 10)."),
        new(FailureRate, "Intentos reprobados entre intentos cursados en ese periodo, por 100."),
        new(RetakesPerPeriod, "Exámenes extraordinarios que presentó en ese periodo."),
        new(EntryAge, "Años cumplidos al iniciar su primer periodo, no la edad de hoy."),
        new(Works, "1 si el alumno declaró que trabaja, 0 si no."),
    ];

    /// <summary>The bounds a rule puts on one input, in plain Spanish, e.g. "Reprobación mayor a 32.5 %".</summary>
    public static string Describe(string feature, double? above, double? atMost)
    {
        // Binary input: the only cut is 0.5, between "no" and "yes".
        if (feature == Works) return above is not null ? "Trabaja" : "No trabaja";

        // Whole years: "≤ 18.5" means 18 or less, "> 18.5" means 19 or more.
        if (feature == EntryAge)
        {
            var from = above is { } a ? (int)Math.Floor(a) + 1 : (int?)null;
            var to = atMost is { } b ? (int)Math.Floor(b) : (int?)null;
            return (from, to) switch
            {
                ({ } f, { } t) when f == t => $"Ingresó con {f} años",
                ({ } f, { } t) => $"Ingresó con {f} a {t} años",
                ({ } f, null) => $"Ingresó con {f} años o más",
                _ => $"Ingresó con {to} años o menos",
            };
        }

        var (name, unit) = feature == FailureRate ? ("Reprobación", " %") : (feature, "");
        return (above, atMost) switch
        {
            ({ } low, { } high) => $"{name} entre {Number(low)} y {Number(high)}{unit}",
            ({ } low, null) => $"{name} mayor a {Number(low)}{unit}",
            _ => $"{name} de {Number(atMost!.Value)}{unit} o menos",
        };
    }

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
