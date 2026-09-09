namespace CurveDep.Models;

/// <summary>
/// Точка кривой депрессии: расстояние от уреза воды и высота столба воды в этой точке.
/// </summary>
public record CurvePoint(double X, double Hx);