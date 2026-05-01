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
    public bool IgnoreAngleBrackets { get; set; } = true;
    public string WordlistPath { get; set; } = "";
    public List<string> Whitelist { get; set; } = [];
    public string Language { get; set; } = "zh-CN";
    public string Theme { get; set; } = "Dark";

    public Settings(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "appsettings.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);
            if (data is null) return;

            IgnoreChinese = data.IgnoreChinese;
            IgnoreAngleBrackets = data.IgnoreAngleBrackets;
            WordlistPath = data.WordlistPath ?? "";
            Whitelist = data.Whitelist ?? [];
            Language = data.Language ?? "zh-CN";
            Theme = data.Theme ?? "Dark";
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
                IgnoreAngleBrackets = IgnoreAngleBrackets,
                WordlistPath = WordlistPath,
                Whitelist = Whitelist,
                Language = Language,
                Theme = Theme,
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
        public bool IgnoreAngleBrackets { get; init; } = true;
        public string? WordlistPath { get; init; }
        public List<string>? Whitelist { get; init; }
        public string? Language { get; init; }
        public string? Theme { get; init; }
    }
}
