using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace NewWinampClassic.Controls;

/// <summary>
/// Hand-drawn Winamp-style slider used for the seek bar, volume, balance
/// and EQ faders: dark groove, optional green fill, bevelled metal thumb.
/// All runtime visual updates use Canvas positioning only (never child
/// size/alignment changes during layout) to avoid re-entrant layout.
/// Click-to-jump plus drag; raises <see cref="Commit"/> on pointer release.
/// </summary>
public sealed class ClassicSlider : ContentControl
{
    private readonly Grid _root;
    private readonly Canvas _track;
    private readonly Border _groove;
    private readonly Rectangle _fill;
    private readonly Border _thumb;
    private bool _dragging;

    public event EventHandler<double>? ValueChanged;
    public event EventHandler<double>? Commit;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(ClassicSlider), new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(ClassicSlider), new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ClassicSlider), new PropertyMetadata(100.0, OnValueChanged));

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(nameof(Step), typeof(double), typeof(ClassicSlider), new PropertyMetadata(0.0));

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(ClassicSlider), new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

    public static readonly DependencyProperty ShowFillProperty =
        DependencyProperty.Register(nameof(ShowFill), typeof(bool), typeof(ClassicSlider), new PropertyMetadata(true, OnOrientationChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, Coerce(Snap(value)));
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public bool ShowFill
    {
        get => (bool)GetValue(ShowFillProperty);
        set => SetValue(ShowFillProperty, value);
    }

    public ClassicSlider()
    {
        IsTabStop = true;

        _fill = new Rectangle
        {
            Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x1E, 0xD0, 0x1E)),
            RadiusX = 1,
            RadiusY = 1,
        };

        _groove = new Border
        {
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x14, 0x14, 0x18)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Background = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1),
                GradientStops =
                {
                    new GradientStop { Color = Windows.UI.Color.FromArgb(255, 0x1E, 0x24, 0x1E), Offset = 0 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(255, 0x0A, 0x12, 0x0A), Offset = 1 },
                },
            },
        };

        _thumb = new Border
        {
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x22, 0x22, 0x28)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Background = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1),
                GradientStops =
                {
                    new GradientStop { Color = Windows.UI.Color.FromArgb(255, 0x8A, 0x8A, 0x94), Offset = 0 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(255, 0x54, 0x54, 0x5C), Offset = 0.5 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(255, 0x3C, 0x3C, 0x44), Offset = 1 },
                },
            },
        };

        _track = new Canvas();
        _track.Children.Add(_fill);
        _track.Children.Add(_thumb);

        _root = new Grid();
        _root.Children.Add(_groove);
        _root.Children.Add(_track);
        Content = _root;

        UpdateLayoutFor(Orientation);

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCanceled += OnPointerReleased;
        // Canvas.SetLeft/SetTop updates don't invalidate measure, so this is
        // safe to run from a layout-time event.
        SizeChanged += (_, _) => PositionVisual();
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _dragging = true;
        CapturePointer(e.Pointer);
        UpdateFromPoint(e);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_dragging)
        {
            UpdateFromPoint(e);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
            return;
        _dragging = false;
        ReleasePointerCapture(e.Pointer);
        Commit?.Invoke(this, Value);
        e.Handled = true;
    }

    private void UpdateFromPoint(PointerRoutedEventArgs e)
    {
        var p = e.GetCurrentPoint(this);
        double ratio;
        if (Orientation == Orientation.Horizontal)
        {
            var thumb = _thumb.ActualWidth > 0 ? _thumb.ActualWidth / 2 : 12;
            ratio = (p.Position.X - thumb) / Math.Max(1, ActualWidth - thumb * 2);
        }
        else
        {
            var thumb = _thumb.ActualHeight > 0 ? _thumb.ActualHeight / 2 : 12;
            ratio = 1 - (p.Position.Y - thumb) / Math.Max(1, ActualHeight - thumb * 2);
        }
        ratio = Math.Clamp(ratio, 0, 1);
        Value = Coerce(Snap(Minimum + ratio * (Maximum - Minimum)));
    }

    private double Snap(double v)
    {
        if (Step <= 0)
            return v;
        return Math.Round(v / Step) * Step;
    }

    private double Coerce(double v) => Math.Clamp(v, Math.Min(Minimum, Maximum), Math.Max(Minimum, Maximum));

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var s = (ClassicSlider)d;
        s.PositionVisual();
        s.ValueChanged?.Invoke(s, (double)e.NewValue);
    }

    private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var s = (ClassicSlider)d;
        s.UpdateLayoutFor(s.Orientation);
    }

    private void UpdateLayoutFor(Orientation o)
    {
        if (o == Orientation.Horizontal)
        {
            _groove.Height = 6;
            _groove.HorizontalAlignment = HorizontalAlignment.Stretch;
            _groove.VerticalAlignment = VerticalAlignment.Center;
            _groove.Margin = new Thickness(2, 0, 2, 0);
            _fill.Height = 4;
            _fill.Width = 0;
            _thumb.Width = 26;
            _thumb.Height = 14;
        }
        else
        {
            _groove.Width = 6;
            _groove.HorizontalAlignment = HorizontalAlignment.Center;
            _groove.VerticalAlignment = VerticalAlignment.Stretch;
            _groove.Margin = new Thickness(0, 2, 0, 2);
            _fill.Width = 4;
            _fill.Height = 0;
            _thumb.Width = 14;
            _thumb.Height = 26;
        }
        _fill.Visibility = ShowFill ? Visibility.Visible : Visibility.Collapsed;
        PositionVisual();
    }

    private void PositionVisual()
    {
        var range = Maximum - Minimum;
        var ratio = range > 0 ? Math.Clamp((Value - Minimum) / range, 0, 1) : 0;

        if (Orientation == Orientation.Horizontal)
        {
            var w = ActualWidth;
            var fillW = Math.Max(0, (w - 4) * ratio);
            _fill.Width = fillW;
            Canvas.SetLeft(_fill, 2);
            Canvas.SetTop(_fill, (ActualHeight - _fill.Height) / 2);

            var x = Math.Max(0, Math.Min(Math.Max(0, w - _thumb.Width), (w - _thumb.Width) * ratio));
            Canvas.SetLeft(_thumb, x);
            Canvas.SetTop(_thumb, (ActualHeight - _thumb.Height) / 2);
        }
        else
        {
            var h = ActualHeight;
            var fillH = Math.Max(0, (h - 4) * ratio);
            _fill.Height = fillH;
            Canvas.SetLeft(_fill, (ActualWidth - _fill.Width) / 2);
            Canvas.SetTop(_fill, h - 2 - fillH);

            var y = Math.Max(0, Math.Min(Math.Max(0, h - _thumb.Height), (h - _thumb.Height) * (1 - ratio)));
            Canvas.SetLeft(_thumb, (ActualWidth - _thumb.Width) / 2);
            Canvas.SetTop(_thumb, y);
        }
    }
}
