using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenRuler
{
    /// <summary>
    /// Fullscreen transparent overlay used to click-drag a distance/angle measurement anywhere on screen.
    /// </summary>
    public partial class MeasureOverlayWindow : Window
    {
        private Point? _start;
        private bool _dragging;

        public MeasureOverlayWindow()
        {
            InitializeComponent();
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => Focus();

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var start = e.GetPosition(RootCanvas);
            _start = start;
            _dragging = true;

            MeasureLine.X1 = MeasureLine.X2 = start.X;
            MeasureLine.Y1 = MeasureLine.Y2 = start.Y;
            MeasureLine.Visibility = Visibility.Visible;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || _start is not { } start) return;

            var pos = e.GetPosition(RootCanvas);
            MeasureLine.X2 = pos.X;
            MeasureLine.Y2 = pos.Y;

            var dx = pos.X - start.X;
            var dy = pos.Y - start.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var angle = Math.Atan2(-dy, dx) * 180 / Math.PI;

            TooltipText.Text = $"{distance:F0} px, {angle:F1}°";
            Tooltip.Visibility = Visibility.Visible;
            Canvas.SetLeft(Tooltip, pos.X + 18);
            Canvas.SetTop(Tooltip, pos.Y + 18);
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => _dragging = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                DialogResult = false;
        }
    }
}
