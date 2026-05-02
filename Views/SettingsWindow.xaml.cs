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

    public SettingsWindow(Settings settings, Checker checker, WordList wordlist, LocalizationService localization)
    {
        InitializeComponent();
        _settings = settings;
        _checker = checker;
        _wordlist = wordlist;
        _localization = localization;

        this.EnableDarkTitleBar();

        IgnoreChineseCheck.IsChecked = _checker.IgnoreChinese;
        FilterFormattingCheck.IsChecked = _checker.FilterFormatting;
        FilterNamingCheck.IsChecked = _checker.FilterNaming;
        WordlistPathBox.Text = string.IsNullOrEmpty(_settings.WordlistPath)
            ? "默认词库（内嵌）"
            : _settings.WordlistPath;

        // 设置字体大小 ComboBox 选中项
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

    private void OnResetDefaults(object sender, RoutedEventArgs e)
    {
        IgnoreChineseCheck.IsChecked = true;
        FilterFormattingCheck.IsChecked = true;
        FilterNamingCheck.IsChecked = true;
        WordlistPathBox.Text = "默认词库（内嵌）";
        // 默认字体 14
        foreach (ComboBoxItem item in FontSizeCombo.Items)
        {
            if (item.Content.ToString() == "14")
            {
                FontSizeCombo.SelectedItem = item;
                break;
            }
        }
        WordWrapCheck.IsChecked = true;
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
