using System;
using System.Globalization;
using Afterglow.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Afterglow.Views.Controls;

/// <summary>
/// Plots the color temperature over a full day (00:00 to 24:00), with a marker for the current
/// time and a color-coded bar of the cycle stages along the bottom edge.
/// Hovering or dragging across the graph shows the time and values at that point.
/// </summary>
public class CycleGraph : Control
{
    // Room at the top for the hover readout
    private const double PlotTopPadding = 30;

    // Room at the bottom for the in-graph caption and the stage bar
    private const double PlotBottomPadding = 34;

    private const double StageBarHeight = 6;

    public static readonly StyledProperty<CycleCurve?> CurveProperty = AvaloniaProperty.Register<
        CycleGraph,
        CycleCurve?
    >(nameof(Curve));

    // Current time of day, as a fraction of the day
    public static readonly StyledProperty<double> NowProperty = AvaloniaProperty.Register<
        CycleGraph,
        double
    >(nameof(Now));

    public static readonly StyledProperty<IBrush?> CurveBrushProperty = AvaloniaProperty.Register<
        CycleGraph,
        IBrush?
    >(nameof(CurveBrush), new SolidColorBrush(Color.Parse("#3B464E")));

    public static readonly StyledProperty<IBrush?> WaveBrushProperty = AvaloniaProperty.Register<
        CycleGraph,
        IBrush?
    >(nameof(WaveBrush), new SolidColorBrush(Color.Parse("#3B464E"), 0.045));

    public static readonly StyledProperty<IBrush?> SunBrushProperty = AvaloniaProperty.Register<
        CycleGraph,
        IBrush?
    >(nameof(SunBrush), new SolidColorBrush(Color.Parse("#F5821F")));

    public static readonly StyledProperty<IBrush?> MarkerRingBrushProperty =
        AvaloniaProperty.Register<CycleGraph, IBrush?>(
            nameof(MarkerRingBrush),
            new SolidColorBrush(Colors.White)
        );

    public static readonly StyledProperty<IBrush?> ReadoutBackgroundProperty =
        AvaloniaProperty.Register<CycleGraph, IBrush?>(
            nameof(ReadoutBackground),
            new SolidColorBrush(Color.Parse("#3B464E"), 0.92)
        );

    public static readonly StyledProperty<IBrush?> ReadoutForegroundProperty =
        AvaloniaProperty.Register<CycleGraph, IBrush?>(
            nameof(ReadoutForeground),
            new SolidColorBrush(Colors.White)
        );

    public static readonly StyledProperty<IBrush?> DayBrushProperty = AvaloniaProperty.Register<
        CycleGraph,
        IBrush?
    >(nameof(DayBrush), new SolidColorBrush(Color.Parse("#5B9BD5")));

    public static readonly StyledProperty<IBrush?> EveningBrushProperty = AvaloniaProperty.Register<
        CycleGraph,
        IBrush?
    >(nameof(EveningBrush), new SolidColorBrush(Color.Parse("#F4B942")));

    public static readonly StyledProperty<IBrush?> NightBrushProperty = AvaloniaProperty.Register<
        CycleGraph,
        IBrush?
    >(nameof(NightBrush), new SolidColorBrush(Color.Parse("#E85D3F")));

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<CycleGraph>();

    // Rebuilt only when the curve or the size changes
    private StreamGeometry? _curveGeometry;
    private StreamGeometry? _areaGeometry;
    private StreamGeometry? _backWaveGeometry;
    private StreamGeometry? _frontWaveGeometry;
    private Geometry? _aboveCurveGeometry;
    private IBrush? _areaBrush;

    // Inside the area under the curve, the waves are only a faint light texture, so that they
    // don't read as part of what the area shows
    private static readonly IBrush WaveTextureBrush = new ImmutableSolidColorBrush(
        Color.Parse("#F0F2F5"),
        0.04
    );
    private (double Low, double High) _temperatureRange;
    private Size _geometrySize;

    private double? _hoverFraction;
    private bool _isDragging;

    static CycleGraph()
    {
        AffectsRender<CycleGraph>(
            CurveProperty,
            NowProperty,
            CurveBrushProperty,
            WaveBrushProperty,
            SunBrushProperty,
            MarkerRingBrushProperty,
            ReadoutBackgroundProperty,
            ReadoutForegroundProperty,
            DayBrushProperty,
            EveningBrushProperty,
            NightBrushProperty,
            ShadeOpacityProperty,
            FontFamilyProperty
        );

        CursorProperty.OverrideDefaultValue<CycleGraph>(new Cursor(StandardCursorType.Cross));
    }

