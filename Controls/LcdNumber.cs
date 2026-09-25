using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace NewWinampClassic.Controls;

/// <summary>
/// Classic green 7-segment LCD number display, as found in the Winamp main
/// window (time, bitrate, kHz). Renders "88:88"-style strings; supports
/// digits, colon, minus, blank. Off segments stay faintly visible, like a
/// real LCD.
/// </summary>
public sealed class LcdNumber : ContentControl
{
    private readonly StackPanel _root = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
    private readonly List<Rectangle[]> _digitSegments = [];
    private readonly List<FrameworkElement> _colonDots = [];
    private string _rendered = "";

    private static readonly Windows.UI.Color OnColor = Windows.UI.Color.FromArgb(255, 0x2B, 0xE2, 0x2B);
    private static readonly Windows.UI.Color OffColor = Windows.UI.Color.FromArgb(70, 0x14, 0x4A, 0x14);

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(LcdNumber), new PropertyMetadata("", OnTextChanged));

    public static readonly DependencyProperty DigitWidthProperty =
        DependencyProperty.Register(nameof(DigitWidth), typeof(double), typeof(LcdNumber), new PropertyMetadata(22.0, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double DigitWidth
    {
        get => (double)GetValue(DigitWidthProperty);
        set => SetValue(DigitWidthProperty, value);
    }

    public double DigitHeight => DigitWidth * 1.9;

    public LcdNumber()
    {
        IsTabStop = false;
        Content = _root;
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((LcdNumber)d).Rebuild();

    private void Rebuild()
    {
        var text = Text ?? "";
        if (text == _rendered && _root.Children.Count > 0)
            return;
        _rendered = text;

        _root.Children.Clear();
        _digitSegments.Clear();
        _colonDots.Clear();

        foreach (var ch in text)
        {
            if (ch == ':')
            {
                var colon = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = DigitHeight * 0.28,
                };
                for (var i = 0; i < 2; i++)
                {
                    var dot = new Rectangle
                    {
                        Width = DigitWidth * 0.16,
                        Height = DigitWidth * 0.16,
                        Fill = new SolidColorBrush(OnColor),
                        RadiusX = 1,
                        RadiusY = 1,
                    };
                    colon.Children.Add(dot);
                    _colonDots.Add(dot);
                }

                _root.Children.Add(colon);
            }
            else
            {
                _root.Children.Add(BuildDigit(ch));
            }
        }

        UpdateSegments();
    }

    private FrameworkElement BuildDigit(char ch)
    {
        var w = DigitWidth;
        var h = DigitHeight;
        var t = Math.Max(3.0, w * 0.20);          // segment thickness
        var g = t * 0.55;                          // gap inset

        var canvas = new Grid { Width = w, Height = h, VerticalAlignment = VerticalAlignment.Center };

        Rectangle Seg(double x, double y, double width, double height)
        {
            var r = new Rectangle
            {
                Margin = new Thickness(x, y, 0, 0),
                Width = width,
                Height = height,
                RadiusX = 1,
                RadiusY = 1,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Fill = new SolidColorBrush(OffColor),
            };
            canvas.Children.Add(r);
            return r;
        }

        // seven segments
        var a = Seg(g, 0, w - 2 * g, t);                            // top
        var b = Seg(w - t, g, t, h / 2 - 1.5 * g);                  // top-right
        var c = Seg(w - t, h / 2 + 0.5 * g, t, h / 2 - 1.5 * g);    // bottom-right
        var d = Seg(g, h - t, w - 2 * g, t);                        // bottom
        var e = Seg(0, h / 2 + 0.5 * g, t, h / 2 - 1.5 * g);        // bottom-left
        var f = Seg(0, g, t, h / 2 - 1.5 * g);                      // top-left
        var gg = Seg(g, (h - t) / 2, w - 2 * g, t);                 // middle

        _digitSegments.Add([a, b, c, d, e, f, gg]);
        return canvas;
    }

    /// <summary>Which segments light up per character (a,b,c,d,e,f,g).</summary>
    private static readonly Dictionary<char, string> SegmentMap = new()
    {
        ['0'] = "abcdef",
        ['1'] = "bc",
        ['2'] = "abdeg",
        ['3'] = "abcdg",
        ['4'] = "bcfg",
        ['5'] = "acdfg",
        ['6'] = "acdefg",
        ['7'] = "abc",
        ['8'] = "abcdefg",
        ['9'] = "abcdfg",
        ['-'] = "g",
        [' '] = "",
    };

    private void UpdateSegments()
    {
        var idx = 0;
        foreach (var ch in _rendered)
        {
            if (ch == ':')
                continue;

            if (idx >= _digitSegments.Count)
                break;

            var segs = _digitSegments[idx];
            var on = SegmentMap.TryGetValue(ch, out var m) ? m : "";
            var names = "abcdefg";
            for (var i = 0; i < segs.Length; i++)
            {
                var isOn = on.Contains(names[i]);
                segs[i].Fill = new SolidColorBrush(isOn ? OnColor : OffColor);
            }
            idx++;
        }
    }

}
