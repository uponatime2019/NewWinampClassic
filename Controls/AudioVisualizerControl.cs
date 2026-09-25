using Microsoft.UI;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace NewWinampClassic.Controls;

/// <summary>
/// Renders real-time audio visualization with 11 modes.
/// Call <see cref="UpdateSpectrum"/> with FFT magnitude data to animate.
/// </summary>
public sealed class AudioVisualizerControl : Grid
{
    private const int BarCount = 32;
    private const double BarMaxHeight = 300;
    private const double BarMinHeight = 2;
    private const double BarWidth = 8;
    private const double BarSpacing = 4;

    private readonly Rectangle[] _bars = new Rectangle[BarCount];
    private readonly double[] _peakValues = new double[BarCount];
    private readonly double[] _peakFall = new double[BarCount];
    private readonly SolidColorBrush _barBrush;
    private readonly SolidColorBrush _barBrush2;
    private readonly SolidColorBrush _barBrush3;
    private readonly SolidColorBrush _mirrorBrush;
    private readonly Panel _barsPanel;
    private readonly Panel _mirrorPanel;
    private readonly Panel _wavePanel;
    private readonly Polyline _waveLine;
    private readonly Panel _circlePanel;
    private readonly Border _circleBorder;
    private bool _isMirrored;
    private bool _isWave;
    private bool _isCircle;

    public static readonly DependencyProperty VisualizerModeProperty =
        DependencyProperty.Register(nameof(VisualizerMode), typeof(int), typeof(AudioVisualizerControl),
            new PropertyMetadata(0, OnModeChanged));

    public int VisualizerMode
    {
        get => (int)GetValue(VisualizerModeProperty);
        set => SetValue(VisualizerModeProperty, value);
    }

