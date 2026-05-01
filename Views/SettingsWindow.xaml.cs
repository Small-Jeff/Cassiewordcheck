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
        IgnoreAngleCheck.IsChecked = _checker.IgnoreAngleBrackets;
        WordlistPathBox.Text = string.IsNullOrEmpty(_settings.WordlistPath)
            ? "默认词库（内嵌）"
            : _settings.WordlistPath;

        var themeIndex = _settings.Theme switch
        {
            "Dark" => 0,
            "Light" => 1,
            "System" => 2,
            _ => 0,
        };
        ThemeSelector.SelectedIndex = themeIndex;
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

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeSelector.SelectedItem is ComboBoxItem item)
        {
            var theme = item.Content.ToString() switch
            {
                "深色" => "Dark",
                "浅色" => "Light",
                "系统" => "System",
                _ => "Dark",
            };
            _settings.Theme = theme;
            _settings.Save();
        }
    }

    private void OnResetDefaults(object sender, RoutedEventArgs e)
    {
        IgnoreChineseCheck.IsChecked = true;
        IgnoreAngleCheck.IsChecked = true;
        WordlistPathBox.Text = "默认词库（内嵌）";
        ThemeSelector.SelectedIndex = 0;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _checker.IgnoreChinese = IgnoreChineseCheck.IsChecked ?? true;
        _checker.IgnoreAngleBrackets = IgnoreAngleCheck.IsChecked ?? true;

        _settings.IgnoreChinese = _checker.IgnoreChinese;
        _settings.IgnoreAngleBrackets = _checker.IgnoreAngleBrackets;

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
