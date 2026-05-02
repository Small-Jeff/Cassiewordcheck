using System.Text.Json;

namespace CassieWordCheck.Models;

public class Settings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public bool IgnoreChinese { get; set; } = true;
    public bool FilterFormatting { get; set; } = true;
    public bool FilterNaming { get; set; } = true;
    public string WordlistPath { get; set; } = "";
    public List<string> Whitelist { get; set; } = [];
    public string Language { get; set; } = "zh-CN";
    public string Theme { get; set; } = "Dark";
    public int FontSize { get; set; } = 14;
    public bool WordWrap { get; set; } = true;

    public Settings(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            GetAppDir(),
            "data", "appsettings.json");
        Load();
    }

    private static string GetAppDir() =>
        Path.GetDirectoryName(Environment.ProcessPath)
        ?? AppDomain.CurrentDomain.BaseDirectory;

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
            // Fall back to defaults
        }
    }

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
            // Silently fail
        }
    }

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
