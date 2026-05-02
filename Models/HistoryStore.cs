using System.Text.Json;

namespace CassieWordCheck.Models;

public class HistoryStore
{
    private readonly string _filePath;
    private readonly List<HistoryItem> _items = [];

    public IReadOnlyList<HistoryItem> Items => _items.AsReadOnly();
    public int Count => _items.Count;

    public HistoryStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            GetAppDir(), "data", "history.json");
        Load();
    }

    private static string GetAppDir() =>
        Path.GetDirectoryName(Environment.ProcessPath)
        ?? AppDomain.CurrentDomain.BaseDirectory;

    public void Add(string inputText, string resultText,
        int available, int unavailable, int ignored, double coverage)
    {
        // 简单去重：与最后一条相同则跳过（频率由调用方的 Timer 控制）
        if (_items.Count > 0 && _items[^1].InputText == inputText)
            return;

        _items.Add(new HistoryItem
        {
            InputText = inputText,
            ResultText = resultText,
            Available = available,
            Unavailable = unavailable,
            Ignored = ignored,
            Coverage = coverage,
            Timestamp = DateTime.Now,
        });

        if (_items.Count > 50)
            _items.RemoveAt(0);

        Save();
    }

    public void Clear()
    {
        _items.Clear();
        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<List<HistoryItem>>(json);
            if (data is null) return;
            _items.Clear();
            _items.AddRange(data);
        }
        catch { /* 静默 */ }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { /* 静默 */ }
    }
}

public class HistoryItem
{
    public string InputText { get; init; } = "";
    public string ResultText { get; init; } = "";
    public int Available { get; init; }
    public int Unavailable { get; init; }
    public int Ignored { get; init; }
    public double Coverage { get; init; }
    public DateTime Timestamp { get; init; }
}
