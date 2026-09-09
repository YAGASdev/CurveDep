namespace CurveDep.Services;

/// <summary>
/// Схематическая геометрия профиля плотины для отрисовки.
/// Верховой откос строится по реальным данным (H1, M1),
/// гребень и низовой откос — условно, для визуального контекста.
/// Система координат: X слева направо (0 = урез воды), Y снизу вверх (0 = основание).
/// </summary>
public class DamProfileGeometry
{
    private const double CrestWidth = 5.0;      // ширина гребня, м
    private const double FreeboardRatio = 0.15; // запас высоты гребня над водой, доля от H1

    public double H1 { get; set; }
    public double M1 { get; set; }
    public double L { get; set; }

    public double CrestHeight => H1 * (1 + FreeboardRatio);

    /// <summary>Левая нижняя точка (основание верхового откоса).</summary>
    public (double X, double Y) UpstreamToeBottom => (-M1 * H1, 0);

    /// <summary>Урез воды (верх воды на верховом откосе).</summary>
    public (double X, double Y) WaterEdge => (0, H1);

    /// <summary>
    /// Верхняя точка верхового откоса (пересечение с гребнем).
    /// Лежит на той же прямой, что и UpstreamToeBottom и WaterEdge:
    /// x(y) = M1 * (y - H1).
    /// </summary>
    public (double X, double Y) UpstreamCrestPoint => (M1 * (CrestHeight - H1), CrestHeight);

    /// <summary>Правая точка гребня (начало низового откоса).</summary>
    public (double X, double Y) DownstreamCrestPoint => (UpstreamCrestPoint.X + CrestWidth, CrestHeight);

    /// <summary>Точка на основании, где заканчивается зона фильтрации (x = L).</summary>
    public (double X, double Y) DownstreamToeBottom => (L, 0);

    /// <summary>
    /// Полный контур тела плотины (замкнутый многоугольник) для заливки.
    /// </summary>
    public List<(double X, double Y)> GetBodyContour()
    {
        return new List<(double X, double Y)>
        {
            UpstreamToeBottom,
            UpstreamCrestPoint,
            DownstreamCrestPoint,
            DownstreamToeBottom
        };
    }
}