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
        private string _selectedTag;
        private readonly string logFilePath = "log.csv";             // CSV保存ファイル
        private readonly string tagHistoryFilePath = "tagHistory.txt"; // タグ履歴の保存ファイル
        private readonly string titleHistoryFilePath = "titleHistory.txt"; // タイトル履歴の保存ファイル

        private HashSet<string> tagHistory = new HashSet<string>();    // タグ履歴
        private HashSet<string> titleHistory = new HashSet<string>();  // タイトル履歴

        public bool isPaused { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(1); // 1秒ごとに更新

            LoadLog(); // CSVからログを読み込む

            btnStop.IsEnabled = false; // 初期状態で停止ボタンは無効
            btnStart.IsEnabled = false; // 初期状態で開始ボタンも無効
            btnPause.IsEnabled = false;
            cmbTitle.IsEnabled = true;
            cmbMinutes.IsEnabled = true;
            cmbTags.IsEnabled = true;

            // **ComboBox の値を設定 (5分～60分)**
#if DEBUG
            cmbMinutes.Items.Add(1);
#endif
            // コンボボックス（cmbMinutes）の選択肢を追加 (5分～60分, 5分刻み)
            for (int i = 5; i <= 60; i += 5)
            {
                cmbMinutes.Items.Add(i);
            }
            cmbMinutes.SelectedIndex = -1; // 初期選択なし

            // タグ履歴の読み込み
            if (File.Exists(tagHistoryFilePath))
            {
                string[] savedTags = File.ReadAllLines(tagHistoryFilePath);
                foreach (var tag in savedTags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        tagHistory.Add(tag);
                        cmbTags.Items.Add(tag);
                    }
                }
            }

            // タイトル履歴の読み込み
            if (File.Exists(titleHistoryFilePath))
            {
                string[] savedTitles = File.ReadAllLines(titleHistoryFilePath);
                foreach (var title in savedTitles)
                {
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        titleHistory.Add(title);
                        cmbTitle.Items.Add(title);
                    }
                }
            }
        }
        // タイトル、時間、タグの入力チェック（TextChanged/SelectionChanged共通イベントハンドラー）
        private void TxtInputChanged(object sender, EventArgs e)
        {
            bool isTitleValid = !string.IsNullOrWhiteSpace(cmbTitle.Text);
            bool isMinutesValid = cmbMinutes.SelectedItem != null;
            // タグは任意入力なのでチェック対象外

            btnStart.IsEnabled = isTitleValid && isMinutesValid;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (cmbMinutes.SelectedItem == null)
            {
                ShowAlert("時間を選択してください。", "エラー", isError: true);
                return;
            }

            _intervalMinutes = (int)cmbMinutes.SelectedItem;
            _startTime = DateTime.Now;
            _nextAlertTime = _startTime.AddMinutes(_intervalMinutes);
            _remainingTime = TimeSpan.FromMinutes(_intervalMinutes);
            _alertTitle = cmbTitle.Text;      // タイトル取得
            _selectedTag = cmbTags.Text;      // タグ取得

            // タイトル履歴にない場合、保存＆ComboBoxへ追加
            if (!string.IsNullOrWhiteSpace(_alertTitle) && !titleHistory.Contains(_alertTitle))
            {
                titleHistory.Add(_alertTitle);
                cmbTitle.Items.Add(_alertTitle);
                File.AppendAllText(titleHistoryFilePath, _alertTitle + Environment.NewLine);
            }

            // タグ履歴にない場合、保存＆ComboBoxへ追加
            if (!string.IsNullOrWhiteSpace(_selectedTag) && !tagHistory.Contains(_selectedTag))
            {
                tagHistory.Add(_selectedTag);
                cmbTags.Items.Add(_selectedTag);
                File.AppendAllText(tagHistoryFilePath, _selectedTag + Environment.NewLine);
            }

            lblStartTime.Text = $"スタート時間: {_startTime:HH:mm:ss}";
            lblNextAlert.Text = $"次のアラート時間: {_nextAlertTime:HH:mm:ss}";
            lblCountdown.Text = $"残り時間: {_remainingTime:mm\\:ss}";

            _timer.Start();

            // 入力とボタンの制御
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnPause.IsEnabled = true;
            cmbTitle.IsEnabled = false;
            cmbMinutes.IsEnabled = false;
            cmbTags.IsEnabled = false;

            //ShowAlert($"アラートを {_intervalMinutes} 分ごとに設定しました。\nタイトル: {_alertTitle}\nタグ: {_selectedTag}", "開始");
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            TimeSpan elapsedTime = DateTime.Now - _startTime;

            lblCountdown.Text = "カウントダウン: --:--";
            lblElapsedTime.Text = $"経過時間: {elapsedTime:mm\\:ss}";

            // CSVにログを保存
            LogEntry newLog = SaveLog(_alertTitle, _startTime, elapsedTime, _selectedTag);

            // UIのListViewに追加
            logListView.Items.Insert(0, newLog);

            // 入力とボタンの制御
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnPause.IsEnabled = false;
            cmbTitle.IsEnabled = true;
            cmbMinutes.IsEnabled = true;
            cmbTags.IsEnabled = true;

            //ShowAlert($"アラートを停止しました。\nタイトル: {_alertTitle}\nタグ: {_selectedTag}\n経過時間: {elapsedTime:mm\\:ss}", "停止");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isPaused) return;

            _remainingTime = _nextAlertTime - DateTime.Now;

            if (_remainingTime.TotalSeconds <= 0)
            {
                // 指定時間経過時、カスタムダイアログを表示
                _timer.Stop();
                AlertDialog dialog = new AlertDialog($"【{_alertTitle}】\nタグ: {_selectedTag}\n指定時間 {_intervalMinutes} 分が経過しました！");
                dialog.Owner = this;
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    if (dialog.Result == AlertDialogResult.Pause)
                    {
                        isPaused = true;
                        btnPause.Content = "再開";
                        //ShowAlert("タイマーを一時停止しました。", "一時停止");
                        return; // 一時停止状態
                    }
                    else if (dialog.Result == AlertDialogResult.Stop)
                    {
                        BtnStop_Click(null, null);
                        return;
                    }
                    else if (dialog.Result == AlertDialogResult.Continue)
                    {
                        // 継続が選択された場合、次のアラート時刻をリセットして再開
                        _nextAlertTime = DateTime.Now.AddMinutes(_intervalMinutes);
                        _timer.Start();
                    }
                }
                else
                {
                    // ダイアログでキャンセルされた場合も、次のインターバルで再開
                    _nextAlertTime = DateTime.Now.AddMinutes(_intervalMinutes);
                    _timer.Start();
                }
            }
            lblCountdown.Text = $"残り時間: {_remainingTime:mm\\:ss}";
            lblNextAlert.Text = $"次のアラート時間: {_nextAlertTime:HH:mm:ss}";
        }
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaused)
            {
                _timer.Stop();
                isPaused = true;
                btnPause.Content = "再開"; // 一時停止状態
                //ShowAlert("タイマーを一時停止しました。", "一時停止");
            }
            else
            {
                // **一時停止解除（再開）**
                _nextAlertTime = DateTime.Now.Add(_remainingTime); // 残り時間を適用
                _timer.Start();
                isPaused = false;
                btnPause.Content = "一時停止"; // 再開時
                //ShowAlert("タイマーを再開しました。", "再開");
            }
        }
        private void ShowAlert(string message, string title, bool isWarning = false, bool isError = false)
        {
            MessageBoxImage icon = isWarning ? MessageBoxImage.Warning :
                                     isError ? MessageBoxImage.Error :
                                     MessageBoxImage.Information;
            MessageBox.Show(message, title, MessageBoxButton.OK, icon, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private LogEntry SaveLog(string title, DateTime startTime, TimeSpan elapsedTime, string tag)
        {
            LogEntry logEntry = new LogEntry
            {
                Title = title,
                StartTime = startTime.ToString("yyyy/MM/dd HH:mm:ss"),
                ElapsedTime = elapsedTime.ToString(@"mm\:ss"),
                Tag = tag
            };

            try
            {
                bool fileExists = File.Exists(logFilePath);
                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    if (!fileExists)
                    {
                        sw.WriteLine("タイトル,開始時間,経過時間,タグ");
                    }
                    sw.WriteLine($"{logEntry.Title},{logEntry.StartTime},{logEntry.ElapsedTime},{logEntry.Tag}");
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
                    if (columns.Length == 4)
                    {
                        logListView.Items.Insert(0, new LogEntry
                        {
                            Title = columns[0],
                            StartTime = columns[1],
                            ElapsedTime = columns[2],
                            Tag = columns[3]
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

        private void logListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (logListView.SelectedItem is LogEntry selectedLog)
            {
                // **タイトルとタグをメインウィンドウの入力欄に反映**
                cmbTitle.Text = selectedLog.Title;
                cmbTags.Text = selectedLog.Tag;
            }
        }
        private void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            // ログデータを収集
            List<LogEntry> logEntries = logListView.Items.Cast<LogEntry>().ToList();

            if (logEntries.Count == 0)
            {
                ShowAlert("ログデータがありません。", "分析エラー", isError: true);
                return;
            }

            // 分析ダイアログを開く
            AnalysisDialog analysisDialog = new AnalysisDialog(logEntries);
            analysisDialog.Owner = this;
            analysisDialog.ShowDialog();
        }
    }

    public class LogEntry
    {
        public string Title { get; set; }
        public string StartTime { get; set; }
        public string ElapsedTime { get; set; }
        public string Tag { get; set; }
    }
}
