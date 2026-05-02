using System.Collections.Concurrent;
using System.Text.Json;

namespace CassieWordCheck.Services;

public class LocalizationService
{
    private const string LocaleDir = "Resources/Locales";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
    private string _currentLanguage = "zh-CN";

    public string CurrentLanguage => _currentLanguage;

    public event Action? LanguageChanged;

    public void SetLanguage(string langCode)
    {
        if (_currentLanguage == langCode) return;
        _currentLanguage = langCode;
        EnsureLoaded(langCode);
        LanguageChanged?.Invoke();
    }

    public IReadOnlyDictionary<string, string> AvailableLanguages()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var localePath = Path.Combine(baseDir, LocaleDir);

        // Try from base directory first, then source
        if (!Directory.Exists(localePath))
        {
            localePath = Path.Combine(Directory.GetCurrentDirectory(), LocaleDir);
            if (!Directory.Exists(localePath))
            {
                // Try project root
                var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
                localePath = Path.Combine(projectRoot, LocaleDir);
            }
        }

        if (!Directory.Exists(localePath))
        {
            // Return default
            return new Dictionary<string, string> { ["简体中文"] = "zh-CN" };
        }

        var result = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(localePath, "*.json"))
        {
            var langCode = Path.GetFileNameWithoutExtension(file);
            var name = Translate($"_{langCode}", langCode) ?? langCode;
            result[name] = langCode;
        }
        return result;
    }

    public string? Translate(string key, string? langCode = null)
    {
        var lang = langCode ?? _currentLanguage;
        EnsureLoaded(lang);

        if (_cache.TryGetValue(lang, out var data) && data.TryGetValue(key, out var value))
            return value;

        return null;
    }

    public string this[string key] => Translate(key) ?? key;

    private void EnsureLoaded(string langCode)
    {
        if (_cache.ContainsKey(langCode)) return;

        var data = new Dictionary<string, string>();
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(baseDir, LocaleDir, $"{langCode}.json"),
            Path.Combine(Directory.GetCurrentDirectory(), LocaleDir, $"{langCode}.json"),
            Path.Combine(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")), LocaleDir, $"{langCode}.json"),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    data = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? data;
                    break;
                }
                catch { }
            }
        }

        _cache[langCode] = data;
    }
}
