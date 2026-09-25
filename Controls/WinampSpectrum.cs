using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace NewWinampClassic.Controls;

/// <summary>
/// Classic Winamp spectrum analyzer: segmented bars (green at the bottom,
/// yellow, red at the top) with falling white peak caps on a black LCD
/// background. Feed it FFT frames via <see cref="UpdateSpectrum"/>.
/// </summary>
public sealed class WinampSpectrum : Canvas
{
    private const int BarCount = 24;
    private const double CellH = 4.0;     // segment cell height
    private const double CellGap = 1.0;   // gap between cells
    private const double BarGap = 8.0;    // gap between bars

    private readonly float[] _levels = new float[BarCount];
    private readonly float[] _peaks = new float[BarCount];
    private readonly float[] _barMax = new float[BarCount];  // adaptive normalization
    private readonly Rectangle?[] _cells = new Rectangle?[BarCount * 32];
    private readonly Rectangle[] _caps = new Rectangle[BarCount];
    private readonly DispatcherQueueTimer _timer;

    private int _cellCount;

    public WinampSpectrum()
    {
        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 8, 20, 8));
        // Build outside the layout pass — creating children during layout
        // risks re-entrant layout.
        SizeChanged += (_, _) => DispatcherQueue.TryEnqueue(BuildBars);

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33);
        _timer.Tick += (_, _) => Decay();
        _timer.Start();
    }

    private void BuildBars()
    {
        if (ActualWidth < 10 || ActualHeight < 10)
            return;

        Children.Clear();
        Array.Clear(_cells);
        _cellCount = Math.Clamp((int)(ActualHeight / (CellH + CellGap)), 4, 32);
        var barW = Math.Max(6.0, (ActualWidth - (BarCount - 1) * BarGap - 8) / BarCount);

        for (var bar = 0; bar < BarCount; bar++)
        {
            var x = 4 + bar * (barW + BarGap);
            for (var cell = 0; cell < _cellCount; cell++)
            {
                var level = (double)cell / (_cellCount - 1); // 0 bottom .. 1 top
                var rect = new Rectangle
                {
                    Width = barW,
                    Height = CellH,
                    Fill = new SolidColorBrush(CellColor(level)),
                    RadiusX = 1,
                    RadiusY = 1,
                    Opacity = 0.0,
                };
                var y = ActualHeight - 2 - (cell + 1) * (CellH + CellGap);
                SetLeft(rect, x);
                SetTop(rect, y);
                Children.Add(rect);
                _cells[bar * 32 + cell] = rect;
            }

            var cap = new Rectangle
            {
                Width = barW,
                Height = 2,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(230, 0xE8, 0xF0, 0xE8)),
                Opacity = 0.0,
            };
            SetLeft(cap, x);
            SetTop(cap, ActualHeight - 4);
            Children.Add(cap);
            _caps[bar] = cap;
        }
    }

    private static Windows.UI.Color CellColor(double level)
    {
        if (level < 0.62) return Windows.UI.Color.FromArgb(255, 0x00, 0xD8, 0x00);
        if (level < 0.85) return Windows.UI.Color.FromArgb(255, 0xD8, 0xD8, 0x00);
        return Windows.UI.Color.FromArgb(255, 0xD8, 0x18, 0x18);
    }

    /// <summary>
    /// Push one frame of FFT magnitudes. Call on the UI thread.
    /// </summary>
    public void UpdateSpectrum(float[] mags)
    {
        if (_cellCount == 0)
            return;

        var bins = mags.Length;
        for (var bar = 0; bar < BarCount; bar++)
        {
            // geometric bin range, bass-heavy like the classic analyzer
            var lo = (int)Math.Pow(bins - 1, (double)bar / BarCount) + 1;
            var hi = (int)Math.Pow(bins - 1, (double)(bar + 1) / BarCount) + 1;
            if (hi <= lo) hi = lo + 1;
            if (hi > bins) hi = bins;

            float mag = 0;
            for (var i = lo; i < hi; i++)
            {
                if (mags[i] > mag) mag = mags[i];
            }

            // adaptive normalization: track the loudest this bar has been
            _barMax[bar] = Math.Max(_barMax[bar] * 0.995f, mag);
            var norm = _barMax[bar] > 1e-6f ? mag / _barMax[bar] : 0f;
            norm = (float)Math.Clamp(norm * 1.15, 0.0, 1.0);

            _levels[bar] = Math.Max(_levels[bar], norm);
            _peaks[bar] = Math.Max(_peaks[bar], norm);
        }

        Paint();
    }

    private void Decay()
    {
        for (var bar = 0; bar < BarCount; bar++)
        {
            _levels[bar] *= 0.80f;
            _peaks[bar] = MathF.Max(0f, _peaks[bar] - 0.022f);
        }
        Paint();
    }

    private void Paint()
    {
        if (_cellCount == 0)
            return;

        for (var bar = 0; bar < BarCount; bar++)
        {
            var lit = (int)MathF.Round(_levels[bar] * (_cellCount - 1));
            for (var cell = 0; cell < _cellCount; cell++)
            {
                if (_cells[bar * 32 + cell] is { } rect)
                    rect.Opacity = cell <= lit ? 1.0 : 0.0;
            }

            var cap = _caps[bar];
            if (cap is not null)
            {
                var peakCell = (int)MathF.Round(_peaks[bar] * (_cellCount - 1));
                if (_peaks[bar] > 0.02f)
                {
                    cap.Opacity = 1.0;
                    SetTop(cap, Math.Max(0, ActualHeight - 2 - (peakCell + 2) * (CellH + CellGap)));
                }
                else
                {
                    cap.Opacity = 0.0;
                }
            }
        }
    }

    public void Reset()
    {
        Array.Clear(_levels);
        Array.Clear(_peaks);
        Array.Clear(_barMax);
        Paint();
    }
}
