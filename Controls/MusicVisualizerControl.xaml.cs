using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;
using Windows.Foundation;

namespace NewWinampClassic.Controls
{
    public sealed partial class MusicVisualizerControl : UserControl
    {
        private DispatcherTimer _animationTimer;
        private int _currentEffect = 0;
        private bool _isPlaying = true;
        private Random _random = new Random();
        private List<Rectangle> _spectrumBars = new List<Rectangle>();
        private List<Ellipse> _particles = new List<Ellipse>();
        private List<TextBlock> _matrixChars = new List<TextBlock>();
        private List<Ellipse> _pulseCircles = new List<Ellipse>();
        private List<Ellipse> _stars = new List<Ellipse>();
        private List<Rectangle> _raindrops = new List<Rectangle>();
        private List<Ellipse> _snowflakes = new List<Ellipse>();
        private List<Polygon> _hearts = new List<Polygon>();
        private List<Polygon> _lightning = new List<Polygon>();
        private List<Rectangle> _colorBoxes = new List<Rectangle>();
        private List<double> _boxScales = new List<double>();
        private List<double> _boxGrowthRates = new List<double>();
        private double _canvasWidth = 800;
        private double _canvasHeight = 600;
        private const int MaxEffects = 31; // All effects including new ones
        private double _speedMultiplier = 1.0;
        private const double MinSpeed = 0.25;
        private const double MaxSpeed = 4.0;
        private const double SpeedIncrement = 0.25;

        public static readonly DependencyProperty VisualizerModeProperty =
            DependencyProperty.Register(nameof(VisualizerMode), typeof(int), typeof(MusicVisualizerControl),
                new PropertyMetadata(0, OnModeChanged));

        public int VisualizerMode
        {
            get => (int)GetValue(VisualizerModeProperty);
            set => SetValue(VisualizerModeProperty, value);
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MusicVisualizerControl ctrl)
            {
                ctrl.CurrentEffect = (int)e.NewValue;
            }
        }

        public int CurrentEffect
        {
            get => _currentEffect;
            set
            {
                if (value >= 0 && value < MaxEffects)
                {
                    _currentEffect = value;
                    if (VisualizerComboBox != null)
                        VisualizerComboBox.SelectedIndex = value;
                    UpdateVisibility();
                }
            }
        }

        public MusicVisualizerControl()
        {
            this.InitializeComponent();
            this.Loaded += MusicVisualizerControl_Loaded;
            this.SizeChanged += MusicVisualizerControl_SizeChanged;
        }

