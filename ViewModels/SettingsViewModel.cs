using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly WordList _wordlist;
    private readonly Checker _checker;
    private readonly Settings _settings;
    private readonly LocalizationService _localization;

    [ObservableProperty]
    private bool _ignoreChinese;

    [ObservableProperty]
    private bool _ignoreAngleBrackets;

    [ObservableProperty]
    private string _wordlistPath = "";

    [ObservableProperty]
    private string _selectedTheme = "";

    [ObservableProperty]
    private string _selectedLanguage = "";

    [ObservableProperty]
    private string _wordlistPathLabel = "";

    public List<ThemeOption> Themes { get; } =
    [
        new("label.dark", "Dark"),
        new("label.light", "Light"),
        new("label.system", "System"),
    ];

    public List<LanguageOption> Languages { get; } = [];

    public SettingsViewModel(WordList wordlist, Checker checker, Settings settings, LocalizationService localization)
    {
        _wordlist = wordlist;
        _checker = checker;
        _settings = settings;
        _localization = localization;

        // Load current values
        IgnoreChinese = _checker.IgnoreChinese;
        IgnoreAngleBrackets = _checker.IgnoreAngleBrackets;
        WordlistPath = _settings.WordlistPath;
        WordlistPathLabel = WordlistPath;

        var theme = _settings.Theme;
        SelectedTheme = theme switch
        {
            "Dark" => _localization["label.dark"],
            "Light" => _localization["label.light"],
            "System" => _localization["label.system"],
            _ => _localization["label.dark"],
        };

        // Load available languages
        foreach (var (display, code) in _localization.AvailableLanguages())
        {
            Languages.Add(new LanguageOption(display, code));
        }

        SelectedLanguage = Languages
            .FirstOrDefault(l => l.Code == _localization.CurrentLanguage)
            ?.Display ?? "简体中文";
    }

    [RelayCommand]
    private void BrowseWordlist()
    {
        // Handled in view code-behind
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        IgnoreChinese = true;
        IgnoreAngleBrackets = true;
        WordlistPath = "";
        SelectedTheme = _localization["label.dark"];
    }

    public void Apply()
    {
        _checker.IgnoreChinese = IgnoreChinese;
        _checker.IgnoreAngleBrackets = IgnoreAngleBrackets;

        _settings.IgnoreChinese = IgnoreChinese;
        _settings.IgnoreAngleBrackets = IgnoreAngleBrackets;

        if (!string.IsNullOrWhiteSpace(WordlistPath) && WordlistPath != _settings.WordlistPath)
        {
            _settings.WordlistPath = WordlistPath;
            try
            {
                _wordlist.LoadFromFile(WordlistPath);
            }
            catch { }
        }

        var themeMap = new Dictionary<string, string>
        {
            [_localization["label.dark"]] = "Dark",
            [_localization["label.light"]] = "Light",
            [_localization["label.system"]] = "System",
        };
        _settings.Theme = themeMap.GetValueOrDefault(SelectedTheme, "Dark");

        var langMap = Languages.ToDictionary(l => l.Display, l => l.Code);
        var langCode = langMap.GetValueOrDefault(SelectedLanguage, "zh-CN");
        _settings.Language = langCode;
        _localization.SetLanguage(langCode);

        _settings.Save();
    }
}

public record ThemeOption(string DisplayKey, string Value);

public record LanguageOption(string Display, string Code);
