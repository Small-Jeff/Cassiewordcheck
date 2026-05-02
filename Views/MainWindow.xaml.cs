using System.Windows.Media.Animation;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class MainWindow : Window
{
    private readonly WordList _wordlist;
    private readonly Checker _checker;
    private readonly Settings _settings;
    private readonly LocalizationService _localization;
    private bool _isLanguageInit = true;
    private bool _suppressAnimation;

    public MainWindow()
    {
        InitializeComponent();

        _wordlist = new WordList();
        _checker = new Checker(_wordlist);
        _settings = new Settings();
        _localization = new LocalizationService();

        _checker.IgnoreChinese = _settings.IgnoreChinese;
        _checker.FilterFormatting = _settings.FilterFormatting;
        _checker.FilterNaming = _settings.FilterNaming;

        if (_settings.Whitelist.Count > 0)
            _wordlist.SetWhitelist(_settings.Whitelist);

        _localization.SetLanguage(_settings.Language);

        this.EnableDarkTitleBar();

        InputBox.FontSize = _settings.FontSize;
        InputBox.TextWrapping = _settings.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        ResultBox.FontSize = _settings.FontSize;

        PopulateLanguages();
        _isLanguageInit = false;

        LoadWordListAsync();
        UpdateUILanguage();
    }

    // ── 入场动画 ─────────────────────────────────────────────────
    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        // ── 顶部工具栏：顶部滑入 ──
        ToolbarCard.Opacity = 0;
        var toolbarT = new TranslateTransform(0, -15);
        ToolbarCard.RenderTransform = toolbarT;
        ToolbarCard.RenderTransformOrigin = new Point(0.5, 0.5);
        Animate(ToolbarCard, UIElement.OpacityProperty, 0, 1, 350, new QuadraticEase());
        Animate(toolbarT, TranslateTransform.YProperty, -15, 0, 400, new QuadraticEase());

        // ── 主内容区（输入/结果卡片）──
        var cards = MainContentGrid;
        cards.Opacity = 0;
        var ct = (TranslateTransform)cards.RenderTransform;
        ct.Y = 25;
        Animate(cards, UIElement.OpacityProperty, 0, 1, 400, new QuadraticEase());
        Animate(ct, TranslateTransform.YProperty, 25, 0, 500, new QuadraticEase());

        // ── 两卡片从中心 0.15x 同步弹开 ──
        var inputScale = (ScaleTransform)InputCard.RenderTransform;
        var resultGroup = (TransformGroup)ResultCard.RenderTransform;
        var resultScale = (ScaleTransform)resultGroup.Children[1];

        inputScale.ScaleX = 0.15;
        inputScale.ScaleY = 0.15;
        InputCard.Opacity = 0;
        resultScale.ScaleX = 0.15;
        resultScale.ScaleY = 0.15;
        ResultCard.Opacity = 0;

        var cardEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 };
        const int cardD = 480;

        Animate(inputScale, ScaleTransform.ScaleXProperty, 0.15, 1, cardD, cardEase);
        Animate(inputScale, ScaleTransform.ScaleYProperty, 0.15, 1, cardD, cardEase);
        Animate(InputCard, UIElement.OpacityProperty, 0, 1, cardD - 100, new QuadraticEase());
        Animate(resultScale, ScaleTransform.ScaleXProperty, 0.15, 1, cardD, cardEase);
        Animate(resultScale, ScaleTransform.ScaleYProperty, 0.15, 1, cardD, cardEase);
        Animate(ResultCard, UIElement.OpacityProperty, 0, 1, cardD - 100, new QuadraticEase());

        // ── 统计栏 ──
        StatsBar.Opacity = 0;
        Animate(StatsBar, UIElement.OpacityProperty, 0, 1, 550, new QuadraticEase());
    }

    // ── 输入变化 ─────────────────────────────────────────────────
    private void OnInputTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_suppressAnimation)
            UpdateResult();
    }

    private void UpdateResult()
    {
        var text = InputBox.Text;
        CharCountLabel.Text = string.Format(_localization["stats.chars"], text.Length);

        var results = _checker.CheckText(text);
        var stats = _checker.GetStatistics(text);

        ResultBox.Document = DocumentBuilder.BuildResultDocument(results, ResultBox.ActualWidth, _settings.FontSize);

        var available = (int)stats["available"];
        var unavailable = (int)stats["unavailable"];
        var ignored = (int)stats["ignored"];
        var coverage = (double)stats["coverage"];

        AvailableLabel.Text = string.Format(_localization["stats.available"], available);
        UnavailableLabel.Text = string.Format(_localization["stats.unavailable"], unavailable);
        IgnoredLabel.Text = string.Format(_localization["stats.ignored"], ignored);
        CoverageLabel.Text = $"{coverage:F1}%";

        // 进度条过渡
        var targetWidth = Math.Max(2, Math.Min(120, coverage * 1.2));
        Animate(CoverageBar, FrameworkElement.WidthProperty,
            CoverageBar.ActualWidth, targetWidth, 400, new QuadraticEase());

        // 输入实时同步 → 结果卡片"键入"微动
        var resultGroup = (TransformGroup)ResultCard.RenderTransform;
        var resultScale = (ScaleTransform)resultGroup.Children[1];
        ResultCard.RenderTransformOrigin = new Point(0.5, 0.5);

        var typingBounce = new DoubleAnimation(1, 1.015, new Duration(TimeSpan.FromMilliseconds(80)))
        { AutoReverse = true, EasingFunction = new QuadraticEase() };
        resultScale.BeginAnimation(ScaleTransform.ScaleXProperty, typingBounce);
        resultScale.BeginAnimation(ScaleTransform.ScaleYProperty, (DoubleAnimation)typingBounce.Clone());

        // 建议面板 + 卡片整体上移动画（清空/退格时跳过）
        if (_suppressAnimation) return;

        var unavailableWords = results
            .Where(r => r.Status == CheckStatus.Unavailable)
            .Select(r => r.Text)
            .Distinct()
            .ToList();

        if (unavailableWords.Count > 0)
        {
            if (SuggestionsPanel.Visibility != Visibility.Visible)
            {
                // 先设 Visible 但高度为 0，然后动画展开 → 卡片被推上移
                SuggestionsPanel.Visibility = Visibility.Visible;
                SuggestionsPanel.MaxHeight = 0;
                SuggestionsPanel.Opacity = 0;

                var st = (TranslateTransform)SuggestionsPanel.RenderTransform;
                st.Y = 20;

                Animate(SuggestionsPanel, UIElement.OpacityProperty, 0, 1, 250, new QuadraticEase());
                Animate(st, TranslateTransform.YProperty, 20, 0, 300, new QuadraticEase());

                var heightAnim = new DoubleAnimation(250, new Duration(TimeSpan.FromMilliseconds(350)))
                { EasingFunction = new QuadraticEase() };
                SuggestionsPanel.BeginAnimation(FrameworkElement.MaxHeightProperty, heightAnim);
            }

            SuggestionsList.ItemsSource = unavailableWords
                .Select(w => new { Original = w, SuggestionText = FormatSuggestions(w) })
                .ToList();
        }
        else if (SuggestionsPanel.Visibility == Visibility.Visible)
        {
            // 高度归零 → 卡片自动下移回原位
            var heightAnim = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(200)));
            heightAnim.Completed += (_, _) =>
            {
                SuggestionsPanel.Visibility = Visibility.Collapsed;
                SuggestionsPanel.BeginAnimation(FrameworkElement.MaxHeightProperty, null);
            };
            SuggestionsPanel.BeginAnimation(FrameworkElement.MaxHeightProperty, heightAnim);

            var fadePanel = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(150)));
            SuggestionsPanel.BeginAnimation(UIElement.OpacityProperty, fadePanel);
        }
    }

    // ── 清空输入：平滑淡出 + 上移，替代逐字删除 ────────────────
    private void OnClearInput(object sender, RoutedEventArgs e)
    {
        if (InputBox.Text.Length == 0) return;

        _suppressAnimation = true;

        // 输入框淡出
        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(200)));
        fadeOut.Completed += (_, _) =>
        {
            InputBox.Clear();
            _suppressAnimation = false;

            // 先同步结果，再恢复所有控件亮度
            UpdateResult();
            Animate(InputBox, UIElement.OpacityProperty, 0, 1, 120, null);
            Animate(ResultCard, UIElement.OpacityProperty, 0.5, 1, 200, new QuadraticEase());
            InputBox.Focus();
        };
        InputBox.BeginAnimation(TextBox.OpacityProperty, fadeOut);

        // 结果卡片同步变暗
        Animate(ResultCard, UIElement.OpacityProperty, 1, 0.5, 200, new QuadraticEase());
    }

    // ── 退格检测 ─────────────────────────────────────────────────
    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back || e.Key == Key.Delete)
            _suppressAnimation = true;
    }

    private void OnInputPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back || e.Key == Key.Delete)
        {
            _suppressAnimation = false;
            UpdateResult();
        }
    }

    // ── 工具方法 ─────────────────────────────────────────────────
    // UIElement 和 Animatable 是两条继承链，各自有 BeginAnimation，所以需要两个重载
    private static void Animate(UIElement target, DependencyProperty prop,
        double from, double to, int ms, IEasingFunction? easing = null)
    {
        var anim = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(ms)));
        if (easing != null) anim.EasingFunction = easing;
        target.BeginAnimation(prop, anim);
    }

    private static void Animate(Animatable target, DependencyProperty prop,
        double from, double to, int ms, IEasingFunction? easing = null)
    {
        var anim = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(ms)));
        if (easing != null) anim.EasingFunction = easing;
        target.BeginAnimation(prop, anim);
    }

    private string FormatSuggestions(string word)
    {
        var closest = LevenshteinHelper.FindClosest(word, _wordlist.Words, 3);
        return closest.Count > 0
            ? $"→ {string.Join(", ", closest.Select(c => c.word))}"
            : $"— {_localization["suggestion.no_match"]}";
    }

    // ── 语言 ─────────────────────────────────────────────────────
    private void PopulateLanguages()
    {
        LanguageSelector.Items.Clear();
        var langs = _localization.AvailableLanguages()
            .OrderBy(l => l.Key).ToList();
        foreach (var (display, code) in langs)
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
                        WordListInfo.Text = string.Format(_localization["status.words"], count);
                        var path = _wordlist.SourcePath ?? "";
                        WordlistPathLink.Inlines.Clear();
                        WordlistPathLink.Inlines.Add(new System.Windows.Documents.Run(
                            string.IsNullOrEmpty(path) ? "—" : Path.GetFileName(path)));
                    });
                }
                else
                    Dispatcher.Invoke(() =>
                    {
                        WordListInfo.Text = string.Format(_localization["status.load_failed"], "未找到词库文件");
                        WordlistPathLink.Inlines.Clear();
                        WordlistPathLink.Inlines.Add(new System.Windows.Documents.Run("—"));
                    });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => WordListInfo.Text = $"⚠ {_localization["status.load_failed"]}: {ex.Message}");
            }
        });
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLanguageInit) return;
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
        WhitelistButton.Content = $"⊞ {_localization["whitelist.title"]}";
        SettingsButton.Content = $"⚙ {_localization["settings.title"]}";
        ReloadButton.ToolTip = _localization["menu.reload_wordlist"];
        InputLabel.Text = _localization["input.label"];
        ResultLabel.Text = _localization["result.label"];
        CopyButton.Content = $"📋 {_localization["menu.copy_result"]}";
        SuggestionLabel.Text = $"💡 {_localization["suggestion.title"]}";

        if (_wordlist.WordCount > 0)
        {
            WordListInfo.Text = string.Format(_localization["status.words"], _wordlist.WordCount);
            var path = _wordlist.SourcePath ?? "";
            WordlistPathLink.Inlines.Clear();
            WordlistPathLink.Inlines.Add(new System.Windows.Documents.Run(
                string.IsNullOrEmpty(path) ? "—" : Path.GetFileName(path)));
        }
        else
            WordListInfo.Text = _localization["status.ready"];

        UpdateResult();
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings, _checker, _wordlist, _localization);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            InputBox.FontSize = _settings.FontSize;
            InputBox.TextWrapping = _settings.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            ResultBox.FontSize = _settings.FontSize;
            UpdateResult();
        }
    }

    private void OnOpenAbout(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutWindow();
        dialog.Owner = this;
        dialog.ShowDialog();
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
            WordListInfo.Text = string.Format(_localization["status.words"], count);
            var path = _wordlist.SourcePath ?? "";
            WordlistPathLink.Inlines.Clear();
            WordlistPathLink.Inlines.Add(new System.Windows.Documents.Run(
                string.IsNullOrEmpty(path) ? "—" : Path.GetFileName(path)));
            UpdateResult();
        }
        catch (Exception ex)
        {
            WordListInfo.Text = $"⚠ {_localization["status.load_failed"]}: {ex.Message}";
        }
    }

    private void OnOpenWordlistLocation(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        var path = _wordlist.SourcePath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            var args = $"/select, \"{path}\"";
            System.Diagnostics.Process.Start("explorer.exe", args);
        }
    }

    private void OnCopyResult(object sender, RoutedEventArgs e)
    {
        var text = new TextRange(ResultBox.Document.ContentStart, ResultBox.Document.ContentEnd).Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                Clipboard.SetText(text.TrimEnd('\r', '\n'));
            }
            catch
            {
                // 剪贴板被占用时静默失败
            }

            // 复制反馈动画
            CopyButton.Opacity = 0.5;
            Animate(CopyButton, UIElement.OpacityProperty, 0.5, 1, 300, new QuadraticEase());
        }
    }

    private record LanguageItem(string Display, string Code)
    {
        public override string ToString() => Display;
    }
}
