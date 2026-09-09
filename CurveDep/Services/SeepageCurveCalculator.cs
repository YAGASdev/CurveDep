using CurveDep.Models;

namespace CurveDep.Services;

public class SeepageCalculationResult
{
    public double Beta { get; init; }
    public double DeltaLb { get; init; }
    public double DeltaLp { get; init; }
    public double Q { get; init; }
    public double Ld { get; init; }
}

public class SeepageCurveCalculator
{
    public double H1 { get; set; }
    public double M1 { get; set; }
    public double L { get; set; }
    public double Kt { get; set; }

    private double _q;
    private double _ld;

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

    /// <summary>
    /// Строит плотную сетку точек кривой депрессии для гладкой отрисовки на графике.
    /// Требует, чтобы перед этим был вызван Calculate().
    /// </summary>
    /// <summary>
    /// Строит плотную сетку точек кривой депрессии для гладкой отрисовки на графике.
    /// Требует, чтобы перед этим был вызван Calculate().
    /// Первые несколько точек плавно корректируются, чтобы кривая визуально
    /// начиналась точно от уровня H1 у уреза воды (косметическая поправка,
    /// не влияющая на табличные расчётные значения).
    /// </summary>
    public List<CurvePoint> BuildCurve(int pointsCount = 60)
    {
        var list = new List<CurvePoint>();
        if (L <= 0 || pointsCount < 2)
            return list;

        double step = L / (pointsCount - 1);

        // Расхождение между реальным H1 и тем, что даёт формула в x=0
        double rawStart = CalculateHx(0);
        double correction = H1 - rawStart;

        // Количество точек, на которые распределяем плавную коррекцию
        int smoothCount = Math.Min(15, pointsCount);

        for (int i = 0; i < pointsCount; i++)
        {
            double x = i * step;
            double hx = CalculateHx(x);

            if (i < smoothCount)
            {
                // Плавный убывающий вес коррекции: от 1.0 в начале до 0.0 на границе smoothCount
                double t = (double)i / smoothCount;
                double weight = Math.Pow(1 - t, 2); // квадратичное затухание — резкий старт, плавный хвост
                hx += correction * weight;
            }

            list.Add(new CurvePoint(x, Math.Round(hx, 2)));
        }

        return list;
    }
}