    // Opacity of the warm shading under the curve (dark backgrounds need less of it)
    public static readonly StyledProperty<double> ShadeOpacityProperty = AvaloniaProperty.Register<
        CycleGraph,
        double
    >(nameof(ShadeOpacity), 1);

    public double ShadeOpacity
    {
        get => GetValue(ShadeOpacityProperty);
        set => SetValue(ShadeOpacityProperty, value);
    }

    public CycleCurve? Curve
    {
        get => GetValue(CurveProperty);
        set => SetValue(CurveProperty, value);
    }

    public double Now
    {
        get => GetValue(NowProperty);
        set => SetValue(NowProperty, value);
    }

    public IBrush? CurveBrush
    {
        get => GetValue(CurveBrushProperty);
        set => SetValue(CurveBrushProperty, value);
    }

    public IBrush? WaveBrush
    {
        get => GetValue(WaveBrushProperty);
        set => SetValue(WaveBrushProperty, value);
    }

    public IBrush? SunBrush
    {
        get => GetValue(SunBrushProperty);
        set => SetValue(SunBrushProperty, value);
    }

    public IBrush? MarkerRingBrush
    {
        get => GetValue(MarkerRingBrushProperty);
        set => SetValue(MarkerRingBrushProperty, value);
    }

    public IBrush? ReadoutBackground
    {
        get => GetValue(ReadoutBackgroundProperty);
        set => SetValue(ReadoutBackgroundProperty, value);
    }

    public IBrush? ReadoutForeground
    {
        get => GetValue(ReadoutForegroundProperty);
        set => SetValue(ReadoutForegroundProperty, value);
    }

    public IBrush? DayBrush
    {
        get => GetValue(DayBrushProperty);
        set => SetValue(DayBrushProperty, value);
    }

    public IBrush? EveningBrush
    {
        get => GetValue(EveningBrushProperty);
        set => SetValue(EveningBrushProperty, value);
    }

    public IBrush? NightBrush
    {
        get => GetValue(NightBrushProperty);
        set => SetValue(NightBrushProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CurveProperty)
            _curveGeometry = null;
    }

    private double PlotTop => PlotTopPadding;

    private double PlotBottom => Math.Max(PlotTop + 1, Bounds.Height - PlotBottomPadding);

    private double StageBarTop => Bounds.Height - StageBarHeight;

    private double GetX(double fraction) => fraction * Bounds.Width;

    private double GetY(double temperature)
    {
        var (low, high) = _temperatureRange;
        var normalized = high > low ? (temperature - low) / (high - low) : 0.5;

        return PlotBottom - normalized * (PlotBottom - PlotTop);
    }

    // Warmer temperatures get warmer (and more opaque) tints in the shaded area under the curve
    private static Color GetShadeColor(double temperature)
    {
        static Color Lerp(Color a, Color b, double t) =>
            Color.FromArgb(
                (byte)(a.A + (b.A - a.A) * t),
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t)
            );

        var cool = Color.FromArgb(28, 0x5B, 0x9B, 0xD5);
        var amber = Color.FromArgb(70, 0xF4, 0xB9, 0x42);
        var ember = Color.FromArgb(80, 0xE8, 0x5D, 0x3F);

