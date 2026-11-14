using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace LightBulb.Views.Controls;

/// <summary>
/// Horizontal slider for picking a color temperature, with the value shown underneath the thumb.
/// </summary>
public class TemperatureSlider : Control
{
    private const double ThumbRadius = 12;
    private const double TrackThickness = 6;
    private const double LabelGap = 4;

    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<
        TemperatureSlider,
        double
    >(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<
        TemperatureSlider,
        double
    >(nameof(Minimum), 1900);

    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<
        TemperatureSlider,
        double
    >(nameof(Maximum), 6600);

    public static readonly StyledProperty<double> StepProperty = AvaloniaProperty.Register<
        TemperatureSlider,
        double
    >(nameof(Step), 50);

    public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<
        TemperatureSlider,
        IBrush?
    >(nameof(TrackBrush), new SolidColorBrush(Color.Parse("#6C8CD5")));

    public static readonly StyledProperty<IBrush?> ThumbBorderBrushProperty =
        AvaloniaProperty.Register<TemperatureSlider, IBrush?>(
            nameof(ThumbBorderBrush),
            new SolidColorBrush(Color.Parse("#B9C1CC"))
        );

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<TemperatureSlider>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<TemperatureSlider>();

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<TemperatureSlider>();

    private bool _isDragging;
    private bool _isPointerOver;

    static TemperatureSlider()
    {
        AffectsRender<TemperatureSlider>(
            ValueProperty,
            MinimumProperty,
            MaximumProperty,
            TrackBrushProperty,
            ThumbBorderBrushProperty,
            ForegroundProperty,
            FontSizeProperty,
            FontFamilyProperty
        );

        AffectsMeasure<TemperatureSlider>(FontSizeProperty, FontFamilyProperty);

        FocusableProperty.OverrideDefaultValue<TemperatureSlider>(true);
        CursorProperty.OverrideDefaultValue<TemperatureSlider>(new Cursor(StandardCursorType.Hand));
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? ThumbBorderBrush
    {
        get => GetValue(ThumbBorderBrushProperty);
        set => SetValue(ThumbBorderBrushProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    // Horizontal extent of the track; inset so that the thumb is never clipped at the edges
    private double TrackStart => ThumbRadius + 2;

    private double TrackEnd => Math.Max(TrackStart, Bounds.Width - ThumbRadius - 2);

    // Leave room for the thumb's drop shadow above the track
    private double TrackCenterY => ThumbRadius + 2;

    private double ValueFraction =>
        Maximum > Minimum ? Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1) : 0;

    private FormattedText CreateLabel() =>
        new(
            Value.ToString("F0", CultureInfo.CurrentCulture) + "K",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle.Normal, FontWeight.Medium),
            FontSize,
            Foreground
        );

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width;
        var height = TrackCenterY + ThumbRadius + LabelGap + CreateLabel().Height;

        return new Size(width, height);
    }

    public override void Render(DrawingContext context)
    {
        var trackStart = TrackStart;
        var trackEnd = TrackEnd;
        var centerY = TrackCenterY;

        // Track
        context.DrawLine(
            new Pen(TrackBrush, TrackThickness, lineCap: PenLineCap.Round),
            new Point(trackStart, centerY),
            new Point(trackEnd, centerY)
        );

        var thumbCenter = new Point(trackStart + (trackEnd - trackStart) * ValueFraction, centerY);
        var radius = _isDragging || _isPointerOver ? ThumbRadius + 1 : ThumbRadius;

        // Soft drop shadow, layered to fake a blur
        for (var i = 3; i >= 1; i--)
        {
            context.DrawEllipse(
                new SolidColorBrush(Color.FromArgb((byte)(18 * (4 - i)), 0, 0, 0)),
                null,
                thumbCenter + new Point(0, 1.5),
                radius + i,
                radius + i
            );
        }

        // Thumb, shaded from white at the top to light gray at the bottom
        var thumbBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.Parse("#FFFFFF"), 0),
                new GradientStop(Color.Parse("#E4E7EB"), 1),
            },
        };

        context.DrawEllipse(thumbBrush, new Pen(ThumbBorderBrush, 1), thumbCenter, radius, radius);

        // Keyboard focus indicator
        if (IsFocused && IsKeyboardFocusWithin && !_isDragging)
        {
            context.DrawEllipse(null, new Pen(TrackBrush, 2), thumbCenter, radius + 4, radius + 4);
        }

        // Value label, centered under the thumb but kept within the control's bounds
        var label = CreateLabel();
        var labelX = Math.Clamp(
            thumbCenter.X - label.Width / 2,
            0,
            Math.Max(0, Bounds.Width - label.Width)
        );

        context.DrawText(label, new Point(labelX, centerY + ThumbRadius + LabelGap));
    }

    private void SetValueFromPosition(double x)
    {
        var trackStart = TrackStart;
        var trackEnd = TrackEnd;
        if (trackEnd <= trackStart)
            return;

        var fraction = Math.Clamp((x - trackStart) / (trackEnd - trackStart), 0, 1);
        SetValueSnapped(Minimum + fraction * (Maximum - Minimum));
    }

    private void SetValueSnapped(double value)
    {
        var step = Step > 0 ? Step : 1;
        var snapped = Math.Round(value / step) * step;

        Value = Math.Clamp(snapped, Minimum, Maximum);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        _isDragging = true;
        e.Pointer.Capture(this);
        Focus(NavigationMethod.Pointer);

        SetValueFromPosition(e.GetPosition(this).X);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDragging)
            SetValueFromPosition(e.GetPosition(this).X);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isDragging)
            return;

        _isDragging = false;
        e.Pointer.Capture(null);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        _isDragging = false;
        InvalidateVisual();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        _isPointerOver = true;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        _isPointerOver = false;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var delta = e.Key switch
        {
            Key.Left or Key.Down => -100,
            Key.Right or Key.Up => 100,
            Key.PageDown => -500,
            Key.PageUp => 500,
            _ => 0,
        };

        if (e.Key == Key.Home)
        {
            SetValueSnapped(Minimum);
            e.Handled = true;
        }
        else if (e.Key == Key.End)
        {
            SetValueSnapped(Maximum);
            e.Handled = true;
        }
        else if (delta != 0)
        {
            SetValueSnapped(Value + delta);
            e.Handled = true;
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        InvalidateVisual();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        InvalidateVisual();
    }
}
