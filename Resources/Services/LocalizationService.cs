using System.Collections.Concurrent;
using System.Text.Json;

namespace CassieWordCheck.Services;

/// <summary>
/// 多语言管理——从 Resources/Locales/*.json 加载翻译喵~
/// 支持运行时切换语言，启用缓存避免重复读文件喵！
/// </summary>
public class LocalizationService
{
    // locale JSON 文件存放目录喵~
    private const string LocaleDir = "Resources/Locales";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true // key 不区分大小写喵~
    };

    // 多级缓存：{ languageCode → { key → value } } 喵~
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
    private string _currentLanguage = "zh-CN";

    public string CurrentLanguage => _currentLanguage;

    /// <summary>语言切换事件——UI 可以通过这个事件刷新文本喵~</summary>
    public event Action? LanguageChanged;

    /// <summary>切换到指定语言喵~</summary>
    public void SetLanguage(string langCode)
    {
        if (_currentLanguage == langCode) return;
        _currentLanguage = langCode;
        EnsureLoaded(langCode); // 确保已加载该语言的 JSON 喵~
        LanguageChanged?.Invoke();
    }

    /// <summary>获取所有可用语言列表（显示名 → 代码）喵~</summary>
    public IReadOnlyDictionary<string, string> AvailableLanguages()
    {
        var localePath = ResolveLocalePath();
        if (localePath is null)
            return new Dictionary<string, string> { ["简体中文"] = "zh-CN" };

        var result = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(localePath, "*.json"))
        {
            var langCode = Path.GetFileNameWithoutExtension(file);
            // 每个 JSON 里都有 "_zh-CN": "简体中文" 这样的自描述 key 喵~
            var name = Translate($"_{langCode}", langCode) ?? langCode;
            result[name] = langCode;
        }
        return result;
    }

    /// <summary>翻译指定 key（可指定语言，不传用当前语言）喵~</summary>
    public string? Translate(string key, string? langCode = null)
    {
        var lang = langCode ?? _currentLanguage;
        EnsureLoaded(lang);

        if (_cache.TryGetValue(lang, out var data) && data.TryGetValue(key, out var value))
            return value;

        return null; // 没找到就返回 null，调用方 fallback 到 key 本身喵~
    }

    /// <summary>索引器：翻译不到的 key 直接返回 key 本身喵~</summary>
    public string this[string key] => Translate(key) ?? key;

    // 多路径查找 locale 目录（支持开发模式和打包发布两种场景喵~）
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

    // 确保某种语言的 JSON 已加载到缓存喵~
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
            catch { /* 文件损坏就返回空字典喵~ */ }
        }

        _cache[langCode] = data;
    }
}
