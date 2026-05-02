using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class MainWindow : Window
{
    private readonly WordList _wordlist;
    private readonly Checker _checker;
    private readonly Settings _settings;
    private readonly LocalizationService _localization;
    private readonly Dictionary<string, string> _suggestionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private readonly HistoryStore _historyStore = new();
    private readonly DispatcherTimer _historyTimer = new();
    private string _suggestionLabelOriginal = "";
    private bool _suppressAnimation;
    private DateTime _lastBounceTime = DateTime.MinValue;
    // 历史快照缓存（每隔 3 分钟由 Timer 写入磁盘）
    private string _snapshotText = "";
    private string _snapshotResult = "";
    private int _snapshotAvailable, _snapshotUnavailable, _snapshotIgnored;
    private double _snapshotCoverage;

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

        _suggestionLabelOriginal = SuggestionLabel.Text;
        LoadWordListAsync();
        UpdateUILanguage();

        // 每 3 分钟自动保存历史快照
        _historyTimer.Interval = TimeSpan.FromMinutes(3);
        _historyTimer.Tick += OnHistoryTimerTick;
        _historyTimer.Start();
    }

    // ── 入场动画（错峰播放，减少同时并发）─────────────────────────
    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        LoadToolbarIcon();

        // Wave 1 (0ms): 工具栏从顶部滑入 —— 只有 2 个动画
        ToolbarCard.Opacity = 0;
        var toolbarT = new TranslateTransform(0, -15);
        ToolbarCard.RenderTransform = toolbarT;
        ToolbarCard.RenderTransformOrigin = new Point(0.5, 0.5);
        Animate(ToolbarCard, UIElement.OpacityProperty, 0, 1, 300, new QuadraticEase());
        Animate(toolbarT, TranslateTransform.YProperty, -15, 0, 350, new QuadraticEase());

        // Wave 2 (80ms): 主内容区上移淡入 —— 2 个动画
        var cards = MainContentGrid;
        cards.Opacity = 0;
        var ct = (TranslateTransform)cards.RenderTransform;
        ct.Y = 25;
        Animate(cards, UIElement.OpacityProperty, 0, 1, 320, new QuadraticEase(), 80);
        Animate(ct, TranslateTransform.YProperty, 25, 0, 400, new QuadraticEase(), 80);

        // Wave 3 (160ms): 两卡片从中心弹开 —— 6 个动画
        var inputScale = (ScaleTransform)InputCard.RenderTransform;
        var resultGroup = (TransformGroup)ResultCard.RenderTransform;
        var resultScale = (ScaleTransform)resultGroup.Children[1];

        inputScale.ScaleX = 0.35;
        inputScale.ScaleY = 0.35;
        InputCard.Opacity = 0;
        resultScale.ScaleX = 0.35;
        resultScale.ScaleY = 0.35;
        ResultCard.Opacity = 0;

        var cardEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.2 };
        const int cardD = 420;

        Animate(inputScale, ScaleTransform.ScaleXProperty, 0.35, 1, cardD, cardEase, 160);
        Animate(inputScale, ScaleTransform.ScaleYProperty, 0.35, 1, cardD, cardEase, 160);
        Animate(InputCard, UIElement.OpacityProperty, 0, 1, cardD - 100, new QuadraticEase(), 160);
        Animate(resultScale, ScaleTransform.ScaleXProperty, 0.35, 1, cardD, cardEase, 160);
        Animate(resultScale, ScaleTransform.ScaleYProperty, 0.35, 1, cardD, cardEase, 160);
        Animate(ResultCard, UIElement.OpacityProperty, 0, 1, cardD - 100, new QuadraticEase(), 160);

        // Wave 4 (360ms): 底部统计栏 + 文件按钮淡入 —— 2 个动画
        StatsBar.Opacity = 0;
        Animate(StatsBar, UIElement.OpacityProperty, 0, 1, 400, new QuadraticEase(), 360);

        FileOpenButton.Opacity = 0;
        var fileAnim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(280)));
        fileAnim.EasingFunction = new QuadraticEase();
        fileAnim.BeginTime = TimeSpan.FromMilliseconds(360);
        FileOpenButton.BeginAnimation(UIElement.OpacityProperty, fileAnim);
    }

    // ── 从 data/ 加载工具栏图标 ─────────────────────────────────
    private void LoadToolbarIcon()
    {
        try
        {
            var paths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "AAA.JPG"),
                Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? ".", "data", "AAA.JPG"),
            };
            var imgPath = paths.FirstOrDefault(File.Exists);
            if (imgPath is not null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imgPath);
                bitmap.EndInit();
                ToolbarIcon.Source = bitmap;
            }
        }
        catch { /* 静默 */ }
    }

    // ── 输入变化 ─────────────────────────────────────────────────
    private void OnInputTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_suppressAnimation)
            UpdateResult();
    }

    /// <summary>保存当前输入到撤销栈，用于粘贴/清空/加载文件等操作</summary>
    private void PushUndo()
    {
        var text = InputBox.Text;
        if (_undoStack.Count == 0 || _undoStack.Peek() != text)
            _undoStack.Push(text);
        if (_undoStack.Count > 50)
        {
            var items = _undoStack.ToArray();
            Array.Reverse(items);
            _undoStack.Clear();
            for (int i = items.Length - 50; i < items.Length; i++)
                _undoStack.Push(items[i]);
        }
        _redoStack.Clear();
    }

    private void UpdateResult()
    {
        var text = InputBox.Text;
        CharCountLabel.Text = string.Format(_localization["stats.chars"], text.Length);

        var results = _checker.CheckText(text);
        var stats = _checker.GetStatistics(results, text);

        ResultBox.Document = DocumentBuilder.BuildResultDocument(results, ResultBox.ActualWidth, _settings.FontSize);

        var available = (int)stats["available"];
        var unavailable = (int)stats["unavailable"];
        var ignored = (int)stats["ignored"];
        var coverage = (double)stats["coverage"];

        AvailableLabel.Text = string.Format(_localization["stats.available"], available);
        UnavailableLabel.Text = string.Format(_localization["stats.unavailable"], unavailable);
        IgnoredLabel.Text = string.Format(_localization["stats.ignored"], ignored);
        CoverageLabel.Text = $"{coverage:F1}%";

        // 缓存当前快照（Timer 每隔 3 分钟自动写入磁盘）
        _snapshotText = text;
        _snapshotResult = new TextRange(ResultBox.Document.ContentStart, ResultBox.Document.ContentEnd).Text;
        _snapshotAvailable = available;
        _snapshotUnavailable = unavailable;
        _snapshotIgnored = ignored;
        _snapshotCoverage = coverage;

        // 进度条过渡
        var scale = coverage / 100.0;
        Animate(CoverageBar.RenderTransform, ScaleTransform.ScaleXProperty,
            ((ScaleTransform)CoverageBar.RenderTransform).ScaleX, scale, 400, new QuadraticEase());

        // 输入实时同步 → 结果卡片"键入"微动（防抖：300ms 内不重复触发）
        var now = DateTime.UtcNow;
        if ((now - _lastBounceTime).TotalMilliseconds > 300)
        {
            _lastBounceTime = now;

            var resultGroup = (TransformGroup)ResultCard.RenderTransform;
            var resultScale = (ScaleTransform)resultGroup.Children[1];
            ResultCard.RenderTransformOrigin = new Point(0.5, 0.5);

            var typingBounce = new DoubleAnimation(1, 1.015, new Duration(TimeSpan.FromMilliseconds(80)))
            { AutoReverse = true, EasingFunction = new QuadraticEase() };
            resultScale.BeginAnimation(ScaleTransform.ScaleXProperty, typingBounce);
            resultScale.BeginAnimation(ScaleTransform.ScaleYProperty, (DoubleAnimation)typingBounce.Clone());
        }

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

        PushUndo();
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

    // ── 退格 & 撤销/重做 ──────────────────────────────────────────
    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back || e.Key == Key.Delete)
            _suppressAnimation = true;

        // Ctrl+V 粘贴 → 保存撤销点
        if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            PushUndo();

        // Ctrl+Z 撤销
        if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(InputBox.Text);
                InputBox.Text = _undoStack.Pop();
                InputBox.CaretIndex = InputBox.Text.Length;
            }
            e.Handled = true;
        }

        // Ctrl+Y 重做
        if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(InputBox.Text);
                InputBox.Text = _redoStack.Pop();
                InputBox.CaretIndex = InputBox.Text.Length;
            }
            e.Handled = true;
        }
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
        double from, double to, int ms, IEasingFunction? easing = null, int delayMs = 0)
    {
        var anim = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(ms)));
        if (easing != null) anim.EasingFunction = easing;
        if (delayMs > 0) anim.BeginTime = TimeSpan.FromMilliseconds(delayMs);
        target.BeginAnimation(prop, anim);
    }

    private static void Animate(Animatable target, DependencyProperty prop,
        double from, double to, int ms, IEasingFunction? easing = null, int delayMs = 0)
    {
        var anim = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(ms)));
        if (easing != null) anim.EasingFunction = easing;
        if (delayMs > 0) anim.BeginTime = TimeSpan.FromMilliseconds(delayMs);
        target.BeginAnimation(prop, anim);
    }

    private string FormatSuggestions(string word)
    {
        if (_suggestionCache.TryGetValue(word, out var cached))
            return cached;

        var closest = LevenshteinHelper.FindClosest(word, _wordlist.Words, 3);
        var result = closest.Count > 0
            ? $"→ {string.Join(", ", closest.Select(c => c.word))}"
            : $"— {_localization["suggestion.no_match"]}";
        _suggestionCache[word] = result;
        return result;
    }

    // ── 文件操作：打开文本 / 导入单词 ────────────────────────────
    private async void OnFileOpenOrImport(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "打开文件 / 导入单词",
            Filter = "所有支持的文件 (*.txt;*.csv;*.xlsx)|*.txt;*.csv;*.xlsx|" +
                     "文本文件 (*.txt)|*.txt|CSV 文件 (*.csv)|*.csv|Excel 文件 (*.xlsx)|*.xlsx",
            Multiselect = true,
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() != true) return;

        var textFiles = dialog.FileNames.Where(f =>
            f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)).ToList();
        var importFiles = dialog.FileNames.Where(f =>
            !f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)).ToList();

        // 导入非 .txt 文件（CSV/Excel → 单词）
        if (importFiles.Count > 0)
        {
            var totalAdded = 0;
            foreach (var file in importFiles)
            {
                try
                {
                    var added = await Task.Run(() => _wordlist.AddFromFile(file));
                    if (added > 0) totalAdded += added;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败：{Path.GetFileName(file)}\n{ex.Message}",
                        "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            if (totalAdded > 0)
            {
                _suggestionCache.Clear();
                WordListInfo.Text = string.Format(_localization["status.words"], _wordlist.WordCount);
                UpdateResult();
            }
        }

        // .txt 文件 → 加载到输入框
        if (textFiles.Count > 0)
        {
            PushUndo();
            if (textFiles.Count == 1)
            {
                InputBox.Text = File.ReadAllText(textFiles[0]);
            }
            else
            {
                var parts = textFiles.Select(f =>
                    $"===== {Path.GetFileName(f)} =====\n{File.ReadAllText(f)}");
                InputBox.Text = string.Join("\n\n", parts);
            }
        }
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

    private void UpdateUILanguage()
    {
        var settingsLabel = _localization["settings.title"];
        if (_localization.CurrentLanguage != "en-US")
            settingsLabel += " (settings)";
        WhitelistButton.Content = $"⊞ {_localization["whitelist.title"]}";
        SettingsButton.Content = $"⚙ {settingsLabel}";
        ReloadButton.ToolTip = _localization["menu.reload_wordlist"];
        StatsButton.ToolTip = _localization["menu.statistics"];
        AboutButton.ToolTip = _localization["menu.about"];
        FileOpenButton.ToolTip = "打开文件 / 导入单词";
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

    // ── 打开设置 ──────────────────────────────────────────────────
    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings, _checker, _wordlist, _localization);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            InputBox.FontSize = _settings.FontSize;
            InputBox.TextWrapping = _settings.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            ResultBox.FontSize = _settings.FontSize;
            UpdateUILanguage();
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
        dialog.ShowDialog();

        // 窗口关闭时无条件保存白名单（WhitelistWindow 对 _wordlist 的修改是实时的）
        _settings.Whitelist = _wordlist.Whitelist.ToList();
        _settings.Save();
        UpdateResult();
    }

    private void OnReloadWordlist(object sender, RoutedEventArgs e)
    {
        try
        {
            var count = _wordlist.Reload();
            _suggestionCache.Clear();
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

    // ── 统计面板 ──────────────────────────────────────────────────
    private void OnOpenStatistics(object sender, RoutedEventArgs e)
    {
        var dialog = new StatisticsWindow(_historyStore, _localization)
        {
            Owner = this,
        };
        dialog.ShowDialog();
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

    // ── 导出结果 ──────────────────────────────────────────────────
    private void OnExportResult(object sender, RoutedEventArgs e)
    {
        var text = new TextRange(ResultBox.Document.ContentStart, ResultBox.Document.ContentEnd).Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "导出检查结果",
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = $"CWC_result_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, text.TrimEnd('\r', '\n'));
            }
            catch { }
        }
    }

    // ── 检查历史 ──────────────────────────────────────────────────
    private void OnToggleHistory(object sender, RoutedEventArgs e)
    {
        // 恢复建议面板标题（修复 BUG：标题被历史覆盖后不恢复）
        SuggestionLabel.Text = _suggestionLabelOriginal;

        var dialog = new HistoryWindow(_historyStore, text =>
        {
            PushUndo();
            Dispatcher.Invoke(() => InputBox.Text = text);
        })
        {
            Owner = this,
        };
        dialog.ShowDialog();
    }

    /// <summary>每隔 3 分钟自动将当前快照写入历史记录</summary>
    private void OnHistoryTimerTick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_snapshotText)) return;
        _historyStore.Add(_snapshotText, _snapshotResult,
            _snapshotAvailable, _snapshotUnavailable, _snapshotIgnored, _snapshotCoverage);
    }

}
