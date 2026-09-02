using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ColorPicker
{
    /// <summary>
    /// Fullscreen transparent overlay used to sample a pixel color from anywhere on screen.
    /// </summary>
    public partial class PickerOverlayWindow : Window
    {
        public Color? PickedColor { get; private set; }

        public PickerOverlayWindow()
        {
            InitializeComponent();
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => Focus();

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!TryGetPixelColor(out var color)) return;

            TooltipSwatch.Background = new SolidColorBrush(color);
            TooltipText.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            Tooltip.Visibility = Visibility.Visible;

            var pos = e.GetPosition(RootCanvas);
            Canvas.SetLeft(Tooltip, pos.X + 18);
            Canvas.SetTop(Tooltip, pos.Y + 18);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!TryGetPixelColor(out var color)) return;
            PickedColor = color;
            DialogResult = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                DialogResult = false;
        }

        // CLR_INVALID: GetPixel's sentinel return value when the pixel could not be read.
        const uint ClrInvalid = 0xFFFFFFFF;

        static bool TryGetPixelColor(out Color color)
        {
            color = default;

            if (!GetCursorPos(out var point)) return false;

            var hdc = GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero) return false;

            try
            {
                var pixel = GetPixel(hdc, point.X, point.Y);
                if (pixel == ClrInvalid) return false;

                color = Color.FromRgb(
                    (byte)(pixel & 0x000000FF),
                    (byte)((pixel & 0x0000FF00) >> 8),
                    (byte)((pixel & 0x00FF0000) >> 16));
                return true;
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Point
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out Point point);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int x, int y);
    }
}