    private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizerControl ctrl)
            ctrl.ApplyMode((int)e.NewValue);
    }

    public AudioVisualizerControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        var accent = (Color)Application.Current.Resources["SystemAccentColor"];
        _barBrush = new SolidColorBrush(accent);
        _barBrush2 = new SolidColorBrush(Color.FromArgb(255,
            (byte)Math.Min(255, accent.R + 40),
            (byte)Math.Min(255, accent.G + 40),
            (byte)Math.Min(255, accent.B + 40)));
        _barBrush3 = new SolidColorBrush(Color.FromArgb(255,
            (byte)Math.Min(255, accent.R + 80),
            (byte)Math.Min(255, accent.G + 80),
            (byte)Math.Min(255, accent.B + 80)));
        _mirrorBrush = new SolidColorBrush(Color.FromArgb(180, accent.R, accent.G, accent.B));

        // Main bars panel (bottom-aligned)
        _barsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Spacing = BarSpacing
        };

        for (var i = 0; i < BarCount; i++)
        {
            var bar = new Rectangle
            {
                Width = BarWidth,
                Height = BarMinHeight,
                RadiusX = 3,
                RadiusY = 3,
                Fill = _barBrush,
                VerticalAlignment = VerticalAlignment.Bottom,
                Opacity = 0.9
            };
            _bars[i] = bar;
            _barsPanel.Children.Add(bar);
        }

        Children.Add(_barsPanel);

        // Mirror panel (top-aligned, for mirrored modes)
        _mirrorPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Spacing = BarSpacing,
            Visibility = Visibility.Collapsed
        };
        var mirrorBars = new Rectangle[BarCount];
        for (var i = 0; i < BarCount; i++)
        {
            mirrorBars[i] = new Rectangle
            {
                Width = BarWidth,
                Height = BarMinHeight,
                RadiusX = 3,
                RadiusY = 3,
                Fill = _mirrorBrush,
                VerticalAlignment = VerticalAlignment.Top,
                Opacity = 0.4
            };
        }
        // Store mirror bars reference via Tag for UpdateSpectrum
        _mirrorPanel.Tag = mirrorBars;
        Children.Add(_mirrorPanel);

        // Wave line (for wave modes)
        _wavePanel = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed
        };
        _waveLine = new Polyline
        {
            Stroke = _barBrush,
            StrokeThickness = 3,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        _wavePanel.Children.Add(_waveLine);
        Children.Add(_wavePanel);

        // Circle visualizer
        _circlePanel = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        _circleBorder = new Border
        {
            Width = 300,
            Height = 300,
            CornerRadius = new CornerRadius(150),
            BorderThickness = new Thickness(6),
            BorderBrush = _barBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _circlePanel.Children.Add(_circleBorder);
        Children.Add(_circlePanel);
    }

    private void ApplyMode(int mode)
    {
        // Hide all
        _barsPanel.Visibility = Visibility.Collapsed;
        _mirrorPanel.Visibility = Visibility.Collapsed;
        _wavePanel.Visibility = Visibility.Collapsed;
        _circlePanel.Visibility = Visibility.Collapsed;
        _isMirrored = false;
        _isWave = false;
        _isCircle = false;

        switch (mode)
        {
            case 0: // Classic Bars
                _barsPanel.Visibility = Visibility.Visible;
                SetBarStyle(BarWidth, 3);
                break;
            case 1: // Wide Bars
                _barsPanel.Visibility = Visibility.Visible;
                SetBarStyle(16, 4);
                break;
            case 2: // Thin Lines
                _barsPanel.Visibility = Visibility.Visible;
                SetBarStyle(2, 0);
                break;
            case 3: // Mirror
                _barsPanel.Visibility = Visibility.Visible;
                _mirrorPanel.Visibility = Visibility.Visible;
                _isMirrored = true;
                SetBarStyle(BarWidth, 3);
                break;
            case 4: // Wave
                _wavePanel.Visibility = Visibility.Visible;
                _isWave = true;
                break;
            case 5: // Circle
                _circlePanel.Visibility = Visibility.Visible;
                _isCircle = true;
                break;
            case 6: // Gradient Bars
                _barsPanel.Visibility = Visibility.Visible;
                ApplyGradientBars();
                break;
            case 7: // Rounded Thick
                _barsPanel.Visibility = Visibility.Visible;
                SetBarStyle(10, 5);
                break;
            case 8: // Mirror Thin
                _barsPanel.Visibility = Visibility.Visible;
                _mirrorPanel.Visibility = Visibility.Visible;
                _isMirrored = true;
                SetBarStyle(2, 0);
                break;
            case 9: // Dots
                _barsPanel.Visibility = Visibility.Visible;
                SetBarStyle(6, 3);
                foreach (var bar in _bars) bar.Opacity = 0.7;
                break;
            case 10: // Neon Glow
                _barsPanel.Visibility = Visibility.Visible;
                SetBarStyle(4, 2);
                foreach (var bar in _bars)
                {
                    bar.Opacity = 1.0;
                    bar.Stroke = _barBrush;
                    bar.StrokeThickness = 2;
                }
                break;
            default:
                _barsPanel.Visibility = Visibility.Visible;
                SetBarStyle(BarWidth, 3);
                break;
        }
    }

    private void SetBarStyle(double width, double radius)
    {
        for (var i = 0; i < BarCount; i++)
        {
            _bars[i].Width = width;
            _bars[i].RadiusX = radius;
            _bars[i].RadiusY = radius;
            _bars[i].Fill = _barBrush;
            _bars[i].Stroke = null;
            _bars[i].StrokeThickness = 0;
            _bars[i].Opacity = 0.9;
        }
        // Update mirror panel bars too
        if (_mirrorPanel.Tag is Rectangle[] mirrors)
        {
            for (var i = 0; i < BarCount; i++)
            {
                mirrors[i].Width = width;
                mirrors[i].RadiusX = radius;
                mirrors[i].RadiusY = radius;
            }
        }
    }

    private void ApplyGradientBars()
    {
        var accent = (Color)Application.Current.Resources["SystemAccentColor"];
        for (var i = 0; i < BarCount; i++)
        {
            var t = i / (double)(BarCount - 1);
            var r = (byte)(accent.R + (255 - accent.R) * t);
            var g = (byte)(accent.G * (1 - t * 0.5));
            var b = (byte)(accent.B + (255 - accent.B) * t);
            _bars[i].Fill = new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }
    }

    public void UpdateSpectrum(float[] fftData)
    {
        if (fftData == null || fftData.Length < 2) return;

        var values = ComputeMagnitudes(fftData);

        if (_isWave)
        {
            UpdateWave(values);
            return;
        }

        if (_isCircle)
        {
            UpdateCircle(values);
            return;
        }

        for (var i = 0; i < BarCount; i++)
        {
            var normalized = values[i];
            if (normalized > _peakValues[i])
            {
                _peakValues[i] = normalized;
                _peakFall[i] = 0;
            }
            else
            {
                _peakFall[i] += 0.02;
                _peakValues[i] = Math.Max(0, Math.Max(normalized, _peakValues[i] - _peakFall[i]));
            }

            var height = BarMinHeight + (BarMaxHeight - BarMinHeight) * _peakValues[i];
            _bars[i].Height = height;

            // Update mirror bars
            if (_isMirrored && _mirrorPanel.Tag is Rectangle[] mirrors)
                mirrors[i].Height = height;
        }
    }

    private double[] ComputeMagnitudes(float[] fftData)
    {
        var halfLength = fftData.Length;
        var usableBins = Math.Min(halfLength, 256);
        var values = new double[BarCount];

        for (var i = 0; i < BarCount; i++)
        {
            var logMin = Math.Log(1);
            var logMax = Math.Log(usableBins);
            var logIndex = logMin + (logMax - logMin) * i / BarCount;
            var binStart = (int)Math.Pow(Math.E, logIndex);
            var logIndexNext = logMin + (logMax - logMin) * (i + 1) / BarCount;
            var binEnd = Math.Min((int)Math.Pow(Math.E, logIndexNext), usableBins);

            float sum = 0;
            var count = Math.Max(1, binEnd - binStart);
            for (var j = binStart; j < binEnd; j++)
                sum += fftData[j];
            var avg = sum / count;
            values[i] = Math.Min(1.0, avg / 400.0);
        }

        return values;
    }

    private void UpdateWave(double[] values)
    {
        var pc = new PointCollection();
        for (var i = 0; i < BarCount; i++)
        {
            var x = i * (ActualWidth / (BarCount - 1));
            var y = BarMaxHeight - (BarMaxHeight * values[i]);
            pc.Add(new Point(x, ActualHeight / 2 + y / 4));
            pc.Add(new Point(x, ActualHeight / 2 - y / 4));
        }
        _waveLine.Points = pc;
    }

    private void UpdateCircle(double[] values)
    {
        var avg = values.Sum() / values.Length;
        var scale = 1.0 + avg * 0.4;
        _circleBorder.Width = 300 * scale;
        _circleBorder.Height = 300 * scale;
        _circleBorder.Opacity = 0.5 + avg * 0.5;
    }

    public void Reset()
    {
        for (var i = 0; i < BarCount; i++)
        {
            _peakValues[i] = 0;
            _peakFall[i] = 0;
            _bars[i].Height = BarMinHeight;
        }
        if (_mirrorPanel.Tag is Rectangle[] mirrors)
        {
            for (var i = 0; i < BarCount; i++)
                mirrors[i].Height = BarMinHeight;
        }
    }
}
