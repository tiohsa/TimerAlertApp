using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TimerAlertApp
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private int _intervalMinutes;
        private DateTime _startTime;
        private DateTime _nextAlertTime;
        private TimeSpan _remainingTime;
        private string _alertTitle;
        private readonly string logFilePath = "log.csv"; // CSV保存ファイル

        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(1); // 1秒ごとに更新

            LoadLog(); // 起動時にCSVからログを読み込む
            btnStop.IsEnabled = false; // 初期状態で停止ボタンは無効
            btnStart.IsEnabled = true; // 初期状態で開始ボタンも無効
            txtTitle.IsEnabled = true;
            cmbMinutes.IsEnabled = true;

            // **ComboBox の値を設定 (5分～60分)**
#if DEBUG
            cmbMinutes.Items.Add(1);
#endif
            for (int i = 5; i <= 60; i += 5)
            {
                cmbMinutes.Items.Add(i);
            }
            cmbMinutes.SelectedIndex = -1; // 初期選択なし
        }

        private void TxtTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnStart.IsEnabled = !string.IsNullOrWhiteSpace(txtTitle.Text);
        }

        private void TxtInputChanged(object sender, EventArgs e)
        {
            // **タイトルと時間の入力チェック**
            bool isTitleValid = !string.IsNullOrWhiteSpace(txtTitle.Text);
            bool isMinutesValid = cmbMinutes.SelectedItem != null;

            // 両方の入力がある場合のみ `開始` ボタンを有効化
            btnStart.IsEnabled = isTitleValid && isMinutesValid;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                ShowAlert("タイトルを入力してください。", "エラー", isError: true);
                return;
            }
            if (int.TryParse(cmbMinutes.Text, out _intervalMinutes) && _intervalMinutes > 0)
            {
                _startTime = DateTime.Now;
                _nextAlertTime = _startTime.AddMinutes(_intervalMinutes);
                _remainingTime = TimeSpan.FromMinutes(_intervalMinutes);
                _alertTitle = txtTitle.Text; // タイトル取得

                lblStartTime.Text = $"スタート時間: {_startTime:HH:mm:ss}";
                lblNextAlert.Text = $"次のアラート時間: {_nextAlertTime:HH:mm:ss}";
                lblCountdown.Text = $"残り時間: {_remainingTime:mm\\:ss}";

                _timer.Start();

                // **ボタンの制御**
                btnStart.IsEnabled = false;  // 開始ボタンを無効化
                btnStop.IsEnabled = true;    // 停止ボタンを有効化
                txtTitle.IsEnabled = false;
                cmbMinutes.IsEnabled = false;

                //ShowAlert($"アラートを {_intervalMinutes} 分ごとに設定しました。\nタイトル: {_alertTitle}", "開始");
            }
            else
            {
                ShowAlert("正しい時間（分）を入力してください。", "エラー", isError: true);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            TimeSpan elapsedTime = DateTime.Now - _startTime;

            lblCountdown.Text = "カウントダウン: --:--";
            lblElapsedTime.Text = $"経過時間: {elapsedTime:mm\\:ss}";

            // **CSVにログを保存**
            LogEntry newLog = SaveLog(_alertTitle, _startTime, elapsedTime);

            // **UIのリストに追加**
            logListView.Items.Insert(0, newLog);

            // **ボタンの制御**
            btnStart.IsEnabled = true;   // 開始ボタンを有効化
            btnStop.IsEnabled = false;   // 停止ボタンを無効化
            txtTitle.IsEnabled = true;
            cmbMinutes.IsEnabled = true;

            //ShowAlert($"アラートを停止しました。\nタイトル: {_alertTitle}\n経過時間: {elapsedTime:mm\\:ss}", "停止");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _remainingTime = _nextAlertTime - DateTime.Now;

            if (_remainingTime.TotalSeconds <= 0)
            {
                ShowAlert($"【{_alertTitle}】\n指定時間 {_intervalMinutes} 分が経過しました！", "アラート", isWarning: true);
                _nextAlertTime = DateTime.Now.AddMinutes(_intervalMinutes);
            }

            lblCountdown.Text = $"残り時間: {_remainingTime:mm\\:ss}";
            lblNextAlert.Text = $"次のアラート時間: {_nextAlertTime:HH:mm:ss}";
        }

        private void ShowAlert(string message, string title, bool isWarning = false, bool isError = false)
        {
            MessageBoxImage icon = isWarning ? MessageBoxImage.Warning :
                                 isError ? MessageBoxImage.Error :
                                 MessageBoxImage.Information;

            MessageBox.Show(message, title, MessageBoxButton.OK, icon, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private LogEntry SaveLog(string title, DateTime startTime, TimeSpan elapsedTime)
        {
            LogEntry logEntry = new LogEntry
            {
                Title = title,
                StartTime = startTime.ToString("yyyy/MM/dd HH:mm:ss"),
                ElapsedTime = elapsedTime.ToString(@"mm\:ss")
            };

            try
            {
                bool fileExists = File.Exists(logFilePath);

                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    if (!fileExists)
                    {
                        sw.WriteLine("タイトル,開始時間,経過時間"); // CSVのヘッダー行
                    }
                    sw.WriteLine($"{logEntry.Title},{logEntry.StartTime},{logEntry.ElapsedTime}");
                }
            }
            catch (Exception ex)
            {
                ShowAlert($"CSVログの保存に失敗しました。\n{ex.Message}", "エラー", isError: true);
            }

            return logEntry;
        }

        private void LoadLog()
        {
            if (File.Exists(logFilePath))
            {
                string[] logEntries = File.ReadAllLines(logFilePath).Skip(1).ToArray(); // ヘッダーをスキップ
                foreach (string entry in logEntries)
                {
                    string[] columns = entry.Split(',');
                    if (columns.Length == 3)
                    {
                        logListView.Items.Insert(0, new LogEntry
                        {
                            Title = columns[0],
                            StartTime = columns[1],
                            ElapsedTime = columns[2]
                        });
                    }
                }
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            logListView.Items.Clear();
            try
            {
                File.WriteAllText(logFilePath, ""); // CSVの内容をクリア
            }
            catch (Exception ex)
            {
                ShowAlert($"CSVログのクリアに失敗しました。\n{ex.Message}", "エラー", isError: true);
            }
        }
    }

    public class LogEntry
    {
        public string Title { get; set; }
        public string StartTime { get; set; }
        public string ElapsedTime { get; set; }
    }
}
