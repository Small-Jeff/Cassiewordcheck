using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly WordList _wordlist;
    private readonly Checker _checker;
    private readonly Settings _settings;
    private readonly LocalizationService _localization;

    [ObservableProperty]
    private string _inputText = "";

    [ObservableProperty]
    private string _resultHtml = "";

    [ObservableProperty]
    private int _availableCount;

    [ObservableProperty]
    private int _unavailableCount;

    [ObservableProperty]
    private int _ignoredCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private double _coverage;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _wordlistInfo = "";

    [ObservableProperty]
    private bool _isReady;

    [ObservableProperty]
    private bool _hasUnavailableWords;

    [ObservableProperty]
    private bool _isWordlistLoaded;

    public ObservableCollection<SuggestionItem> Suggestions { get; } = [];

    public MainViewModel(WordList wordlist, Checker checker, Settings settings, LocalizationService localization)
    {
        _wordlist = wordlist;
        _checker = checker;
        _settings = settings;
        _localization = localization;

        _localization.LanguageChanged += OnLanguageChanged;
    }

    partial void OnInputTextChanged(string value)
    {
        UpdateResults();
    }

    private void UpdateResults()
    {
        var results = _checker.CheckText(InputText);
        var stats = _checker.GetStatistics(InputText);

        AvailableCount = (int)stats["available"];
        UnavailableCount = (int)stats["unavailable"];
        IgnoredCount = (int)stats["ignored"];
        TotalCount = (int)stats["total"];
        Coverage = (double)stats["coverage"];
        HasUnavailableWords = UnavailableCount > 0;

        // Build result text with color markers for the view
        var sb = new StringBuilder();
        foreach (var r in results)
        {
            switch (r.Status)
            {
                case CheckStatus.Available:
                    sb.Append($"<span class=\"available\">{EscapeXml(r.Text)}</span>");
                    break;
                case CheckStatus.Unavailable:
                    sb.Append($"<span class=\"unavailable\">{EscapeXml(r.Text)}</span>");
                    break;
                case CheckStatus.Ignored:
                    sb.Append($"<span class=\"ignored\">{EscapeXml(r.Text)}</span>");
                    break;
                case CheckStatus.Separator:
                    sb.Append(EscapeXml(r.Text));
                    break;
            }
        }
        ResultHtml = sb.ToString();

        UpdateSuggestions(results);
    }

    private void UpdateSuggestions(List<CheckResult> results)
    {
        Suggestions.Clear();
        var unavailableWords = results
            .Where(r => r.Status == CheckStatus.Unavailable)
            .Select(r => r.Text)
            .Distinct();

        foreach (var word in unavailableWords)
        {
            var closest = LevenshteinHelper.FindClosest(word, _wordlist.Words, 3);
            if (closest.Count > 0)
            {
                Suggestions.Add(new SuggestionItem
                {
                    Original = word,
                    Suggestions = closest.Select(c => c.word).ToList()
                });
            }
        }
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    [RelayCommand]
    private void CopyResult()
    {
        var text = ExtractPlainText(ResultHtml);
        Clipboard.SetText(text);
    }

    [RelayCommand]
    private void CopyBroadcast()
    {
        Clipboard.SetText(InputText);
    }

    [RelayCommand]
    private void ReloadWordlist()
    {
        try
        {
            var count = _wordlist.Reload();
            IsWordlistLoaded = true;
            StatusText = _localization["status.loaded"]
                .Replace("{0}", count.ToString())
                .Replace("{1}", Path.GetFileName(_wordlist.SourcePath));
            WordlistInfo = _localization["status.words"]
                .Replace("{0}", count.ToString());
            UpdateResults();
        }
        catch (Exception ex)
        {
            StatusText = _localization["status.load_failed"]
                .Replace("{0}", ex.Message);
        }
    }

    private static string ExtractPlainText(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
    }

    private void OnLanguageChanged()
    {
        StatusText = _localization["status.ready"];
        if (IsWordlistLoaded)
        {
            WordlistInfo = _localization["status.words"]
                .Replace("{0}", _wordlist.WordCount.ToString());
        }
        UpdateResults();
    }

    public void LoadWordList(string? path = null)
    {
        try
        {
            var target = path;
            if (string.IsNullOrEmpty(target) || !File.Exists(target))
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var defaultPath = Path.Combine(baseDir, "data", "cassie-text.txt");
                if (!File.Exists(defaultPath))
                {
                    defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "cassie-text.txt");
                    if (!File.Exists(defaultPath))
                    {
                        var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
                        defaultPath = Path.Combine(projectRoot, "data", "cassie-text.txt");
                    }
                }
                target = defaultPath;
            }

            if (File.Exists(target))
            {
                var count = _wordlist.LoadFromFile(target);
                IsWordlistLoaded = true;
                StatusText = _localization["status.loaded"]
                    .Replace("{0}", count.ToString())
                    .Replace("{1}", Path.GetFileName(target));
                WordlistInfo = _localization["status.words"]
                    .Replace("{0}", count.ToString());
            }
            else
            {
                StatusText = _localization["status.load_failed"]
                    .Replace("{0}", "No word list found");
            }
        }
        catch (Exception ex)
        {
            StatusText = _localization["status.load_failed"]
                .Replace("{0}", ex.Message);
        }

        IsReady = true;
    }
}

public class SuggestionItem
{
    public string Original { get; set; } = "";
    public List<string> Suggestions { get; set; } = [];
    public string SuggestionText => Suggestions.Count > 0
        ? string.Join(", ", Suggestions)
        : "—";
}
