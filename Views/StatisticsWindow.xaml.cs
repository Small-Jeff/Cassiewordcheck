using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class StatisticsWindow : Window
{
    private const double MarginLeft = 50;
    private const double MarginRight = 20;
    private const double MarginTop = 20;
    private const double MarginBottom = 40;

    private static readonly Brush GridLineBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3A));
    private static readonly Brush AxisBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly Brush CoverageBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
    private static readonly Brush UnavailableBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
    private static readonly Brush PointFill = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));

    private readonly HistoryStore _store;
    private readonly LocalizationService _localization;
    private bool _loaded;

    public StatisticsWindow(HistoryStore store, LocalizationService localization)
    {
        InitializeComponent();
        _store = store;
        _localization = localization;
        this.EnableDarkTitleBar();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;

        UpdateLabels();
        RenderChart();
    }

    private void UpdateLabels()
    {
        Title = _localization["stats.title"];
        TitleLabel.Text = "📊 " + _localization["stats.title"];

        if (ViewSelector.Items.Count >= 2)
        {
            ((ComboBoxItem)ViewSelector.Items[0]).Content = _localization["stats.coverage_trend"];
            ((ComboBoxItem)ViewSelector.Items[1]).Content = _localization["stats.unavailable_trend"];
        }

        EmptyLabel.Text = _localization["stats.no_data"];
    }

    private void OnViewChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded) return;
        RenderChart();
    }

    private void RenderChart()
    {
        try
        {
            ChartCanvas.Children.Clear();

            if (!_store.Items.Any())
            {
                EmptyLabel.Visibility = Visibility.Visible;
                TotalChecksLabel.Text = "检查：0 次";
                AvgCoverageLabel.Text = "平均：0%";
                WorstCoverageLabel.Text = "最低：0%";
                TotalUnavailableLabel.Text = "不可用词合计：0";
                return;
            }
            EmptyLabel.Visibility = Visibility.Collapsed;

            // 推迟到 Dispatcher 空闲时画图，确保 Canvas 已布局完成
            Dispatcher.BeginInvoke(() => DoRender());
        }
        catch
        {
            // 静默
        }
    }

    private void DoRender()
    {
        var w = ChartCanvas.ActualWidth;
        var h = ChartCanvas.ActualHeight;
        if (w < 50 || h < 50) return;

        var items = _store.Items;
        var viewMode = ViewSelector.SelectedItem is ComboBoxItem ci && ci.Tag is string tag
            ? tag : "coverage";

        var plotW = w - MarginLeft - MarginRight;
        var plotH = h - MarginTop - MarginBottom;
        if (plotW < 10 || plotH < 10) return;

        // ── 准备数据 ──
        var data = items.Select((item, i) => new
        {
            Index = i,
            Value = viewMode == "coverage" ? item.Coverage : (double)item.Unavailable,
            Label = item.Timestamp.ToString("MM/dd"),
        }).ToList();

        var dataMax = viewMode == "coverage"
            ? 100.0
            : Math.Max(data.Max(d => d.Value) * 1.2, 10.0);
        if (dataMax <= 0) dataMax = 10.0;

        var lineBrush = viewMode == "coverage" ? CoverageBrush : UnavailableBrush;

        // ── 网格线 ──
        for (int i = 0; i <= 5; i++)
        {
            double y = MarginTop + plotH - (plotH * i / 5.0);
            ChartCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = MarginLeft, Y1 = y,
                X2 = MarginLeft + plotW, Y2 = y,
                Stroke = GridLineBrush,
                StrokeThickness = 0.5,
            });

            double labelVal = dataMax * i / 5.0;
            var tb = new TextBlock
            {
                Text = viewMode == "coverage" ? $"{labelVal:F0}%" : $"{(int)labelVal}",
                Foreground = AxisBrush,
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI, Consolas"),
            };
            Canvas.SetLeft(tb, 2);
            Canvas.SetTop(tb, y - 7);
            ChartCanvas.Children.Add(tb);
        }

        // ── 折线图 ──
        if (data.Count >= 2)
        {
            var points = data.Select(d =>
            {
                double px = MarginLeft + plotW * d.Index / (data.Count - 1);
                double rawY = MarginTop + plotH - (plotH * d.Value / dataMax);
                return (x: px, y: double.IsNaN(rawY) || double.IsInfinity(rawY) ? MarginTop + plotH : rawY);
            }).ToList();

            for (int i = 0; i < points.Count - 1; i++)
            {
                ChartCanvas.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = points[i].x, Y1 = points[i].y,
                    X2 = points[i + 1].x, Y2 = points[i + 1].y,
                    Stroke = lineBrush,
                    StrokeThickness = 2,
                    StrokeEndLineCap = PenLineCap.Round,
                });
            }

            foreach (var pt in points)
            {
                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 6, Height = 6,
                    Fill = PointFill,
                    Stroke = lineBrush,
                    StrokeThickness = 2,
                };
                Canvas.SetLeft(dot, pt.x - 3);
                Canvas.SetTop(dot, pt.y - 3);
                ChartCanvas.Children.Add(dot);
            }

            // X 轴标签
            int[] labelIndices = [0, data.Count / 2, data.Count - 1];
            foreach (int idx in labelIndices)
            {
                if (idx < 0 || idx >= points.Count) continue;
                var tb = new TextBlock
                {
                    Text = data[idx].Label,
                    Foreground = AxisBrush,
                    FontSize = 10,
                };
                Canvas.SetLeft(tb, points[idx].x - 20);
                Canvas.SetTop(tb, MarginTop + plotH + 8);
                ChartCanvas.Children.Add(tb);
            }
        }
        else if (data.Count == 1)
        {
            double cx = MarginLeft + plotW / 2;
            double rawY = MarginTop + plotH - (plotH * data[0].Value / dataMax);
            double cy = double.IsNaN(rawY) || double.IsInfinity(rawY) ? MarginTop + plotH : rawY;
            var dot = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Fill = lineBrush };
            Canvas.SetLeft(dot, cx - 4);
            Canvas.SetTop(dot, cy - 4);
            ChartCanvas.Children.Add(dot);
        }

        // ── 底部统计 ──
        TotalChecksLabel.Text = $"检查：{items.Count} 次";
        AvgCoverageLabel.Text = $"平均：{items.Average(i => i.Coverage):F1}%";
        WorstCoverageLabel.Text = $"最低：{items.Min(i => i.Coverage):F1}%";
        TotalUnavailableLabel.Text = $"不可用词合计：{items.Sum(i => i.Unavailable)}";
    }
}
