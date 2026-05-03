using System.Text.Json;

namespace CassieWordCheck.Models;

/// <summary>
/// 应用设置——自动读写 data/appsettings.json，保存用户的偏好喵~
/// </summary>
public class Settings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true // 格式化 JSON，方便手动查看喵~
    };

    private readonly string _filePath;

    // ===== 用户可配置项 =====
    public bool IgnoreChinese { get; set; } = true;
    public bool FilterFormatting { get; set; } = true;
    public bool FilterNaming { get; set; } = true;
    public string WordlistPath { get; set; } = "";
    public List<string> Whitelist { get; set; } = [];
    public string Language { get; set; } = "zh-CN";
    public string Theme { get; set; } = "Dark";
    public int FontSize { get; set; } = 14;
    public bool WordWrap { get; set; } = true;

    /// <param name="filePath">可自定义路径，不传则用默认喵~</param>
    public Settings(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            GetAppDir(),
            "data", "appsettings.json");
        Load(); // 构造时自动加载已保存的配置喵！
    }

    // 获取 exe 真实目录喵~
    private static string GetAppDir() =>
        Path.GetDirectoryName(Environment.ProcessPath)
        ?? AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>从 JSON 文件加载配置喵~</summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);
            if (data is null) return;

            IgnoreChinese = data.IgnoreChinese;
            FilterFormatting = data.FilterFormatting;
            FilterNaming = data.FilterNaming;
            WordlistPath = data.WordlistPath ?? "";
            Whitelist = data.Whitelist ?? [];
            Language = data.Language ?? "zh-CN";
            Theme = data.Theme ?? "Dark";
            FontSize = data.FontSize > 0 ? data.FontSize : 14;
            WordWrap = data.WordWrap;
        }
        catch
        {
            // 文件损坏就回到默认配置喵~
        }
    }

    /// <summary>保存当前配置到 JSON 文件喵~</summary>
    public void Save()
    {
        try
        {
            var data = new SettingsData
            {
                IgnoreChinese = IgnoreChinese,
                FilterFormatting = FilterFormatting,
                FilterNaming = FilterNaming,
                WordlistPath = WordlistPath,
                Whitelist = Whitelist,
                Language = Language,
                Theme = Theme,
                FontSize = FontSize,
                WordWrap = WordWrap,
            };
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // 写入失败就静默喵~
        }
    }

    /// <summary>
    /// 内部使用的序列化 DTO——和 Settings 属性一一对应喵~
    /// </summary>
    private record SettingsData
    {
        public bool IgnoreChinese { get; init; } = true;
        public bool FilterFormatting { get; init; } = true;
        public bool FilterNaming { get; init; } = true;
        public string? WordlistPath { get; init; }
        public List<string>? Whitelist { get; init; }
        public string? Language { get; init; }
        public string? Theme { get; init; }
        public int FontSize { get; init; } = 14;
        public bool WordWrap { get; init; } = true;
    }
}
// 喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵喵