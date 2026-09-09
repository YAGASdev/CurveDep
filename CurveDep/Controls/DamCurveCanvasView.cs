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

    private readonly List<SKPoint> _screenCurvePoints = new();

    public DamCurveCanvasView()
    {
        PaintSurface += OnPaintSurface;
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

        float margin = 50;
        double modelWidth = maxX - minX;
        double modelHeight = maxY;

        float scaleX = (float)((e.Info.Width - 2 * margin) / modelWidth);
        float scaleY = (float)((e.Info.Height - 2 * margin) / modelHeight);
        float scale = Math.Min(scaleX, scaleY);

        SKPoint ToScreen(double x, double y) => new(
            margin + (float)(x - minX) * scale,
            e.Info.Height - margin - (float)y * scale
        );

        DrawDamBody(canvas, geometry, contour, ToScreen);

        // Строим путь контура тела плотины ещё раз — для обрезки (clip)
        using var bodyClipPath = new SKPath();
        bodyClipPath.MoveTo(ToScreen(contour[0].X, contour[0].Y));
        for (int i = 1; i < contour.Count; i++)
            bodyClipPath.LineTo(ToScreen(contour[i].X, contour[i].Y));
        bodyClipPath.Close();

        canvas.Save();
        canvas.ClipPath(bodyClipPath); // всё, что рисуется дальше, обрежется по контуру плотины
        DrawSeepageCurve(canvas, ToScreen);
        canvas.Restore();
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
        _screenCurvePoints.Clear();

        if (CurvePoints == null)
            return;

        foreach (var obj in CurvePoints)
        {
            if (obj is CurvePoint p)
                _screenCurvePoints.Add(toScreen(p.X, p.Hx));
        }

        if (_screenCurvePoints.Count < 2)
            return;

        using var curvePaint = new SKPaint
        {
            Color = SKColors.DarkBlue,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        using var curvePath = new SKPath();
        curvePath.MoveTo(_screenCurvePoints[0]);
        for (int i = 1; i < _screenCurvePoints.Count; i++)
            curvePath.LineTo(_screenCurvePoints[i]);

        canvas.DrawPath(curvePath, curvePaint);
    }
}