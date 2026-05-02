using System.Windows;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class SettingsWindow : Window
{
    private readonly Settings _settings;
    private readonly Checker _checker;
    private readonly WordList _wordlist;
    private readonly LocalizationService _localization;
    private readonly UpdateService _updateService = new();
    private bool _loaded;

    public SettingsWindow(Settings settings, Checker checker, WordList wordlist, LocalizationService localization)
    {
        InitializeComponent();
        _settings = settings;
        _checker = checker;
        _wordlist = wordlist;
        _localization = localization;
        this.EnableDarkTitleBar();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;

        PopulateLanguages();

        IgnoreChineseCheck.IsChecked = _checker.IgnoreChinese;
        FilterFormattingCheck.IsChecked = _checker.FilterFormatting;
        FilterNamingCheck.IsChecked = _checker.FilterNaming;
        WordlistPathBox.Text = string.IsNullOrEmpty(_settings.WordlistPath)
            ? "默认词库（内嵌）"
            : _settings.WordlistPath;

        var fontSizeStr = _settings.FontSize.ToString();
        foreach (ComboBoxItem item in FontSizeCombo.Items)
        {
            if (item.Content.ToString() == fontSizeStr)
            {
                FontSizeCombo.SelectedItem = item;
                break;
            }
        }

        WordWrapCheck.IsChecked = _settings.WordWrap;
        UpdateUILanguage();
    }

    private void PopulateLanguages()
    {
        LanguageCombo.Items.Clear();
        var langs = _localization.AvailableLanguages()
            .OrderBy(l => l.Key).ToList();
        foreach (var (display, code) in langs)
        {
            LanguageCombo.Items.Add(new LanguageItem(display, code));
            if (code == _localization.CurrentLanguage)
                LanguageCombo.SelectedIndex = LanguageCombo.Items.Count - 1;
        }
        if (LanguageCombo.Items.Count > 0 && LanguageCombo.SelectedIndex < 0)
            LanguageCombo.SelectedIndex = 0;
    }

    private void UpdateUILanguage()
    {
        TitleLabel.Text = _localization["settings.title"];
        Title = _localization["settings.title"];
        UpdateCheckButton.Content = "↻ " + _localization["update.check"];
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded) return;
        if (LanguageCombo.SelectedItem is LanguageItem item)
        {
            _localization.SetLanguage(item.Code);
            _settings.Language = item.Code;
            UpdateUILanguage();
        }
    }

    private void OnBrowseWordlist(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择词库文件",
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() == true)
        {
            WordlistPathBox.Text = dialog.FileName;
        }
    }

    private async void OnCheckUpdate(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        btn.Content = "⏳ " + _localization["update.check"] + "...";

        try
        {
            var info = await Task.Run(() => _updateService.CheckForUpdateAsync());

            if (info is null)
            {
                MessageBox.Show(_localization["update.error"], "CASSIE CWC",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (info.HasUpdate)
            {
                var msg = string.Format(_localization["update.new_version"],
                    info.LatestVersion, _updateService.CurrentVersion);
                var result = MessageBox.Show(
                    $"{msg}\n\n{_localization["update.available"]}",
                    "CASSIE CWC", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = info.HtmlUrl,
                            UseShellExecute = true,
                        });
                }
            }
            else
            {
                MessageBox.Show(
                    $"{_localization["update.current"]}（v{_updateService.CurrentVersion}）",
                    "CASSIE CWC", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch
        {
            MessageBox.Show(_localization["update.error"], "CASSIE CWC",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            btn.Content = "↻ " + _localization["update.check"];
            btn.IsEnabled = true;
        }
    }

    private void OnResetDefaults(object sender, RoutedEventArgs e)
    {
        IgnoreChineseCheck.IsChecked = true;
        FilterFormattingCheck.IsChecked = true;
        FilterNamingCheck.IsChecked = true;
        WordlistPathBox.Text = "默认词库（内嵌）";
        foreach (ComboBoxItem item in FontSizeCombo.Items)
        {
            if (item.Content.ToString() == "14")
            {
                FontSizeCombo.SelectedItem = item;
                break;
            }
        }
        WordWrapCheck.IsChecked = true;

        foreach (var item in LanguageCombo.Items)
        {
            if (item is LanguageItem li && li.Code == "zh-CN")
            {
                LanguageCombo.SelectedItem = item;
                break;
            }
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _checker.IgnoreChinese = IgnoreChineseCheck.IsChecked ?? true;
        _checker.FilterFormatting = FilterFormattingCheck.IsChecked ?? true;
        _checker.FilterNaming = FilterNamingCheck.IsChecked ?? true;

        _settings.IgnoreChinese = _checker.IgnoreChinese;
        _settings.FilterFormatting = _checker.FilterFormatting;
        _settings.FilterNaming = _checker.FilterNaming;
        _settings.FontSize = FontSizeCombo.SelectedItem is ComboBoxItem fi
            && int.TryParse(fi.Content?.ToString(), out var fs) ? fs : 14;
        _settings.WordWrap = WordWrapCheck.IsChecked ?? true;

        var newPath = WordlistPathBox.Text.Trim();
        if (newPath.Length > 0 && newPath != "默认词库（内嵌）" &&
            newPath != _settings.WordlistPath && File.Exists(newPath))
        {
            _settings.WordlistPath = newPath;
            try
            {
                _wordlist.LoadFromFile(newPath);
            }
            catch { }
        }

        _settings.Save();
        DialogResult = true;
        Close();
    }
}

internal record LanguageItem(string Display, string Code)
{
    public override string ToString() => Display;
}
