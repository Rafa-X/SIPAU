namespace UPZMG.Web.Services;

// ── CUCEI Módulo 2 (Sistemas Inteligentes) demonstration data ────────────────

/// <summary>Least-squares linear regression forecast over historical enrollment.</summary>
public sealed record EnrollmentForecast(
    string[] Labels,
    double[] Actual,     // historical values + forecast for the last periods
    double[] Trend,      // fitted regression line ŷ = a + b·x for every period
    int HistoricalCount, // how many of Labels are real (rest are forecast)
    double Slope,        // b
    double Intercept,    // a
    double R2);          // goodness of fit (0..1)

/// <summary>Predicted dropout risk for a career (decision-tree style scoring).</summary>
public sealed record DropoutRisk(string Career, double RiskPct, string Level, string TopFactor);

/// <summary>Detected statistical anomaly to flag to Planeación.</summary>
public sealed record AnomalyAlert(string Title, string Detail, string Severity); // info | warning | error

public interface IIntelligenceService
{
    EnrollmentForecast ForecastEnrollment(int periodsAhead = 2);
    IReadOnlyList<DropoutRisk> DropoutRisks();
    IReadOnlyList<AnomalyAlert> Anomalies();
}

/// <summary>
/// Intelligent-analytics layer for the SIGIE dashboards. The enrollment
/// forecast is a genuine ordinary-least-squares linear regression computed in
/// C#; dropout risk and anomaly flags are demo data shaped like a real model's
/// output. Anchors the CUCEI "Sistemas Inteligentes" module.
/// </summary>
public sealed class IntelligenceService : IIntelligenceService
{
    // Historical institutional enrollment (same source the charts use).
    private static readonly string[] HistoricalLabels =
        { "2022-1", "2022-2", "2023-1", "2023-2", "2024-1", "2024-2", "2025-1", "2025-2" };
    private static readonly double[] HistoricalValues =
        { 890, 940, 1005, 1060, 1120, 1150, 1198, 1245 };

    public EnrollmentForecast ForecastEnrollment(int periodsAhead = 2)
    {
        int n = HistoricalValues.Length;

        // Ordinary least squares: x = 0..n-1
        double sumX = 0, sumY = 0, sumXy = 0, sumXx = 0;
        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += HistoricalValues[i];
            sumXy += i * HistoricalValues[i];
            sumXx += (double)i * i;
        }

        double slope = (n * sumXy - sumX * sumY) / (n * sumXx - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;

        // Coefficient of determination R²
        double meanY = sumY / n;
        double ssTot = 0, ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            double predicted = intercept + slope * i;
            ssTot += Math.Pow(HistoricalValues[i] - meanY, 2);
            ssRes += Math.Pow(HistoricalValues[i] - predicted, 2);
        }
        double r2 = ssTot == 0 ? 1 : 1 - ssRes / ssTot;

        int total = n + periodsAhead;
        var labels = new string[total];
        var actual = new double[total];
        var trend = new double[total];

        for (int i = 0; i < total; i++)
        {
            trend[i] = Math.Round(intercept + slope * i, 1);
            if (i < n)
            {
                labels[i] = HistoricalLabels[i];
                actual[i] = HistoricalValues[i];
            }
            else
            {
                // forecast periods: 2026-1, 2026-2, ...
                labels[i] = $"2026-{i - n + 1}";
                actual[i] = Math.Round(intercept + slope * i, 0); // model projection
            }
        }

        return new EnrollmentForecast(labels, actual, trend, n,
            Math.Round(slope, 2), Math.Round(intercept, 1), Math.Round(r2, 4));
    }

    public IReadOnlyList<DropoutRisk> DropoutRisks() => new List<DropoutRisk>
    {
        new("Ing. en Biotecnología", 24.5, "Alto", "Reprobación en ciencias básicas"),
        new("Ing. Industrial", 15.2, "Medio", "Carga académica del 3er cuatrimestre"),
        new("Ing. en Sistemas Computacionales", 9.8, "Bajo", "Buen índice de aprobación"),
        new("Lic. Admón. y Gestión", 12.6, "Medio", "Colegiaturas vencidas"),
    };

    public IReadOnlyList<AnomalyAlert> Anomalies() => new List<AnomalyAlert>
    {
        new("Caída de aspirantes en Biotecnología", "-18% de solicitudes vs. el mismo periodo del ciclo anterior. Recomendado revisar campaña de difusión.", "error"),
        new("Incremento de colegiaturas vencidas", "+12% de adeudos respecto al periodo previo, concentrado en 2° cuatrimestre.", "warning"),
        new("Matrícula por encima de la tendencia", "El periodo 2025-2 superó en +2.1% la proyección del modelo — posible efecto de nuevas becas.", "info"),
    };
}