        private void MusicVisualizerControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSpeedSetting();
            UpdateCanvasDimensions();
            InitializeTimer();
            InitializeEffects();
            VisualizerComboBox.SelectedIndex = 0;
            CurrentEffect = 0; // Set default effect
            UpdateSpeedDisplay();
        }

        private void MusicVisualizerControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvasDimensions();
            InitializeEffects();
        }

        private void UpdateCanvasDimensions()
        {
            _canvasWidth = this.ActualWidth > 0 ? this.ActualWidth : 800;
            _canvasHeight = this.ActualHeight > 0 ? this.ActualHeight : 600;

            if (VisualizerCanvas != null)
            {
                VisualizerCanvas.Width = _canvasWidth;
                VisualizerCanvas.Height = _canvasHeight;
            }
        }

        private void InitializeTimer()
        {
            _animationTimer = new DispatcherTimer();
            UpdateTimerInterval();
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();
        }

        private void UpdateTimerInterval()
        {
            if (_animationTimer != null)
            {
                double baseInterval = 150.0; // Base 3x slower interval
                double newInterval = baseInterval / _speedMultiplier;
                _animationTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(10, newInterval));
            }
        }

        private void InitializeEffects()
        {
            CreateSpectrumBars();
            CreateBouncingDots();
            CreateParticles();
            CreateMatrixRain();
            UpdateCircularEffectSizes();

            // Position circular elements at center with scaling
            double circularWaveSize = Math.Min(_canvasWidth, _canvasHeight) * 0.4;
            Canvas.SetLeft(CircularWave, _canvasWidth / 2 - circularWaveSize / 2);
            Canvas.SetTop(CircularWave, _canvasHeight / 2 - circularWaveSize / 2);

            Canvas.SetLeft(RippleEffect, _canvasWidth / 2 - 50);
            Canvas.SetTop(RippleEffect, _canvasHeight / 2 - 50);
        }

        private void UpdateCircularEffectSizes()
        {
            double minDimension = Math.Min(_canvasWidth, _canvasHeight);

            if (OuterCircle != null)
            {
                OuterCircle.Width = minDimension * 0.6;
                OuterCircle.Height = minDimension * 0.6;
            }

            if (MiddleCircle != null)
            {
                MiddleCircle.Width = minDimension * 0.4;
                MiddleCircle.Height = minDimension * 0.4;
            }

            if (InnerCircle != null)
            {
                InnerCircle.Width = minDimension * 0.2;
                InnerCircle.Height = minDimension * 0.2;
            }
        }

        private void CreateSpectrumBars()
        {
            _spectrumBars.Clear();
            SpectrumBarsControl.Items.Clear();

            int barCount = Math.Max(16, (int)(_canvasWidth / 25));
            double barWidth = _canvasWidth / barCount - 2;

            for (int i = 0; i < barCount; i++)
            {
                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = 10,
                    Fill = new SolidColorBrush(GetRandomColor()),
                    Margin = new Thickness(1)
                };

                Canvas.SetLeft(bar, i * (_canvasWidth / barCount));
                Canvas.SetTop(bar, _canvasHeight - 50);

                _spectrumBars.Add(bar);
                SpectrumBarsControl.Items.Add(bar);
            }
        }

        private void CreateBouncingDots()
        {
            DotsCanvas.Children.Clear();

            for (int i = 0; i < 20; i++)
            {
                var dot = new Ellipse
                {
                    Width = 15,
                    Height = 15,
                    Fill = new SolidColorBrush(GetRandomColor())
                };

                Canvas.SetLeft(dot, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(dot, _random.NextDouble() * _canvasHeight);

                DotsCanvas.Children.Add(dot);
            }
        }

        private void CreateParticles()
        {
            _particles.Clear();
            ParticleCanvas.Children.Clear();

            for (int i = 0; i < 50; i++)
            {
                var particle = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = new SolidColorBrush(GetRandomColor()),
                    Opacity = 0.7
                };

                Canvas.SetLeft(particle, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(particle, _random.NextDouble() * _canvasHeight);

                _particles.Add(particle);
                ParticleCanvas.Children.Add(particle);
            }
        }

        private void CreateMatrixRain()
        {
            _matrixChars.Clear();
            MatrixCanvas.Children.Clear();

            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            for (int i = 0; i < 100; i++)
            {
                var textBlock = new TextBlock
                {
                    Text = chars[_random.Next(chars.Length)].ToString(),
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Colors.LimeGreen)
                };

                Canvas.SetLeft(textBlock, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(textBlock, _random.NextDouble() * _canvasHeight);

                _matrixChars.Add(textBlock);
                MatrixCanvas.Children.Add(textBlock);
            }
        }

        private Color GetRandomColor()
        {
            Color[] colors = {
                Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow,
                Colors.Purple, Colors.Orange, Colors.Cyan, Colors.Magenta,
                Colors.LimeGreen, Colors.HotPink, Colors.Gold, Colors.Violet
            };
            return colors[_random.Next(colors.Length)];
        }

        private void AnimationTimer_Tick(object sender, object e)
        {
            if (!_isPlaying) return;

            switch (_currentEffect)
            {
                case 0: AnimateSpectrumBars(); break;
                case 1: AnimateCircularWave(); break;
                case 2: AnimatePulseCircle(); break;
                case 3: AnimateRippleEffect(); break;
                case 4: AnimateColorCycle(); break;
                case 5: AnimateBouncingDots(); break;
                case 6: AnimateSpiral(); break;
                case 7: AnimateParticleSystem(); break;
                case 8: AnimateMatrixRain(); break;
                case 9: AnimateStarlights(); break;
                case 10: AnimateNightRain(); break;
                case 11: AnimateWinterSnow(); break;
                case 12: AnimateFallInLove(); break;
                case 13: AnimateLightningStorm(); break;
                case 14: AnimateOceanWaves(); break;
                case 15: AnimateFireDance(); break;
                case 16: AnimateGalaxySpiral(); break;
                case 17: AnimateNeonPulse(); break;
                case 18: AnimateCrystalFormation(); break;
                case 19: AnimateLaserShow(); break;
                case 20: AnimateDigitalRain(); break;
                case 21: AnimatePlasmaStorm(); break;
                case 22: AnimateAuroraBorealis(); break;
                case 23: AnimateFractalTree(); break;
                case 24: AnimateQuantumField(); break;
                case 25: AnimateSolarFlare(); break;
                case 26: AnimateGeometricPatterns(); break;
                case 27: AnimateCyberGrid(); break;
                case 28: AnimateNebulaCloud(); break;
                case 29: AnimateEnergyVortex(); break;
                case 30: AnimateHologram(); break;
            }
        }

        private void AnimateSpectrumBars()
        {
            double maxHeight = _canvasHeight * 0.8;
            foreach (var bar in _spectrumBars)
            {
                var newHeight = 10 + _random.NextDouble() * maxHeight;
                bar.Height = newHeight;
                Canvas.SetTop(bar, _canvasHeight - newHeight - 20);
                ((SolidColorBrush)bar.Fill).Color = GetRandomColor();
            }
        }

        private void AnimateCircularWave()
        {
            if (CircularWaveStoryboard.GetCurrentState() != ClockState.Active)
                CircularWaveStoryboard.Begin();
        }

        private void AnimatePulseCircle()
        {
            PulseCanvas.Children.Clear();
            _pulseCircles.Clear();

            int circleCount = 8;
            double centerX = _canvasWidth / 2;
            double centerY = _canvasHeight / 2;
            double maxRadius = Math.Min(_canvasWidth, _canvasHeight) * 0.4;

            for (int i = 0; i < circleCount; i++)
            {
                double angle = (360.0 / circleCount) * i * Math.PI / 180;
                double radius = 50 + (_random.NextDouble() * maxRadius);
                double size = 20 + _random.NextDouble() * 60;

                var circle = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(GetRandomColor()),
                    Opacity = 0.3 + _random.NextDouble() * 0.5
                };

                double x = centerX + Math.Cos(angle) * radius - size / 2;
                double y = centerY + Math.Sin(angle) * radius - size / 2;

                Canvas.SetLeft(circle, x);
                Canvas.SetTop(circle, y);

                _pulseCircles.Add(circle);
                PulseCanvas.Children.Add(circle);
            }

            foreach (var circle in _pulseCircles)
            {
                var scaleTransform = new CompositeTransform();
                circle.RenderTransform = scaleTransform;

                double scale = 0.5 + _random.NextDouble() * 1.5;
                scaleTransform.ScaleX = scale;
                scaleTransform.ScaleY = scale;

                ((SolidColorBrush)circle.Fill).Color = GetRandomColor();
            }
        }

        private void AnimateRippleEffect()
        {
            if (RippleStoryboard.GetCurrentState() != ClockState.Active)
            {
                RippleStoryboard.Begin();
            }
            else if (RippleStoryboard.GetCurrentState() == ClockState.Active)
            {
                var currentTime = DateTime.Now.Millisecond;
                if (currentTime % 300 < 50)
                {
                    ((SolidColorBrush)RippleEffect1.Stroke).Color = GetRandomColor();
                }

                var storyboardProgress = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % 3000;
                if (storyboardProgress > 2500 && storyboardProgress < 2800 && _random.NextDouble() < 0.3)
                {
                    SpawnSmallRipple();
                }
            }
        }

        private void SpawnSmallRipple()
        {
            var smallCircle = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(Colors.Transparent),
                Stroke = new SolidColorBrush(GetRandomColor()),
                StrokeThickness = 2,
                Opacity = 0.6,
                RenderTransform = new CompositeTransform { CenterX = 10, CenterY = 10, ScaleX = 0.2, ScaleY = 0.2 }
            };

            double offsetX = (_random.NextDouble() - 0.5) * 100;
            double offsetY = (_random.NextDouble() - 0.5) * 100;

            Canvas.SetLeft(smallCircle, (_canvasWidth / 2) + offsetX - 10);
            Canvas.SetTop(smallCircle, (_canvasHeight / 2) + offsetY - 10);

            RippleEffect.Children.Add(smallCircle);

            var storyboard = new Storyboard();

            var scaleXAnimation = new DoubleAnimation
            {
                From = 0.2,
                To = 2.0,
                Duration = TimeSpan.FromMilliseconds(800)
            };
            Storyboard.SetTarget(scaleXAnimation, smallCircle);
            Storyboard.SetTargetProperty(scaleXAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");

            var scaleYAnimation = new DoubleAnimation
            {
                From = 0.2,
                To = 2.0,
                Duration = TimeSpan.FromMilliseconds(800)
            };
            Storyboard.SetTarget(scaleYAnimation, smallCircle);
            Storyboard.SetTargetProperty(scaleYAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");

            var opacityAnimation = new DoubleAnimation
            {
                From = 0.6,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(800)
            };
            Storyboard.SetTarget(opacityAnimation, smallCircle);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
            storyboard.Children.Add(opacityAnimation);

            storyboard.Begin();

            var removeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            removeTimer.Tick += (s, e) =>
            {
                RippleEffect.Children.Remove(smallCircle);
                removeTimer.Stop();
            };
            removeTimer.Start();
        }

        private void AnimateColorCycle()
        {
            if (_colorBoxes.Count == 0)
            {
                CreateColorBoxes();
            }

            for (int i = _colorBoxes.Count - 1; i >= 0; i--)
            {
                var box = _colorBoxes[i];
                _boxScales[i] += _boxGrowthRates[i];

                var transform = box.RenderTransform as CompositeTransform;
                if (transform == null)
                {
                    transform = new CompositeTransform();
                    box.RenderTransform = transform;
                }
                transform.ScaleX = _boxScales[i];
                transform.ScaleY = _boxScales[i];
                transform.Rotation += 2;

                box.Opacity = Math.Max(0, 1.0 - (_boxScales[i] / 5.0));

                if (_boxScales[i] > 5.0 || box.Opacity <= 0.1)
                {
                    ColorBoxesCanvas.Children.Remove(box);
                    _colorBoxes.RemoveAt(i);
                    _boxScales.RemoveAt(i);
                    _boxGrowthRates.RemoveAt(i);
                }
            }

            if (_random.NextDouble() < 0.3 && _colorBoxes.Count < 15)
            {
                CreateNewColorBox();
            }
        }

        private void CreateColorBoxes()
        {
            _colorBoxes.Clear();
            _boxScales.Clear();
            _boxGrowthRates.Clear();
            ColorBoxesCanvas.Children.Clear();

            for (int i = 0; i < 5; i++)
            {
                CreateNewColorBox();
            }
        }

        private void CreateNewColorBox()
        {
            var box = new Rectangle
            {
                Width = 20 + _random.NextDouble() * 40,
                Height = 20 + _random.NextDouble() * 40,
                Fill = new SolidColorBrush(GetRandomColor()),
                Opacity = 0.8
            };

            Canvas.SetLeft(box, _random.NextDouble() * (_canvasWidth - 60));
            Canvas.SetTop(box, _random.NextDouble() * (_canvasHeight - 60));

            _colorBoxes.Add(box);
            _boxScales.Add(0.1);
            _boxGrowthRates.Add(0.05 + _random.NextDouble() * 0.1);
            ColorBoxesCanvas.Children.Add(box);
        }

        private void AnimateBouncingDots()
        {
            foreach (var dot in DotsCanvas.Children.OfType<Ellipse>())
            {
                var currentLeft = Canvas.GetLeft(dot);
                var currentTop = Canvas.GetTop(dot);

                var newLeft = currentLeft + (_random.NextDouble() - 0.5) * 25;
                var newTop = currentTop + (_random.NextDouble() - 0.5) * 25;

                newLeft = Math.Max(0, Math.Min(_canvasWidth - 15, newLeft));
                newTop = Math.Max(0, Math.Min(_canvasHeight - 15, newTop));

                Canvas.SetLeft(dot, newLeft);
                Canvas.SetTop(dot, newTop);

                ((SolidColorBrush)dot.Fill).Color = GetRandomColor();
            }
        }

        private void AnimateSpiral()
        {
            SpiralCanvas.Children.Clear();

            int dotCount = Math.Max(30, (int)(Math.Min(_canvasWidth, _canvasHeight) / 10));
            double maxRadius = Math.Min(_canvasWidth, _canvasHeight) * 0.4;
            double radiusIncrement = maxRadius / dotCount;

            for (int i = 0; i < dotCount; i++)
            {
                var angle = i * 0.3 + DateTime.Now.Millisecond * 0.003;
                var radius = i * radiusIncrement;

                var x = _canvasWidth / 2 + Math.Cos(angle) * radius;
                var y = _canvasHeight / 2 + Math.Sin(angle) * radius;

                var dotSize = Math.Max(4, Math.Min(_canvasWidth, _canvasHeight) / 100);
                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = new SolidColorBrush(GetRandomColor())
                };

                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot, y);

                SpiralCanvas.Children.Add(dot);
            }
        }

        private void AnimateParticleSystem()
        {
            foreach (var particle in _particles)
            {
                var currentLeft = Canvas.GetLeft(particle);
                var currentTop = Canvas.GetTop(particle);

                var newLeft = currentLeft + (_random.NextDouble() - 0.5) * 5;
                var newTop = currentTop + (_random.NextDouble() - 0.5) * 5;

                if (newLeft < 0 || newLeft > _canvasWidth) newLeft = _random.NextDouble() * _canvasWidth;
                if (newTop < 0 || newTop > _canvasHeight) newTop = _random.NextDouble() * _canvasHeight;

                Canvas.SetLeft(particle, newLeft);
                Canvas.SetTop(particle, newTop);

                ((SolidColorBrush)particle.Fill).Color = GetRandomColor();
            }
        }

        private void AnimateMatrixRain()
        {
            foreach (var textBlock in _matrixChars)
            {
                var currentTop = Canvas.GetTop(textBlock);
                var newTop = currentTop + 3;

                if (newTop > _canvasHeight)
                {
                    newTop = -20;
                    Canvas.SetLeft(textBlock, _random.NextDouble() * _canvasWidth);
                }

                Canvas.SetTop(textBlock, newTop);

                if (_random.NextDouble() < 0.1)
                {
                    string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    textBlock.Text = chars[_random.Next(chars.Length)].ToString();
                }
            }
        }

        private void UpdateVisibility()
        {
            SpectrumBars.Visibility = Visibility.Collapsed;
            CircularWave.Visibility = Visibility.Collapsed;
            PulseCircle.Visibility = Visibility.Collapsed;
            RippleEffect.Visibility = Visibility.Collapsed;
            ColorCycle.Visibility = Visibility.Collapsed;
            BouncingDots.Visibility = Visibility.Collapsed;
            Spiral.Visibility = Visibility.Collapsed;
            ParticleSystem.Visibility = Visibility.Collapsed;
            MatrixRain.Visibility = Visibility.Collapsed;
            Starlights.Visibility = Visibility.Collapsed;
            NightRain.Visibility = Visibility.Collapsed;
            WinterSnow.Visibility = Visibility.Collapsed;
            FallInLove.Visibility = Visibility.Collapsed;
            LightningStorm.Visibility = Visibility.Collapsed;
            OceanWaves.Visibility = Visibility.Collapsed;
            FireDance.Visibility = Visibility.Collapsed;
            GalaxySpiral.Visibility = Visibility.Collapsed;
            NeonPulse.Visibility = Visibility.Collapsed;
            CrystalFormation.Visibility = Visibility.Collapsed;
            LaserShow.Visibility = Visibility.Collapsed;
            DigitalRain.Visibility = Visibility.Collapsed;
            PlasmaStorm.Visibility = Visibility.Collapsed;
            AuroraBorealis.Visibility = Visibility.Collapsed;
            FractalTree.Visibility = Visibility.Collapsed;
            QuantumField.Visibility = Visibility.Collapsed;
            SolarFlare.Visibility = Visibility.Collapsed;
            GeometricPatterns.Visibility = Visibility.Collapsed;
            CyberGrid.Visibility = Visibility.Collapsed;
            NebulaCloud.Visibility = Visibility.Collapsed;
            EnergyVortex.Visibility = Visibility.Collapsed;
            Hologram.Visibility = Visibility.Collapsed;

            CircularWaveStoryboard.Stop();
            PulseStoryboard.Stop();
            RippleStoryboard.Stop();
            ColorCycleStoryboard.Stop();

            switch (_currentEffect)
            {
                case 0: SpectrumBars.Visibility = Visibility.Visible; break;
                case 1: CircularWave.Visibility = Visibility.Visible; break;
                case 2: PulseCircle.Visibility = Visibility.Visible; break;
                case 3: RippleEffect.Visibility = Visibility.Visible; break;
                case 4: ColorCycle.Visibility = Visibility.Visible; ColorCycleStoryboard.Begin(); break;
                case 5: BouncingDots.Visibility = Visibility.Visible; break;
                case 6: Spiral.Visibility = Visibility.Visible; break;
                case 7: ParticleSystem.Visibility = Visibility.Visible; break;
                case 8: MatrixRain.Visibility = Visibility.Visible; break;
                case 9: Starlights.Visibility = Visibility.Visible; break;
                case 10: NightRain.Visibility = Visibility.Visible; break;
                case 11: WinterSnow.Visibility = Visibility.Visible; break;
                case 12: FallInLove.Visibility = Visibility.Visible; break;
                case 13: LightningStorm.Visibility = Visibility.Visible; break;
                case 14: OceanWaves.Visibility = Visibility.Visible; break;
                case 15: FireDance.Visibility = Visibility.Visible; break;
                case 16: GalaxySpiral.Visibility = Visibility.Visible; break;
                case 17: NeonPulse.Visibility = Visibility.Visible; break;
                case 18: CrystalFormation.Visibility = Visibility.Visible; break;
                case 19: LaserShow.Visibility = Visibility.Visible; break;
                case 20: DigitalRain.Visibility = Visibility.Visible; break;
                case 21: PlasmaStorm.Visibility = Visibility.Visible; break;
                case 22: AuroraBorealis.Visibility = Visibility.Visible; break;
                case 23: FractalTree.Visibility = Visibility.Visible; break;
                case 24: QuantumField.Visibility = Visibility.Visible; break;
                case 25: SolarFlare.Visibility = Visibility.Visible; break;
                case 26: GeometricPatterns.Visibility = Visibility.Visible; break;
                case 27: CyberGrid.Visibility = Visibility.Visible; break;
                case 28: NebulaCloud.Visibility = Visibility.Visible; break;
                case 29: EnergyVortex.Visibility = Visibility.Visible; break;
                case 30: Hologram.Visibility = Visibility.Visible; break;
            }
        }

        public void SetVisualizerEffect(int effectIndex)
        {
            if (effectIndex >= 0 && effectIndex < MaxEffects)
            {
                _currentEffect = effectIndex;
                UpdateVisibility();
            }
        }

        public void SetRandomEffect()
        {
            var random = new Random();
            _currentEffect = random.Next(MaxEffects);
            UpdateVisibility();
        }

        public void StartVisualizer()
        {
            _isPlaying = true;
            if (_animationTimer == null)
            {
                InitializeTimer();
                InitializeEffects();
            }
            else
            {
                _animationTimer.Start();
            }
        }

        public void StopVisualizer()
        {
            _isPlaying = false;
            _animationTimer?.Stop();
        }

        public void NextEffect()
        {
            CurrentEffect = (_currentEffect + 1) % MaxEffects;
        }

        public void PreviousEffect()
        {
            CurrentEffect = (_currentEffect - 1 + MaxEffects) % MaxEffects;
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            PreviousEffect();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NextEffect();
        }

        private void RandomButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentEffect = _random.Next(MaxEffects);
        }

        private void VisualizerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e?.AddedItems != null && e.AddedItems.Count > 0)
            {
                var selectedItem = e.AddedItems[0] as ComboBoxItem;
                if (selectedItem?.Tag != null)
                {
                    _currentEffect = int.Parse(selectedItem.Tag.ToString());
                    UpdateVisibility();
                }
            }
        }

        private void LoadSpeedSetting()
        {
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (localSettings.Values.ContainsKey("MusicVisualizerSpeed"))
                {
                    _speedMultiplier = (double)localSettings.Values["MusicVisualizerSpeed"];
                }
            }
            catch
            {
                _speedMultiplier = 1.0;
            }
        }

        private void SaveSpeedSetting()
        {
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["MusicVisualizerSpeed"] = _speedMultiplier;
            }
            catch
            {
            }
        }

        private void UpdateSpeedDisplay()
        {
            if (SpeedDisplay != null)
            {
                SpeedDisplay.Text = $"{_speedMultiplier:F1}x";
            }
        }

        private void SpeedUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (_speedMultiplier < MaxSpeed)
            {
                _speedMultiplier = Math.Min(MaxSpeed, _speedMultiplier + SpeedIncrement);
                UpdateTimerInterval();
                UpdateSpeedDisplay();
                SaveSpeedSetting();
            }
        }

        private void SpeedDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (_speedMultiplier > MinSpeed)
            {
                _speedMultiplier = Math.Max(MinSpeed, _speedMultiplier - SpeedIncrement);
                UpdateTimerInterval();
                UpdateSpeedDisplay();
                SaveSpeedSetting();
            }
        }

        private void AnimateStarlights()
        {
            if (_stars.Count == 0)
            {
                CreateStarlights();
            }

            foreach (var star in _stars)
            {
                star.Opacity = 0.2 + Math.Abs(Math.Sin(DateTime.Now.Millisecond * 0.003 + _random.NextDouble() * 5)) * 0.8;

                if (_random.NextDouble() < 0.1)
                {
                    var colors = new[] { Colors.White, Colors.Yellow, Colors.LightBlue, Colors.Pink, Colors.LightGreen };
                    ((SolidColorBrush)star.Fill).Color = colors[_random.Next(colors.Length)];
                }
            }
        }

        private void CreateStarlights()
        {
            _stars.Clear();
            StarlightsCanvas.Children.Clear();

            for (int i = 0; i < 120; i++)
            {
                FrameworkElement starShape;
                int shapeType = _random.Next(3);

                switch (shapeType)
                {
                    case 0:
                        starShape = new Ellipse
                        {
                            Width = 3 + _random.NextDouble() * 8,
                            Height = 3 + _random.NextDouble() * 8,
                            Fill = new SolidColorBrush(Colors.White)
                        };
                        break;
                    case 1:
                        var triangle = new Polygon
                        {
                            Fill = new SolidColorBrush(Colors.White),
                            Points = new PointCollection { new Point(0, 8), new Point(4, 0), new Point(8, 8) }
                        };
                        starShape = triangle;
                        break;
                    default:
                        var star = new Polygon
                        {
                            Fill = new SolidColorBrush(Colors.White),
                            Points = new PointCollection {
                                new Point(5, 0), new Point(6, 3),
                                new Point(10, 3), new Point(7, 6),
                                new Point(8, 10), new Point(5, 7),
                                new Point(2, 10), new Point(3, 6),
                                new Point(0, 3), new Point(4, 3)
                            }
                        };
                        starShape = star;
                        break;
                }

                starShape.Opacity = 0.3 + _random.NextDouble() * 0.7;
                Canvas.SetLeft(starShape, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(starShape, _random.NextDouble() * _canvasHeight);

                if (starShape is Ellipse ellipse)
                    _stars.Add(ellipse);

                StarlightsCanvas.Children.Add(starShape);
            }
        }

        private void AnimateNightRain()
        {
            if (_raindrops.Count == 0)
            {
                CreateNightRain();
            }

            foreach (var raindrop in _raindrops)
            {
                var currentTop = Canvas.GetTop(raindrop);
                var newTop = currentTop + 5 + _random.NextDouble() * 10;

                if (newTop > _canvasHeight)
                {
                    newTop = -raindrop.Height;
                    Canvas.SetLeft(raindrop, _random.NextDouble() * _canvasWidth);
                }

                Canvas.SetTop(raindrop, newTop);
                raindrop.Opacity = 0.3 + _random.NextDouble() * 0.7;
            }
        }

        private void CreateNightRain()
        {
            _raindrops.Clear();
            NightRainCanvas.Children.Clear();

            for (int i = 0; i < 200; i++)
            {
                var raindrop = new Rectangle
                {
                    Width = 2,
                    Height = 10 + _random.NextDouble() * 20,
                    Fill = new SolidColorBrush(Color.FromArgb(180, 173, 216, 230)),
                    Opacity = 0.6
                };

                Canvas.SetLeft(raindrop, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(raindrop, _random.NextDouble() * _canvasHeight);

                _raindrops.Add(raindrop);
                NightRainCanvas.Children.Add(raindrop);
            }
        }

        private void AnimateWinterSnow()
        {
            if (_snowflakes.Count == 0)
            {
                CreateWinterSnow();
            }

            foreach (var snowflake in _snowflakes)
            {
                var currentLeft = Canvas.GetLeft(snowflake);
                var currentTop = Canvas.GetTop(snowflake);

                var newLeft = currentLeft + Math.Sin(DateTime.Now.Millisecond * 0.01) * 2;
                var newTop = currentTop + 1 + _random.NextDouble() * 3;

                if (newTop > _canvasHeight)
                {
                    newTop = -10;
                    newLeft = _random.NextDouble() * _canvasWidth;
                }

                Canvas.SetLeft(snowflake, newLeft);
                Canvas.SetTop(snowflake, newTop);
            }
        }

        private void CreateWinterSnow()
        {
            _snowflakes.Clear();
            WinterSnowCanvas.Children.Clear();

            for (int i = 0; i < 150; i++)
            {
                var snowflake = new Ellipse
                {
                    Width = 3 + _random.NextDouble() * 8,
                    Height = 3 + _random.NextDouble() * 8,
                    Fill = new SolidColorBrush(Colors.White),
                    Opacity = 0.4 + _random.NextDouble() * 0.6
                };

                Canvas.SetLeft(snowflake, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(snowflake, _random.NextDouble() * _canvasHeight);

                _snowflakes.Add(snowflake);
                WinterSnowCanvas.Children.Add(snowflake);
            }
        }

        private void AnimateFallInLove()
        {
            if (FallInLoveCanvas.Children.Count == 0)
            {
                CreateFallInLove();
            }

            foreach (var heart in FallInLoveCanvas.Children.OfType<Polygon>())
            {
                var currentTop = Canvas.GetTop(heart);
                var currentLeft = Canvas.GetLeft(heart);

                var newTop = currentTop - 2 - _random.NextDouble() * 3;
                var newLeft = currentLeft + Math.Sin(DateTime.Now.Millisecond * 0.01) * 3;

                if (newTop < -30)
                {
                    newTop = _canvasHeight + 20;
                    newLeft = _random.NextDouble() * _canvasWidth;
                    ((SolidColorBrush)heart.Fill).Color = GetHeartColor();
                }

                Canvas.SetLeft(heart, newLeft);
                Canvas.SetTop(heart, newTop);
            }
        }

        private void CreateFallInLove()
        {
            _hearts.Clear();
            FallInLoveCanvas.Children.Clear();

            for (int i = 0; i < 60; i++)
            {
                var heart = new Polygon
                {
                    Points = new PointCollection {
                        new Point(10, 5),
                        new Point(5, 0),
                        new Point(0, 5),
                        new Point(5, 15),
                        new Point(15, 5),
                        new Point(20, 0),
                        new Point(15, -5),
                        new Point(10, 5)
                    },
                    Fill = new SolidColorBrush(GetHeartColor()),
                    Opacity = 0.6 + _random.NextDouble() * 0.4
                };

                Canvas.SetLeft(heart, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(heart, _random.NextDouble() * _canvasHeight);

                FallInLoveCanvas.Children.Add(heart);
            }
        }

        private Color GetHeartColor()
        {
            Color[] heartColors = {
                Colors.Pink, Colors.Red, Colors.HotPink, Colors.DeepPink,
                Colors.LightPink, Colors.Crimson, Colors.Magenta, Colors.Violet
            };
            return heartColors[_random.Next(heartColors.Length)];
        }

        private void AnimateLightningStorm()
        {
            LightningCanvas.Children.Clear();

            if (_random.NextDouble() < 0.4)
            {
                for (int i = 0; i < 5; i++)
                {
                    var lightningColors = new[] { Colors.Yellow, Colors.White, Colors.Red, Colors.LightBlue };
                    var lightning = new Rectangle
                    {
                        Width = 1 + _random.NextDouble() * 6,
                        Height = _canvasHeight,
                        Fill = new SolidColorBrush(lightningColors[_random.Next(lightningColors.Length)]),
                        Opacity = 0.7 + _random.NextDouble() * 0.3
                    };

                    Canvas.SetLeft(lightning, _random.NextDouble() * _canvasWidth);
                    Canvas.SetTop(lightning, 0);

                    LightningCanvas.Children.Add(lightning);
                }
            }
        }

        private void AnimateOceanWaves()
        {
            OceanWavesCanvas.Children.Clear();

            for (int wave = 0; wave < 5; wave++)
            {
                double waveHeight = _canvasHeight * (0.6 + wave * 0.08);
                double time = DateTime.Now.Millisecond * 0.002;

                for (int i = 0; i < _canvasWidth; i += 4)
                {
                    double y = waveHeight + Math.Sin((i * 0.02) + time + (wave * 0.5)) * (20 + wave * 10);

                    var waveDot = new Ellipse
                    {
                        Width = 3,
                        Height = 3,
                        Fill = new SolidColorBrush(Color.FromArgb(150, 0, (byte)(100 + wave * 30), 255))
                    };

                    Canvas.SetLeft(waveDot, i);
                    Canvas.SetTop(waveDot, y);

                    OceanWavesCanvas.Children.Add(waveDot);
                }
            }
        }

        private void AnimateFireDance()
        {
            FireDanceCanvas.Children.Clear();

            double centerX = _canvasWidth / 2;
            double centerY = _canvasHeight;

            for (int i = 0; i < 100; i++)
            {
                double angle = _random.NextDouble() * Math.PI;
                double distance = _random.NextDouble() * (_canvasHeight * 0.6);
                double size = 3 + _random.NextDouble() * 15;

                double x = centerX + Math.Cos(angle) * distance * 0.5;
                double y = centerY - distance + Math.Sin(DateTime.Now.Millisecond * 0.01 + i) * 20;

                var flame = new Ellipse
                {
                    Width = size,
                    Height = size * 1.5,
                    Fill = new SolidColorBrush(Color.FromArgb(180,
                        (byte)(200 + _random.Next(55)),
                        (byte)(100 + _random.Next(155)),
                        (byte)(_random.Next(50)))),
                    Opacity = 0.4 + _random.NextDouble() * 0.6
                };

                Canvas.SetLeft(flame, x);
                Canvas.SetTop(flame, y);

                FireDanceCanvas.Children.Add(flame);
            }
        }

        private void AnimateGalaxySpiral()
        {
            GalaxySpiralCanvas.Children.Clear();

            double centerX = _canvasWidth / 2;
            double centerY = _canvasHeight / 2;
            double time = DateTime.Now.Millisecond * 0.005;
            double maxRadius = Math.Max(_canvasWidth, _canvasHeight) * 2.4;

            for (int i = 0; i < 500; i++)
            {
                double t = i * 0.06;
                double radius = (t / 500.0) * maxRadius;
                double angle = t + time;

                double x = centerX + Math.Cos(angle) * radius;
                double y = centerY + Math.Sin(angle) * radius;

                if (x >= -50 && x <= _canvasWidth + 50 && y >= -50 && y <= _canvasHeight + 50)
                {
                    var star = new Ellipse
                    {
                        Width = 2 + (i % 12 == 0 ? _random.NextDouble() * 10 : 0),
                        Height = 2 + (i % 12 == 0 ? _random.NextDouble() * 10 : 0),
                        Fill = new SolidColorBrush(GetRandomColor()),
                        Opacity = 0.3 + _random.NextDouble() * 0.7
                    };

                    Canvas.SetLeft(star, x);
                    Canvas.SetTop(star, y);

                    GalaxySpiralCanvas.Children.Add(star);
                }
            }
        }

        private void AnimateNeonPulse()
        {
            NeonPulseCanvas.Children.Clear();

            double centerX = _canvasWidth / 2;
            double centerY = _canvasHeight / 2;
            double pulse = Math.Sin(DateTime.Now.Millisecond * 0.01) * 0.5 + 0.5;

            for (int ring = 1; ring <= 8; ring++)
            {
                double radius = ring * 30 * (1 + pulse * 0.5);
                int pointCount = ring * 8;

                for (int i = 0; i < pointCount; i++)
                {
                    double angle = (Math.PI * 2 / pointCount) * i;
                    double x = centerX + Math.Cos(angle) * radius;
                    double y = centerY + Math.Sin(angle) * radius;

                    if (x >= 0 && x <= _canvasWidth && y >= 0 && y <= _canvasHeight)
                    {
                        var neonDot = new Ellipse
                        {
                            Width = 4 + pulse * 6,
                            Height = 4 + pulse * 6,
                            Fill = new SolidColorBrush(Color.FromArgb(200,
                                (byte)(100 + ring * 20),
                                (byte)(255 - ring * 20),
                                255)),
                            Opacity = 0.7 + pulse * 0.3
                        };

                        Canvas.SetLeft(neonDot, x);
                        Canvas.SetTop(neonDot, y);

                        NeonPulseCanvas.Children.Add(neonDot);
                    }
                }
            }
        }

        private void AnimateCrystalFormation()
        {
            CrystalCanvas.Children.Clear();

            double centerX = _canvasWidth / 2;
            double centerY = _canvasHeight / 2;
            double time = DateTime.Now.Millisecond * 0.001;

            for (int crystal = 0; crystal < 50; crystal++)
            {
                double angle = (Math.PI * 2 / 50) * crystal + time;
                double distance = 50 + (crystal * 8) + Math.Sin(time + crystal) * 30;

                double x = centerX + Math.Cos(angle) * distance;
                double y = centerY + Math.Sin(angle) * distance;

                if (x >= 0 && x <= _canvasWidth && y >= 0 && y <= _canvasHeight)
                {
                    var crystalShape = new Rectangle
                    {
                        Width = 8 + _random.NextDouble() * 12,
                        Height = 8 + _random.NextDouble() * 12,
                        Fill = new SolidColorBrush(Color.FromArgb(150,
                            (byte)(150 + _random.Next(105)),
                            (byte)(200 + _random.Next(55)),
                            (byte)(255))),
                        Opacity = 0.4 + _random.NextDouble() * 0.6
                    };

                    crystalShape.RenderTransform = new RotateTransform { Angle = angle * 180 / Math.PI };

                    Canvas.SetLeft(crystalShape, x);
                    Canvas.SetTop(crystalShape, y);

                    CrystalCanvas.Children.Add(crystalShape);
                }
            }
        }

        private void AnimateLaserShow()
        {
            LaserCanvas.Children.Clear();

            for (int i = 0; i < 8; i++)
            {
                var laser = new Rectangle
                {
                    Width = 4,
                    Height = _canvasHeight,
                    Fill = new SolidColorBrush(Color.FromArgb(255, (byte)(200 + _random.Next(55)), 0, (byte)(100 + _random.Next(155)))),
                    Opacity = 0.8 + _random.NextDouble() * 0.2
                };

                Canvas.SetLeft(laser, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(laser, 0);

                LaserCanvas.Children.Add(laser);
            }
        }

        private void AnimateDigitalRain()
        {
            DigitalRainCanvas.Children.Clear();

            string chars = "01";
            for (int i = 0; i < 150; i++)
            {
                var textBlock = new TextBlock
                {
                    Text = chars[_random.Next(chars.Length)].ToString(),
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 0, (byte)(150 + _random.Next(105)), 0))
                };

                Canvas.SetLeft(textBlock, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(textBlock, _random.NextDouble() * _canvasHeight);

                DigitalRainCanvas.Children.Add(textBlock);
            }
        }

        private void AnimatePlasmaStorm()
        {
            PlasmaCanvas.Children.Clear();

            for (int i = 0; i < 80; i++)
            {
                var plasma = new Ellipse
                {
                    Width = 10 + _random.NextDouble() * 30,
                    Height = 10 + _random.NextDouble() * 30,
                    Fill = new SolidColorBrush(Color.FromArgb(180, (byte)(100 + _random.Next(155)), 0, (byte)(200 + _random.Next(55)))),
                    Opacity = 0.4 + _random.NextDouble() * 0.6
                };

                Canvas.SetLeft(plasma, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(plasma, _random.NextDouble() * _canvasHeight);

                PlasmaCanvas.Children.Add(plasma);
            }
        }

        private void AnimateAuroraBorealis()
        {
            AuroraCanvas.Children.Clear();

            for (int i = 0; i < 100; i++)
            {
                var wave = new Rectangle
                {
                    Width = 5,
                    Height = 20 + _random.NextDouble() * 100,
                    Fill = new SolidColorBrush(Color.FromArgb(120, 0, (byte)(100 + _random.Next(155)), (byte)(50 + _random.Next(205)))),
                    Opacity = 0.3 + _random.NextDouble() * 0.5
                };

                Canvas.SetLeft(wave, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(wave, _random.NextDouble() * (_canvasHeight * 0.6));

                AuroraCanvas.Children.Add(wave);
            }
        }

        private void AnimateFractalTree()
        {
            FractalTreeCanvas.Children.Clear();

            double centerX = _canvasWidth / 2;
            double centerY = _canvasHeight;
            double time = DateTime.Now.Millisecond * 0.001;

            double maxTreeHeight = _canvasHeight * 0.75;
            double baseHeight = Math.Min(maxTreeHeight * 0.6, 120 + Math.Sin(time * 2) * 40);
            double sway = Math.Sin(time * 1.5) * 0.4;

            DrawBranch(centerX, centerY, centerX + sway * 40, centerY - baseHeight, 8, FractalTreeCanvas, time);
        }

        private void DrawBranch(double x1, double y1, double x2, double y2, int depth, Canvas canvas, double time)
        {
            if (depth <= 0) return;

            double thickness = Math.Max(1, depth * 1.0 + Math.Sin(time + depth) * 0.7);
            byte colorVariation = (byte)(Math.Sin(time + depth * 0.5) * 50 + 155);

            var branch = new Rectangle
            {
                Width = thickness,
                Height = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2)),
                Fill = new SolidColorBrush(Color.FromArgb(255,
                    (byte)(50 + depth * 20),
                    (byte)(100 + depth * 15 + Math.Sin(time) * 30),
                    (byte)(50 + colorVariation))),
                Opacity = 0.7 + Math.Sin(time + depth) * 0.2
            };

            double angle = Math.Atan2(y2 - y1, x2 - x1) * 180 / Math.PI;
            branch.RenderTransform = new RotateTransform { Angle = angle };

            Canvas.SetLeft(branch, x1);
            Canvas.SetTop(branch, y1);

            canvas.Children.Add(branch);

            if (depth > 1)
            {
                double baseAngle = Math.Atan2(y2 - y1, x2 - x1);
                double angleVariation = Math.Sin(time + depth) * 0.2;
                double angle1 = baseAngle + 0.5 + angleVariation;
                double angle2 = baseAngle - 0.5 - angleVariation;
                double length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2)) * (0.8 + Math.Sin(time + depth) * 0.15);

                double growth1 = 1.0 + Math.Sin(time * 2 + depth) * 0.3;
                double growth2 = 1.0 + Math.Cos(time * 1.8 + depth) * 0.3;

                DrawBranch(x2, y2, x2 + Math.Cos(angle1) * length * growth1, y2 + Math.Sin(angle1) * length * growth1, depth - 1, canvas, time);
                DrawBranch(x2, y2, x2 + Math.Cos(angle2) * length * growth2, y2 + Math.Sin(angle2) * length * growth2, depth - 1, canvas, time);
            }
        }

        private void AnimateQuantumField()
        {
            QuantumCanvas.Children.Clear();

            for (int i = 0; i < 60; i++)
            {
                var particle = new Ellipse
                {
                    Width = 2 + _random.NextDouble() * 6,
                    Height = 2 + _random.NextDouble() * 6,
                    Fill = new SolidColorBrush(Color.FromArgb(200, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
                    Opacity = 0.5 + _random.NextDouble() * 0.5
                };

                Canvas.SetLeft(particle, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(particle, _random.NextDouble() * _canvasHeight);

                QuantumCanvas.Children.Add(particle);
            }
        }

        private void AnimateSolarFlare()
        {
            SolarFlareCanvas.Children.Clear();

            double centerX = _canvasWidth / 2;
            double centerY = _canvasHeight / 2;

            for (int i = 0; i < 50; i++)
            {
                double angle = _random.NextDouble() * Math.PI * 2;
                double distance = _random.NextDouble() * 200;

                var flare = new Rectangle
                {
                    Width = 3 + _random.NextDouble() * 8,
                    Height = 20 + _random.NextDouble() * 80,
                    Fill = new SolidColorBrush(Color.FromArgb(200, 255, (byte)(150 + _random.Next(105)), 0)),
                    Opacity = 0.6 + _random.NextDouble() * 0.4
                };

                Canvas.SetLeft(flare, centerX + Math.Cos(angle) * distance);
                Canvas.SetTop(flare, centerY + Math.Sin(angle) * distance);

                SolarFlareCanvas.Children.Add(flare);
            }
        }

        private void AnimateGeometricPatterns()
        {
            GeometricCanvas.Children.Clear();

            for (int i = 0; i < 20; i++)
            {
                var shape = new Rectangle
                {
                    Width = 20 + _random.NextDouble() * 60,
                    Height = 20 + _random.NextDouble() * 60,
                    Fill = new SolidColorBrush(GetRandomColor()),
                    Opacity = 0.4 + _random.NextDouble() * 0.6
                };

                shape.RenderTransform = new RotateTransform { Angle = _random.NextDouble() * 360 };

                Canvas.SetLeft(shape, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(shape, _random.NextDouble() * _canvasHeight);

                GeometricCanvas.Children.Add(shape);
            }
        }

        private void AnimateCyberGrid()
        {
            CyberGridCanvas.Children.Clear();

            double time = DateTime.Now.Millisecond * 0.001;
            double pulse = Math.Sin(time * 1) * 0.5 + 0.5;

            for (int x = 0; x < _canvasWidth; x += 40)
            {
                double lineOpacity = 60 + pulse * 140;
                double lineShift = Math.Sin(time + x * 0.01) * 5;

                var line = new Rectangle
                {
                    Width = 1 + pulse * 2,
                    Height = _canvasHeight,
                    Fill = new SolidColorBrush(Color.FromArgb((byte)lineOpacity, 0, 255, 255)),
                    Opacity = 0.4 + pulse * 0.6
                };

                Canvas.SetLeft(line, x + lineShift);
                Canvas.SetTop(line, 0);

                CyberGridCanvas.Children.Add(line);

                if (x % 120 == 0)
                {
                    double packetY = (time * 25 + x) % (_canvasHeight + 40) - 20;
                    var packet = new Rectangle
                    {
                        Width = 8,
                        Height = 12,
                        Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)),
                        Opacity = 0.8
                    };

                    Canvas.SetLeft(packet, x + lineShift - 4);
                    Canvas.SetTop(packet, packetY);

                    CyberGridCanvas.Children.Add(packet);
                }
            }

            for (int y = 0; y < _canvasHeight; y += 40)
            {
                double lineOpacity = 60 + pulse * 140;
                double lineShift = Math.Cos(time + y * 0.01) * 5;

                var line = new Rectangle
                {
                    Width = _canvasWidth,
                    Height = 1 + pulse * 2,
                    Fill = new SolidColorBrush(Color.FromArgb((byte)lineOpacity, 0, 255, 255)),
                    Opacity = 0.4 + pulse * 0.6
                };

                Canvas.SetLeft(line, 0);
                Canvas.SetTop(line, y + lineShift);

                CyberGridCanvas.Children.Add(line);

                if (y % 120 == 0)
                {
                    double packetX = (time * 35 + y) % (_canvasWidth + 40) - 20;
                    var packet = new Rectangle
                    {
                        Width = 12,
                        Height = 8,
                        Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)),
                        Opacity = 0.8
                    };

                    Canvas.SetLeft(packet, packetX);
                    Canvas.SetTop(packet, y + lineShift - 4);

                    CyberGridCanvas.Children.Add(packet);
                }
            }

            for (int x = 0; x < _canvasWidth; x += 120)
            {
                for (int y = 0; y < _canvasHeight; y += 120)
                {
                    double nodePulse = Math.Sin(time * 0.8 + x * 0.01 + y * 0.01) * 0.5 + 0.5;
                    var node = new Ellipse
                    {
                        Width = 4 + nodePulse * 8,
                        Height = 4 + nodePulse * 8,
                        Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)),
                        Opacity = 0.6 + nodePulse * 0.4
                    };

                    Canvas.SetLeft(node, x - (2 + nodePulse * 4));
                    Canvas.SetTop(node, y - (2 + nodePulse * 4));

                    CyberGridCanvas.Children.Add(node);
                }
            }
        }

        private void AnimateNebulaCloud()
        {
            NebulaCanvas.Children.Clear();

            for (int i = 0; i < 100; i++)
            {
                var cloud = new Ellipse
                {
                    Width = 20 + _random.NextDouble() * 100,
                    Height = 20 + _random.NextDouble() * 100,
                    Fill = new SolidColorBrush(Color.FromArgb(80, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
                    Opacity = 0.2 + _random.NextDouble() * 0.4
                };

                Canvas.SetLeft(cloud, _random.NextDouble() * _canvasWidth);
                Canvas.SetTop(cloud, _random.NextDouble() * _canvasHeight);

                NebulaCanvas.Children.Add(cloud);
            }
        }

        private void AnimateEnergyVortex()
        {
            VortexCanvas.Children.Clear();

            double centerX = _canvasWidth / 2;
            double centerY = _canvasHeight / 2;
            double time = DateTime.Now.Millisecond * 0.01;

            for (int i = 0; i < 80; i++)
            {
                double angle = (i * 0.2) + time;
                double radius = i * 3;

                var particle = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromArgb(255, (byte)(100 + i * 2), 0, (byte)(255 - i * 2))),
                    Opacity = 0.7
                };

                Canvas.SetLeft(particle, centerX + Math.Cos(angle) * radius);
                Canvas.SetTop(particle, centerY + Math.Sin(angle) * radius);

                VortexCanvas.Children.Add(particle);
            }
        }

        private void AnimateHologram()
        {
            HologramCanvas.Children.Clear();

            for (int i = 0; i < 50; i++)
            {
                var line = new Rectangle
                {
                    Width = _canvasWidth,
                    Height = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(150, 0, (byte)(100 + _random.Next(155)), 255)),
                    Opacity = 0.1 + _random.NextDouble() * 0.4
                };

                Canvas.SetLeft(line, 0);
                Canvas.SetTop(line, _random.NextDouble() * _canvasHeight);

                HologramCanvas.Children.Add(line);
            }
        }

        public void UpdateSpectrum(float[] fftData)
        {
            // Self-running timer animations
        }

        public void Reset()
        {
            _isPlaying = false;
            _animationTimer?.Stop();
        }

        public void ClearData()
        {
            try
            {
                if (_animationTimer != null)
                {
                    _animationTimer.Stop();
                    _animationTimer.Tick -= AnimationTimer_Tick;
                    _animationTimer = null;
                }

                _spectrumBars.Clear();
                _particles.Clear();
                _matrixChars.Clear();
                _pulseCircles.Clear();
                _stars.Clear();
                _raindrops.Clear();
                _snowflakes.Clear();
                _hearts.Clear();
                _lightning.Clear();
                _colorBoxes.Clear();
                _boxScales.Clear();
                _boxGrowthRates.Clear();

                SpectrumBarsControl?.Items?.Clear();
                DotsCanvas?.Children?.Clear();
                ParticleCanvas?.Children?.Clear();
                MatrixCanvas?.Children?.Clear();
                PulseCanvas?.Children?.Clear();
                StarlightsCanvas?.Children?.Clear();
                NightRainCanvas?.Children?.Clear();
                WinterSnowCanvas?.Children?.Clear();
                FallInLoveCanvas?.Children?.Clear();
                LightningCanvas?.Children?.Clear();
                SpiralCanvas?.Children?.Clear();
                ColorBoxesCanvas?.Children?.Clear();
                GalaxySpiralCanvas?.Children?.Clear();
                NeonPulseCanvas?.Children?.Clear();
                CrystalCanvas?.Children?.Clear();
                LaserCanvas?.Children?.Clear();
                DigitalRainCanvas?.Children?.Clear();
                PlasmaCanvas?.Children?.Clear();
                AuroraCanvas?.Children?.Clear();
                FractalTreeCanvas?.Children?.Clear();
                QuantumCanvas?.Children?.Clear();
                SolarFlareCanvas?.Children?.Clear();
                GeometricCanvas?.Children?.Clear();
                CyberGridCanvas?.Children?.Clear();
                NebulaCanvas?.Children?.Clear();
                VortexCanvas?.Children?.Clear();
                HologramCanvas?.Children?.Clear();

                _currentEffect = 0;
                _isPlaying = false;

                System.Diagnostics.Debug.WriteLine("Stop and clear all", nameof(MusicVisualizerControl));
            }
            catch (Exception)
            {
            }
        }
    }
}
