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
        var localePath = ResolveLocalePath();
        if (localePath is null)
            return new Dictionary<string, string> { ["简体中文"] = "zh-CN" };

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

    /// <summary>尝试多个路径查找 locale 目录，返回第一个存在的</summary>
    private static string? ResolveLocalePath()
    {
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LocaleDir),
            Path.Combine(Directory.GetCurrentDirectory(), LocaleDir),
            Path.Combine(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")), LocaleDir),
        };
        return candidates.FirstOrDefault(Directory.Exists);
    }

    private void EnsureLoaded(string langCode)
    {
        if (_cache.ContainsKey(langCode)) return;

        var data = new Dictionary<string, string>();
        var localeDir = ResolveLocalePath();
        if (localeDir is not null)
        {
            var path = Path.Combine(localeDir, $"{langCode}.json");
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    data = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? data;
                }
            }
            catch { }
        }

        _cache[langCode] = data;
    }
}
