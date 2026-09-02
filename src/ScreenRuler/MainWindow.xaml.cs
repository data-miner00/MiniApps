using System.Windows;

namespace ScreenRuler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RulerBarWindow? _rulerWindow;

        public MainWindow()
        {
            InitializeComponent();
            UpdateStatusText();
        }

        private void Mode_Checked(object sender, RoutedEventArgs e) => UpdateStatusText();

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (MeasureModeRadio.IsChecked == true)
            {
                var overlay = new MeasureOverlayWindow { Owner = this };
                overlay.ShowDialog();
                return;
            }

            if (_rulerWindow != null)
            {
                _rulerWindow.Activate();
                return;
            }

            _rulerWindow = new RulerBarWindow { Owner = this };
            _rulerWindow.Closed += (_, _) => _rulerWindow = null;
            _rulerWindow.Show();
        }

        private void UpdateStatusText()
        {
            if (StatusText == null) return;

            StatusText.Text = MeasureModeRadio.IsChecked == true
                ? "Click Start, then click-drag anywhere on screen to measure distance and angle. Esc closes."
                : "Click Start to open a resizable ruler bar. Drag its edges to resize, its body to move.";
        }
    }
}
