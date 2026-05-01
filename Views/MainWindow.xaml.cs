using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class MainWindow : Window
{
    private readonly WordList _wordlist;
    private readonly Checker _checker;
    private readonly Settings _settings;
    private readonly LocalizationService _localization;

    public MainWindow()
    {
        InitializeComponent();

        _wordlist = new WordList();
        _checker = new Checker(_wordlist);
        _settings = new Settings();
        _localization = new LocalizationService();

        // 应用设置
        _checker.IgnoreChinese = _settings.IgnoreChinese;
        _checker.IgnoreAngleBrackets = _settings.IgnoreAngleBrackets;

        if (_settings.Whitelist.Count > 0)
            _wordlist.SetWhitelist(_settings.Whitelist);

        _localization.SetLanguage(_settings.Language);

        // 深色标题栏 + Mica
        this.EnableDarkTitleBar();

        // 加载语言选择器
        PopulateLanguages();

        // 加载词库
        LoadWordListAsync();

        // 更新界面语言
        UpdateUILanguage();
    }

    private void PopulateLanguages()
    {
        LanguageSelector.Items.Clear();
        foreach (var (display, code) in _localization.AvailableLanguages())
        {
            LanguageSelector.Items.Add(new LanguageItem(display, code));
            if (code == _localization.CurrentLanguage)
                LanguageSelector.SelectedIndex = LanguageSelector.Items.Count - 1;
        }

        if (LanguageSelector.Items.Count > 0 && LanguageSelector.SelectedIndex < 0)
            LanguageSelector.SelectedIndex = 0;
    }

    private async void LoadWordListAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var path = _settings.WordlistPath;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    var paths = new[]
                    {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "cassie-text.txt"),
                        Path.Combine(Directory.GetCurrentDirectory(), "data", "cassie-text.txt"),
                        Path.Combine(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")), "data", "cassie-text.txt"),
                    };
                    path = paths.FirstOrDefault(File.Exists) ?? "";
                }

                if (File.Exists(path))
                {
                    var count = _wordlist.LoadFromFile(path);
                    Dispatcher.Invoke(() =>
                    {
                        WordListInfo.Text = $"词库：{count} 个单词";
                    });
                }
                else
                {
                    Dispatcher.Invoke(() => WordListInfo.Text = "⚠ 未找到词库文件");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => WordListInfo.Text = $"⚠ {ex.Message}");
            }
        });
    }

    private void OnInputTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateResult();
    }

    private void UpdateResult()
    {
        var text = InputBox.Text;
        CharCountLabel.Text = $"{text.Length} 字符";

        var results = _checker.CheckText(text);
        var stats = _checker.GetStatistics(text);

        ResultBox.Document = DocumentBuilder.BuildResultDocument(results, ResultBox.ActualWidth);

        var available = (int)stats["available"];
        var unavailable = (int)stats["unavailable"];
        var ignored = (int)stats["ignored"];
        var coverage = (double)stats["coverage"];

        AvailableLabel.Text = $"可用：{available}";
        UnavailableLabel.Text = $"不可用：{unavailable}";
        IgnoredLabel.Text = $"已忽略：{ignored}";
        CoverageLabel.Text = $"{coverage:F1}%";
        CoverageBar.Width = Math.Max(2, Math.Min(120, coverage * 1.2));

        // 建议面板
        var unavailableWords = results
            .Where(r => r.Status == CheckStatus.Unavailable)
            .Select(r => r.Text)
            .Distinct()
            .ToList();

        if (unavailableWords.Count > 0)
        {
            SuggestionsPanel.Visibility = Visibility.Visible;
            SuggestionsList.ItemsSource = unavailableWords
                .Select(w => new
                {
                    Original = w,
                    SuggestionText = FormatSuggestions(w)
                })
                .ToList();
        }
        else
        {
            SuggestionsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private string FormatSuggestions(string word)
    {
        var closest = LevenshteinHelper.FindClosest(word, _wordlist.Words, 3);
        return closest.Count > 0
            ? $"→ {string.Join(", ", closest.Select(c => c.word))}"
            : "— 词库中无相近词";
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageSelector.SelectedItem is LanguageItem item)
        {
            _localization.SetLanguage(item.Code);
            _settings.Language = item.Code;
            _settings.Save();
            UpdateUILanguage();
        }
    }

    private void UpdateUILanguage()
    {
        if (_wordlist.WordCount > 0)
        {
            WordListInfo.Text = $"词库：{_wordlist.WordCount} 个单词";
        }
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings, _checker, _wordlist, _localization);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            UpdateResult();
        }
    }

    private void OnOpenWhitelist(object sender, RoutedEventArgs e)
    {
        var dialog = new WhitelistWindow(_wordlist, _localization);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            _settings.Whitelist = _wordlist.Whitelist.ToList();
            _settings.Save();
            UpdateResult();
        }
    }

    private void OnReloadWordlist(object sender, RoutedEventArgs e)
    {
        try
        {
            var count = _wordlist.Reload();
            WordListInfo.Text = $"词库：{count} 个单词";
            UpdateResult();
        }
        catch (Exception ex)
        {
            WordListInfo.Text = $"⚠ {ex.Message}";
        }
    }

    private void OnCopyResult(object sender, RoutedEventArgs e)
    {
        var text = new TextRange(ResultBox.Document.ContentStart, ResultBox.Document.ContentEnd).Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            Clipboard.SetText(text.TrimEnd('\r', '\n'));
        }
    }

    private record LanguageItem(string Display, string Code)
    {
        public override string ToString() => Display;
    }
}
