using CurveDep.Models;
using CurveDep.Services;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Collections;

namespace CurveDep.Controls;

public class DamCurveCanvasView : SKCanvasView
{
    public static readonly BindableProperty H1Property =
        BindableProperty.Create(nameof(H1), typeof(double), typeof(DamCurveCanvasView), 0.0,
            propertyChanged: (b, o, n) => ((DamCurveCanvasView)b).InvalidateSurface());

    public double H1
    {
        get => (double)GetValue(H1Property);
        set => SetValue(H1Property, value);
    }

    public static readonly BindableProperty M1Property =
        BindableProperty.Create(nameof(M1), typeof(double), typeof(DamCurveCanvasView), 0.0,
            propertyChanged: (b, o, n) => ((DamCurveCanvasView)b).InvalidateSurface());

    public double M1
    {
        get => (double)GetValue(M1Property);
        set => SetValue(M1Property, value);
    }

    public static readonly BindableProperty LProperty =
        BindableProperty.Create(nameof(L), typeof(double), typeof(DamCurveCanvasView), 0.0,
            propertyChanged: (b, o, n) => ((DamCurveCanvasView)b).InvalidateSurface());

    public double L
    {
        get => (double)GetValue(LProperty);
        set => SetValue(LProperty, value);
    }

    public static readonly BindableProperty CurvePointsProperty =
        BindableProperty.Create(nameof(CurvePoints), typeof(IEnumerable), typeof(DamCurveCanvasView), null,
            propertyChanged: (b, o, n) => ((DamCurveCanvasView)b).InvalidateSurface());

    public IEnumerable? CurvePoints
    {
        get => (IEnumerable?)GetValue(CurvePointsProperty);
        set => SetValue(CurvePointsProperty, value);
    }

    public static readonly BindableProperty TablePointsProperty =
        BindableProperty.Create(nameof(TablePoints), typeof(IEnumerable), typeof(DamCurveCanvasView), null,
            propertyChanged: (b, o, n) => ((DamCurveCanvasView)b).InvalidateSurface());

