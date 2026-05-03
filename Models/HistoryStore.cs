using System.Text.Json;

namespace CassieWordCheck.Models;

/// <summary>
/// 检查历史记录管理器——自动保存/加载上次检查的内容喵~
/// 每 3 分钟由 DispatcherTimer 触发快照写入，最多保留 50 条喵
/// </summary>
public class HistoryStore
{
    // 历史记录存哪儿：exe 同目录的 data/history.json 喵~
    private readonly string _filePath;
    private readonly List<HistoryItem> _items = [];

    /// <summary>只读历史条目列表喵~</summary>
    public IReadOnlyList<HistoryItem> Items => _items.AsReadOnly();
    public int Count => _items.Count;

    /// <param name="filePath">可自定义路径，不传则用默认喵~</param>
    public HistoryStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            GetAppDir(), "data", "history.json");
        Load(); // 启动时加载已保存的历史喵~
    }

    // 获取 exe 真实目录（单文件发布时不会指向临时解压目录喵）
    private static string GetAppDir() =>
        Path.GetDirectoryName(Environment.ProcessPath)
        ?? AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>添加一条检查记录，自动去重+保存喵~</summary>
    public void Add(string inputText, string resultText,
        int available, int unavailable, int ignored, double coverage)
    {
        // 和上一条内容一样就不重复存了，节省空间喵~
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

        // 超过 50 条时丢掉最旧的喵~
        if (_items.Count > 50)
            _items.RemoveAt(0);

        Save(); // 每次 Add 即时写入磁盘，防止丢数据喵
    }

    /// <summary>清空所有历史喵~</summary>
    public void Clear()
    {
        _items.Clear();
        Save();
    }

    // 从 JSON 文件加载历史喵~
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
        catch { /* 文件损坏时静默忽略喵~ */ }
    }

    // 将历史写入 JSON 文件喵~
    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { /* 写入失败时静默忽略喵~ */ }
    }
}

/// <summary>
/// 一条检查历史的完整数据喵~
/// </summary>
public class HistoryItem
{
    /// <summary>输入的原文喵~</summary>
    public string InputText { get; init; } = "";

    /// <summary>检查结果的富文本喵~</summary>
    public string ResultText { get; init; } = "";

    /// <summary>可用单词数喵</summary>
    public int Available { get; init; }

    /// <summary>不可用单词数喵...</summary>
    public int Unavailable { get; init; }

    /// <summary>被过滤忽略的单词数喵~</summary>
    public int Ignored { get; init; }

    /// <summary>覆盖率百分比喵~</summary>
    public double Coverage { get; init; }

    /// <summary>检查的时间喵~</summary>
    public DateTime Timestamp { get; init; }
}
