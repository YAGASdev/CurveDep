namespace CurveDep.Services;

/// <summary>
/// Результат расчёта параметров кривой депрессии земляной плотины.
/// </summary>
public class SeepageCalculationResult
{
    public double Beta { get; init; }
    public double DeltaLb { get; init; }
    public double DeltaLp { get; init; }
    public double Q { get; init; }
    public double Ld { get; init; }
}

/// <summary>
/// Чистая математика расчёта кривой депрессии.
/// Не зависит от UI — можно использовать как для таблицы, так и для графика.
/// </summary>
public class SeepageCurveCalculator
{
    public double H1 { get; set; }
    public double M1 { get; set; }
    public double L { get; set; }
    public double Kt { get; set; }

    private double _q;
    private double _ld;

    /// <summary>
    /// Выполняет основной расчёт (β, ΔLb, ΔLp, q, Lδ).
    /// Обязательно вызвать перед CalculateHx.
    /// </summary>
    public SeepageCalculationResult Calculate()
    {
        double h1 = Math.Round(H1, 2);
        double m1 = Math.Round(M1, 2);
        double l = Math.Round(L, 2);
        double kt = Math.Round(Kt, 2);

        double beta = Math.Round(m1 / (2 * m1 + 1), 2);
        double deltaLb = Math.Round(beta * h1, 2);
        double deltaLp = Math.Round(l + deltaLb, 2);
        double q = Math.Round((h1 * h1) / (2 * deltaLp) * kt, 2);
        double ld = Math.Round((0.5 * q) / kt, 2);

        // Сохраняем для последующих вызовов CalculateHx
        _q = q;
        _ld = ld;

        return new SeepageCalculationResult
        {
            Beta = beta,
            DeltaLb = deltaLb,
            DeltaLp = deltaLp,
            Q = q,
            Ld = ld
        };
    }

    /// <summary>
    /// Высота кривой депрессии hx в точке x.
    /// Требует, чтобы перед этим был вызван Calculate().
    /// </summary>
    public double CalculateHx(double x)
    {
        try
        {
            double result = Math.Sqrt(2 * (_q / Kt) * (L - x + _ld));
            return Math.Round(result, 2);
        }
        catch (Exception)
        {
            return 0.0;
        }
    }
}