    public IEnumerable? TablePoints
    {
        get => (IEnumerable?)GetValue(TablePointsProperty);
        set => SetValue(TablePointsProperty, value);
    }

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(DamCurveCanvasView), -1,
            propertyChanged: (b, o, n) => ((DamCurveCanvasView)b).InvalidateSurface());

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Срабатывает при клике на одну из опорных точек. Параметр — индекс точки (0-6).</summary>
    public event Action<int>? PointSelected;

    private readonly List<SKPoint> _screenTablePoints = new();

    // Желаемая доля высоты холста, которую должна занимать фигура по вертикали
    private const double TargetHeightRatio = 0.85;

    public DamCurveCanvasView()
    {
        PaintSurface += OnPaintSurface;
        EnableTouchEvents = true;
        Touch += OnTouch;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        if (H1 <= 0 || M1 <= 0 || L <= 0)
            return;

        var geometry = new DamProfileGeometry { H1 = H1, M1 = M1, L = L };

        var contour = geometry.GetBodyContour();
        double minX = contour.Min(p => p.X);
        double maxX = contour.Max(p => p.X);
        double maxY = contour.Max(p => p.Y);

        float margin = 30;
        double modelWidth = maxX - minX;
        double modelHeight = maxY;

        float availableWidth = e.Info.Width - 2 * margin;
        float availableHeight = e.Info.Height - 2 * margin;

        // Базовый (честный) масштаб по ширине — заполняем всю доступную ширину
        float scaleX = (float)(availableWidth / modelWidth);

        // Высота фигуры при таком же масштабе по Y (без преувеличения)
        double naturalDrawnHeight = modelHeight * scaleX;

        // Подбираем коэффициент вертикального преувеличения с шагом 0.5,
        // чтобы фигура занимала примерно TargetHeightRatio от доступной высоты,
        // но не выходила за её пределы.
        double verticalExaggeration = 1.0;
        if (naturalDrawnHeight > 0)
        {
            double raw = (availableHeight * TargetHeightRatio) / naturalDrawnHeight;
            double stepped = Math.Floor(raw / 0.5) * 0.5;
            verticalExaggeration = Math.Max(1.0, stepped);
        }

        float scaleY = (float)(scaleX * verticalExaggeration);

        float drawnWidth = (float)(modelWidth * scaleX);
        float drawnHeight = (float)(modelHeight * scaleY);

        float offsetX = margin + (availableWidth - drawnWidth) / 2f;
        float offsetY = margin + (availableHeight - drawnHeight) / 2f;

        SKPoint ToScreen(double x, double y) => new(
            offsetX + (float)(x - minX) * scaleX,
            offsetY + drawnHeight - (float)y * scaleY
        );

        DrawDamBody(canvas, geometry, contour, ToScreen);

        using var bodyClipPath = new SKPath();
        bodyClipPath.MoveTo(ToScreen(contour[0].X, contour[0].Y));
        for (int i = 1; i < contour.Count; i++)
            bodyClipPath.LineTo(ToScreen(contour[i].X, contour[i].Y));
        bodyClipPath.Close();

        canvas.Save();
        canvas.ClipPath(bodyClipPath);
        DrawSeepageCurve(canvas, ToScreen);
        canvas.Restore();

        // Точки рисуем поверх, без обрезки — должны быть видны и кликабельны всегда.
        DrawTablePoints(canvas, ToScreen);

        if (verticalExaggeration > 1.0)
        {
            DrawExaggerationLabel(canvas, e.Info, verticalExaggeration);
        }
    }

    private void DrawDamBody(SKCanvas canvas, DamProfileGeometry geometry,
        List<(double X, double Y)> contour, Func<double, double, SKPoint> toScreen)
    {
        using var bodyPaint = new SKPaint
        {
            Color = new SKColor(0x8B, 0x5A, 0x2B),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var bodyPath = new SKPath();
        bodyPath.MoveTo(toScreen(contour[0].X, contour[0].Y));
        for (int i = 1; i < contour.Count; i++)
            bodyPath.LineTo(toScreen(contour[i].X, contour[i].Y));
        bodyPath.Close();

        canvas.DrawPath(bodyPath, bodyPaint);

        using var strokePaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        canvas.DrawPath(bodyPath, strokePaint);

        using var waterPaint = new SKPaint
        {
            Color = new SKColor(0x4F, 0xA8, 0xE0, 0x90),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var waterPath = new SKPath();
        double minX = contour.Min(p => p.X);
        waterPath.MoveTo(toScreen(minX - 5, 0));
        waterPath.LineTo(toScreen(minX - 5, geometry.WaterEdge.Y));
        waterPath.LineTo(toScreen(geometry.WaterEdge.X, geometry.WaterEdge.Y));
        waterPath.LineTo(toScreen(geometry.UpstreamToeBottom.X, geometry.UpstreamToeBottom.Y));
        waterPath.Close();

        canvas.DrawPath(waterPath, waterPaint);
    }

    private void DrawSeepageCurve(SKCanvas canvas, Func<double, double, SKPoint> toScreen)
    {
        if (CurvePoints == null)
            return;

        var screenPoints = new List<SKPoint>();
        foreach (var obj in CurvePoints)
        {
            if (obj is CurvePoint p)
                screenPoints.Add(toScreen(p.X, p.Hx));
        }

        if (screenPoints.Count < 2)
            return;

        using var curvePaint = new SKPaint
        {
            Color = SKColors.DarkBlue,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        using var curvePath = new SKPath();
        curvePath.MoveTo(screenPoints[0]);
        for (int i = 1; i < screenPoints.Count; i++)
            curvePath.LineTo(screenPoints[i]);

        canvas.DrawPath(curvePath, curvePaint);
    }

    private void DrawTablePoints(SKCanvas canvas, Func<double, double, SKPoint> toScreen)
    {
        _screenTablePoints.Clear();

        if (TablePoints == null)
            return;

        int index = 0;
        foreach (var obj in TablePoints)
        {
            if (obj is CurvePoint p)
            {
                var screenPoint = toScreen(p.X, p.Hx);
                _screenTablePoints.Add(screenPoint);

                bool isSelected = index == SelectedIndex;

                using var pointPaint = new SKPaint
                {
                    Color = isSelected ? new SKColor(0x21, 0x96, 0xF3) : SKColors.OrangeRed,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                canvas.DrawCircle(screenPoint, isSelected ? 8 : 5, pointPaint);

                using var outlinePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1,
                    IsAntialias = true
                };
                canvas.DrawCircle(screenPoint, isSelected ? 8 : 5, outlinePaint);
            }
            index++;
        }
    }

    private void DrawExaggerationLabel(SKCanvas canvas, SKImageInfo info, double factor)
    {
        using var font = new SKFont(SKTypeface.Default, 14);
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        string ratio = FormatAsRatio(factor);
        string text = $"Верт. масштаб {ratio}";
        canvas.DrawText(text, 10, info.Height - 10, font, textPaint);
    }

    /// <summary>
    /// Преобразует коэффициент (кратный 0.5) в инженерную запись отношения,
    /// например 1.5 -> "3:2", 2.0 -> "2:1", 2.5 -> "5:2".
    /// </summary>
    private static string FormatAsRatio(double factor)
    {
        // Приводим к дроби со знаменателем 2 (т.к. шаг 0.5)
        int numerator = (int)Math.Round(factor * 2);
        int denominator = 2;

        // Сокращаем дробь, если возможно (например, 4/2 -> 2/1)
        int gcd = Gcd(numerator, denominator);
        numerator /= gcd;
        denominator /= gcd;

        return $"{numerator}:{denominator}";
    }

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType == SKTouchAction.Pressed)
        {
            const float hitRadius = 15f;

            for (int i = 0; i < _screenTablePoints.Count; i++)
            {
                if (Distance(_screenTablePoints[i], e.Location) <= hitRadius)
                {
                    PointSelected?.Invoke(i);
                    break;
                }
            }
        }

        e.Handled = true;
    }

    private static float Distance(SKPoint a, SKPoint b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}