        return temperature switch
        {
            >= 6000 => cool,
            >= 4500 => Lerp(amber, cool, (temperature - 4500) / 1500),
            >= 3000 => Lerp(ember, amber, (temperature - 3000) / 1500),
            _ => ember,
        };
    }

    private void EnsureGeometry()
    {
        var size = Bounds.Size;
        if (_curveGeometry is not null && _geometrySize == size)
            return;

        _geometrySize = size;

        var curve = Curve;
        var samples = curve?.Samples;

        // Scale the temperature axis to fit the curve, with some headroom
        var minimum = double.MaxValue;
        var maximum = double.MinValue;
        if (samples is not null)
        {
            foreach (var sample in samples)
            {
                minimum = Math.Min(minimum, sample.Temperature);
                maximum = Math.Max(maximum, sample.Temperature);
            }
        }

        if (minimum > maximum)
            (minimum, maximum) = (1900, 6600);

        _temperatureRange = (Math.Min(1500, minimum - 300), Math.Max(6800, maximum + 200));

        // Background waves (purely decorative)
        _backWaveGeometry = CreateWaveGeometry(size, 1.0, 0.1, 0.32);
        _frontWaveGeometry = CreateWaveGeometry(size, 1.6, 0.55, 0.22);

        var curveGeometry = new StreamGeometry();
        var areaGeometry = new StreamGeometry();

        if (samples is { Count: > 1 })
        {
            using (var context = curveGeometry.Open())
            {
                context.BeginFigure(new Point(0, GetY(samples[0].Temperature)), false);

                for (var i = 1; i < samples.Count; i++)
                {
                    var fraction = (double)i / (samples.Count - 1);
                    context.LineTo(new Point(GetX(fraction), GetY(samples[i].Temperature)));
                }

                context.EndFigure(false);
            }

            using (var context = areaGeometry.Open())
            {
                context.BeginFigure(new Point(0, StageBarTop), true);

                for (var i = 0; i < samples.Count; i++)
                {
                    var fraction = (double)i / (samples.Count - 1);
                    context.LineTo(new Point(GetX(fraction), GetY(samples[i].Temperature)));
                }

                context.LineTo(new Point(size.Width, StageBarTop));
                context.EndFigure(true);
            }

            // Horizontal gradient that follows the temperature along the day
            var areaBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            };

            const int stopCount = 97;
            for (var i = 0; i < stopCount; i++)
            {
                var fraction = (double)i / (stopCount - 1);
                areaBrush.GradientStops.Add(
                    new GradientStop(
                        GetShadeColor(curve!.GetValueAt(fraction).Temperature),
                        fraction
                    )
                );
            }

            _areaBrush = areaBrush;
        }

        _curveGeometry = curveGeometry;
        _areaGeometry = areaGeometry;

        // The space above the curve, where the waves are drawn as they are
        _aboveCurveGeometry = new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(size)),
            areaGeometry
        );
    }

    private void DrawWaves(DrawingContext context, IBrush? brush)
    {
        if (_backWaveGeometry is not null)
            context.DrawGeometry(brush, null, _backWaveGeometry);

        if (_frontWaveGeometry is not null)
            context.DrawGeometry(brush, null, _frontWaveGeometry);
    }

    private StreamGeometry CreateWaveGeometry(
        Size size,
        double periods,
        double phase,
        double relativeAmplitude
    )
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();

        var middle = PlotTop + (PlotBottom - PlotTop) * 0.55;
        var amplitude = (PlotBottom - PlotTop) * relativeAmplitude;

        context.BeginFigure(new Point(0, StageBarTop), true);

        const int segmentCount = 120;
        for (var i = 0; i <= segmentCount; i++)
        {
            var fraction = (double)i / segmentCount;
            var y = middle - amplitude * Math.Cos(2 * Math.PI * (fraction * periods + phase));

            context.LineTo(new Point(GetX(fraction), y));
        }

        context.LineTo(new Point(size.Width, StageBarTop));
        context.EndFigure(true);

        return geometry;
    }

    private IBrush? GetSegmentBrush(CycleCurveSegmentKind kind) =>
        kind switch
        {
            CycleCurveSegmentKind.Day => DayBrush,
            CycleCurveSegmentKind.Night => NightBrush,
            // Evening and transitions share the same warm gold
            _ => EveningBrush,
        };

    private void DrawMarker(
        DrawingContext context,
        Point center,
        IBrush? fill,
        double radius,
        bool hasGlow
    )
    {
        if (hasGlow && fill is ISolidColorBrush solidFill)
        {
            context.DrawEllipse(
                new SolidColorBrush(solidFill.Color, 0.25),
                null,
                center,
                radius + 6,
                radius + 6
            );
        }

        context.DrawEllipse(MarkerRingBrush, null, center, radius + 2, radius + 2);
        context.DrawEllipse(fill, null, center, radius, radius);
    }

    private void DrawReadout(DrawingContext context, double fraction)
    {
        var curve = Curve;
        if (curve is null)
            return;

        var value = curve.GetValueAt(fraction);

        // 24:00 can't be represented as a time of day, so show the last minute instead
        var time = TimeOnly.FromTimeSpan(
            TimeSpan.FromDays(Math.Min(fraction, (24 * 60 - 1) / (24.0 * 60)))
        );

        var x = GetX(fraction);
        var point = new Point(x, GetY(value.Temperature));

        // Vertical guide from the readout down to the stage bar
        context.DrawLine(
            new Pen(CurveBrush, 1, new DashStyle([3, 3], 0)),
            new Point(Math.Round(x) + 0.5, PlotTopPadding - 4),
            new Point(Math.Round(x) + 0.5, StageBarTop)
        );

        DrawMarker(context, point, CurveBrush, 4, false);

        var text = new FormattedText(
            time.ToString("t", CultureInfo.CurrentCulture)
                + "  ·  "
                + value.Temperature.ToString("F0", CultureInfo.CurrentCulture)
                + " K  ·  "
                + value.Brightness.ToString("P0", CultureInfo.CurrentCulture),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle.Normal, FontWeight.Medium),
            12,
            ReadoutForeground
        );

        var boxSize = new Size(text.Width + 16, text.Height + 6);
        var boxX = Math.Clamp(
            x - boxSize.Width / 2,
            4,
            Math.Max(4, Bounds.Width - boxSize.Width - 4)
        );
        var box = new Rect(new Point(boxX, 4), boxSize);

        context.DrawRectangle(ReadoutBackground, null, box, 4, 4);
        context.DrawText(text, new Point(box.X + 8, box.Y + 3));
    }

    public override void Render(DrawingContext context)
    {
        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        EnsureGeometry();

        using (context.PushClip(new Rect(bounds.Size)))
        {
            // Layered background waves, in the space above the curve
            if (_aboveCurveGeometry is not null)
            {
                using (context.PushGeometryClip(_aboveCurveGeometry))
                    DrawWaves(context, WaveBrush);
            }
            else
            {
                DrawWaves(context, WaveBrush);
            }

            // Shaded area under the curve, with the waves continuing through it as a faint texture
            if (_areaGeometry is not null && _areaBrush is not null)
            {
                using (context.PushOpacity(ShadeOpacity))
                    context.DrawGeometry(_areaBrush, null, _areaGeometry);

                using (context.PushGeometryClip(_areaGeometry))
                    DrawWaves(context, WaveTextureBrush);
            }

            // Curve
            if (_curveGeometry is not null)
            {
                context.DrawGeometry(
                    null,
                    new Pen(
                        CurveBrush,
                        2.5,
                        lineCap: PenLineCap.Round,
                        lineJoin: PenLineJoin.Round
                    ),
                    _curveGeometry
                );
            }

            // Stage bar along the bottom edge
            if (Curve is { } curve)
            {
                foreach (var segment in curve.Segments)
                {
                    // Snap to whole pixels to keep the edges between segments crisp
                    var left = Math.Round(GetX(segment.Start));
                    var right = Math.Round(GetX(segment.End));
                    if (right <= left)
                        continue;

                    context.FillRectangle(
                        GetSegmentBrush(segment.Kind)!,
                        new Rect(left, StageBarTop, right - left, StageBarHeight)
                    );
                }

                // Sun marker at the current time
                var now = Math.Clamp(Now, 0, 1);
                DrawMarker(
                    context,
                    new Point(GetX(now), GetY(curve.GetValueAt(now).Temperature)),
                    SunBrush,
                    7,
                    true
                );
            }

            if (_hoverFraction is { } hoverFraction)
                DrawReadout(context, hoverFraction);
        }
    }

    private void UpdateHover(PointerEventArgs e)
    {
        var width = Bounds.Width;
        if (width <= 0)
            return;

        _hoverFraction = Math.Clamp(e.GetPosition(this).X / width, 0, 1);
        InvalidateVisual();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        UpdateHover(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        UpdateHover(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        // Keep tracking the pointer while dragging, even outside of the control's bounds
        _isDragging = true;
        e.Pointer.Capture(this);
        UpdateHover(e);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isDragging)
            return;

        _isDragging = false;
        e.Pointer.Capture(null);

        if (!new Rect(Bounds.Size).Contains(e.GetPosition(this)))
            ClearHover();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (!_isDragging)
            ClearHover();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isDragging = false;
    }

    private void ClearHover()
    {
        _hoverFraction = null;
        InvalidateVisual();
    }
}
