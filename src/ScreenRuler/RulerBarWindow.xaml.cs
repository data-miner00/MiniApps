using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenRuler
{
    /// <summary>
    /// Resizable, semi-transparent always-on-top ruler bar with tick marks.
    /// </summary>
    public partial class RulerBarWindow : Window
    {
        private const int MinorTickSpacing = 10;
        private const int MajorTickSpacing = 50;

        private static readonly Brush InkBrush = new SolidColorBrush(Color.FromRgb(0x3B, 0x24, 0x10));

        private Orientation _orientation = Orientation.Horizontal;

        public RulerBarWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => RedrawTicks();

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => RedrawTicks();

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void OrientationButton_Click(object sender, RoutedEventArgs e)
        {
            _orientation = _orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
            (Width, Height) = (Height, Width);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void RedrawTicks()
        {
            TickCanvas.Children.Clear();

            var horizontal = _orientation == Orientation.Horizontal;
            var length = horizontal ? ActualWidth : ActualHeight;
            var thickness = horizontal ? ActualHeight : ActualWidth;

            for (var pos = 0; pos <= length; pos += MinorTickSpacing)
            {
                var isMajor = pos % MajorTickSpacing == 0;
                var tickLength = thickness * (isMajor ? 0.4 : 0.2);

                var line = new Line
                {
                    Stroke = InkBrush,
                    StrokeThickness = isMajor ? 1.5 : 1,
                };

                if (horizontal)
                {
                    line.X1 = line.X2 = pos;
                    line.Y1 = 0;
                    line.Y2 = tickLength;
                }
                else
                {
                    line.Y1 = line.Y2 = pos;
                    line.X1 = 0;
                    line.X2 = tickLength;
                }

                TickCanvas.Children.Add(line);

                if (isMajor && pos > 0)
                {
                    var label = new TextBlock
                    {
                        Text = pos.ToString(),
                        Foreground = InkBrush,
                        FontSize = 9,
                        FontFamily = new FontFamily("Consolas"),
                    };

                    if (horizontal)
                    {
                        Canvas.SetLeft(label, pos + 2);
                        Canvas.SetTop(label, tickLength + 1);
                    }
                    else
                    {
                        Canvas.SetTop(label, pos + 2);
                        Canvas.SetLeft(label, tickLength + 1);
                    }

                    TickCanvas.Children.Add(label);
                }
            }

            LengthText.Text = $"{length:F0} px";
        }
    }
}
