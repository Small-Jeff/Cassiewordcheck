using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CassieWordCheck.Models;

namespace CassieWordCheck.Views;

public partial class HistoryWindow : Window
{
    private readonly HistoryStore _store;
    private readonly Action<string> _onRestoreText;

    public HistoryWindow(HistoryStore store, Action<string> onRestoreText)
    {
        InitializeComponent();
        _store = store;
        _onRestoreText = onRestoreText;

        this.EnableDarkTitleBar();
        RefreshList();
    }

    private void OnItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is HistoryItemDisplay item)
        {
            DialogResult = true;
            _onRestoreText(item.InputText);
            Close();
        }
    }

    private void OnClearAll(object sender, RoutedEventArgs e)
    {
        if (_store.Count == 0) return;

        var result = MessageBox.Show("确定清空所有检查历史吗？", "检查历史",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _store.Clear();
            RefreshList();
        }
    }

    private void RefreshList()
    {
        var items = _store.Items
            .Reverse()
            .Select((h, i) => new HistoryItemDisplay
            {
                TimeLabel = $"#{_store.Count - i}  {h.Timestamp:yyyy-MM-dd HH:mm:ss}",
                StatsLabel = $"✓{h.Available}  ✗{h.Unavailable}  䨻{h.Ignored}  {h.Coverage:F1}%",
                Preview = h.InputText.Length > 120
                    ? h.InputText[..120] + "…"
                    : h.InputText,
                ResultPreview = ExtractPlainText(h.ResultText),
                InputText = h.InputText,
            })
            .ToList();

        HistoryList.ItemsSource = items;
        EmptyLabel.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string ExtractPlainText(string html)
    {
        if (string.IsNullOrEmpty(html)) return "（无结果）";
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
        text = System.Net.WebUtility.HtmlDecode(text);
        return text.Length > 80 ? text[..80] + "…" : text;
    }
}

public class HistoryItemDisplay
{
    public string TimeLabel { get; init; } = "";
    public string StatsLabel { get; init; } = "";
    public string Preview { get; init; } = "";
    public string ResultPreview { get; init; } = "";
    public string InputText { get; init; } = "";
}
