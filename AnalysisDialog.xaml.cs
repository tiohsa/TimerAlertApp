using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using System.Reflection.Emit;

namespace TimerAlertApp
{
    public partial class AnalysisDialog : Window
    {
        private List<LogEntry> allLogEntries; // 全ログデータ
        private bool isBarChart = false; // **現在のグラフの種類（false = 円グラフ, true = 棒グラフ）**

        public AnalysisDialog(List<LogEntry> logEntries)
        {
            InitializeComponent();
            allLogEntries = logEntries;

            // デフォルトで当日を選択
            SelectToday();
        }

        // **グラフの種類を切り替え**
        private void ToggleGraphType(object sender, RoutedEventArgs e)
        {
            isBarChart = !isBarChart;
            btnToggleGraph.Content = isBarChart ? "円グラフに切替" : "棒グラフに切替";
            UpdateCharts();
        }

        // **選択範囲のログデータを集計し、グラフを更新**
        private void UpdateCharts()
        {
            if (!dpStartDate.SelectedDate.HasValue || !dpEndDate.SelectedDate.HasValue)
                return;

            DateTime startDate = dpStartDate.SelectedDate.Value;
            DateTime endDate = dpEndDate.SelectedDate.Value.AddDays(1).AddTicks(-1); // 終日を含む

            // **選択範囲のログデータをフィルタ**
            var filteredLogs = allLogEntries
                .Where(entry => DateTime.TryParse(entry.StartTime, out DateTime logDate) &&
                                logDate >= startDate && logDate <= endDate)
                .ToList();

            // **タグ別・タイトル別の合計時間（秒）を計算**
            Dictionary<string, double> tagTimeSums = filteredLogs
                .Where(entry => !string.IsNullOrEmpty(entry.Tag))
                .GroupBy(entry => entry.Tag)
                .ToDictionary(g => g.Key, g => g.Sum(entry => ParseElapsedTime(entry.ElapsedTime)));

            Dictionary<string, double> titleTimeSums = filteredLogs
                .Where(entry => !string.IsNullOrEmpty(entry.Title))
                .GroupBy(entry => entry.Title)
                .ToDictionary(g => g.Key, g => g.Sum(entry => ParseElapsedTime(entry.ElapsedTime)));

            // **グラフを更新（現在のグラフタイプに応じて変更）**
            if (isBarChart)
            {
                TagChartImage.Source = CreateBarChart(tagTimeSums, "タグ別作業時間");
                TitleChartImage.Source = CreateBarChart(titleTimeSums, "タイトル別作業時間");
            }
            else
            {
                TagChartImage.Source = CreatePieChart(tagTimeSums, "タグ別作業時間");
                TitleChartImage.Source = CreatePieChart(titleTimeSums, "タイトル別作業時間");
            }
        }
        // **秒数を "MM:SS" 形式に変換**
        private string FormatTime(double totalSeconds)
        {
            int minutes = (int)(totalSeconds / 60);
            int seconds = (int)(totalSeconds % 60);
            return $"{minutes:D2}:{seconds:D2}"; // **"MM:SS" 形式にフォーマット**
        }
        // **"MM:SS" を 秒単位 に変換**
        private double ParseElapsedTime(string elapsedTime)
        {
            if (TimeSpan.TryParseExact(elapsedTime, "mm\\:ss", null, out TimeSpan time))
            {
                return time.TotalSeconds; // **秒単位で返す**
            }
            return 0; // 無効なデータの場合は0を返す
        }

        // **円グラフを作成**
        private BitmapSource CreatePieChart(Dictionary<string, double> data, string title)
        {
            var model = new PlotModel { Title = title };

            var pieSeries = new PieSeries
            {
                InsideLabelFormat = "{1}",
                OutsideLabelFormat = "{0}%"
            };

            var total = data.Sum(item => item.Value);

            foreach (var item in data)
            {
                var label = $"{item.Key}: {FormatTime(item.Value)}";
                double totalMinutes = Math.Round(item.Value / total, 2) * 100;
                pieSeries.Slices.Add(new PieSlice(label, totalMinutes));
            }

            model.Series.Add(pieSeries);
            return RenderPlotModel(model);
        }
        private void SelectToday(object sender = null, RoutedEventArgs e = null)
        {
            dpStartDate.SelectedDate = DateTime.Today;
            dpEndDate.SelectedDate = DateTime.Today;
            UpdateCharts();
        }
        // **今週を選択**
        private void SelectThisWeek(object sender = null, RoutedEventArgs e = null)
        {
            DateTime today = DateTime.Today;
            int diff = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
            DateTime startOfWeek = today.AddDays(-diff);
            DateTime endOfWeek = startOfWeek.AddDays(6);

            dpStartDate.SelectedDate = startOfWeek;
            dpEndDate.SelectedDate = endOfWeek;
            UpdateCharts();
        }

        // **今月を選択**
        private void SelectThisMonth(object sender = null, RoutedEventArgs e = null)
        {
            DateTime today = DateTime.Today;
            DateTime startOfMonth = new DateTime(today.Year, today.Month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            dpStartDate.SelectedDate = startOfMonth;
            dpEndDate.SelectedDate = endOfMonth;
            UpdateCharts();
        }
        // **日付範囲が変更されたときの処理**
        private void DateRangeChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCharts();
        }

        // **棒グラフを作成**
        private BitmapSource CreateBarChart(Dictionary<string, double> data, string title)
        {
            var model = new PlotModel { Title = title };

            var categoryAxis = new CategoryAxis { Position = AxisPosition.Left };
            var valueAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "作業時間 (分)" };

            var barSeries = new BarSeries { LabelPlacement = LabelPlacement.Inside, LabelFormatString = "{0}" };

            var total = data.Sum(item => item.Value);

            foreach (var item in data)
            {
                double totalMinutes = item.Value;
                barSeries.Items.Add(new BarItem { Value = totalMinutes });
                categoryAxis.Labels.Add(item.Key);
            }

            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);
            model.Series.Add(barSeries);

            return RenderPlotModel(model);
        }

        // **グラフを画像としてレンダリング**
        private BitmapSource RenderPlotModel(PlotModel model)
        {
            var pngExporter = new PngExporter { Width = 400, Height = 300 };
            using (var stream = new MemoryStream())
            {
                pngExporter.Export(model, stream);
                stream.Seek(0, SeekOrigin.Begin);
                var bitmap = new Bitmap(stream);
                return BitmapToBitmapSource(bitmap);
            }
        }

        // **Bitmap → WPFのImageに変換**
        private BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                return decoder.Frames[0];
            }
        }
    }
}
