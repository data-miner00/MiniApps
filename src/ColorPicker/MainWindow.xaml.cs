using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ColorPicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _statusTimer;

        public MainWindow()
        {
            InitializeComponent();
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _statusTimer.Tick += (_, _) => { StatusText.Text = string.Empty; _statusTimer.Stop(); };
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new PickerOverlayWindow { Owner = this };
            if (overlay.ShowDialog() == true && overlay.PickedColor is { } color)
            {
                ApplyColor(color);
            }
        }

        private void CopyHex_Click(object sender, RoutedEventArgs e) => CopyToClipboard(HexBox.Text);

        private void CopyRgb_Click(object sender, RoutedEventArgs e) => CopyToClipboard(RgbBox.Text);

        private void ApplyColor(Color color)
        {
            Swatch.Background = new SolidColorBrush(color);
            HexBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            RgbBox.Text = $"{color.R}, {color.G}, {color.B}";
            CopyToClipboard(HexBox.Text);
        }

        private void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Clipboard.SetText(text);
            StatusText.Text = $"Copied {text} to clipboard";
            _statusTimer.Stop();
            _statusTimer.Start();
        }
    }
}
