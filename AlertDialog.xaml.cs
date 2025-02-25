using System.Windows;

namespace TimerAlertApp
{
    public enum AlertDialogResult
    {
        Pause,
        Continue,
        Stop
    }

    public partial class AlertDialog : Window
    {
        public AlertDialogResult Result { get; private set; }

        public AlertDialog(string message)
        {
            InitializeComponent();
            txtMessage.Text = message;

            // **常に最前面に表示**
            this.Topmost = true;
            this.Activate();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            Result = AlertDialogResult.Pause;
            this.DialogResult = true;
            Close();
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            Result = AlertDialogResult.Continue;
            this.DialogResult = true;
            Close();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Result = AlertDialogResult.Stop;
            this.DialogResult = true;
            Close();
        }
    }
}
