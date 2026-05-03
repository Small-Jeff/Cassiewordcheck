using System.Collections.Frozen;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace CassieWordCheck.Models;

/// <summary>
/// 词库——用 FrozenSet 做 O(1) 查询，加载后不可变喵~
/// 支持白名单、多格式导入（TXT/CSV/Excel）喵
/// </summary>
public partial class WordList
{
    // 核心词库，加载后不可变，查询极快喵！
    private FrozenSet<string> _words = FrozenSet<string>.Empty;

    // 白名单——用户手动添加的豁免词，运行时可变喵~
    private HashSet<string> _whitelist = [];

    // 当前词库文件的路径，重载时需要喵~
    private string? _sourcePath;

    public int WordCount => _words.Count;
    public int WhitelistCount => _whitelist.Count;
    public string? SourcePath => _sourcePath;
    public IReadOnlySet<string> Words => _words;
    public IReadOnlySet<string> Whitelist => _whitelist;

    /// <summary>从文件加载词库（先清空再加载）喵~</summary>
    public int LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Word list file not found.", path);

        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            // 格式 "word:phoneme" —— 只取冒号前的部分喵~
            if (trimmed.Contains(':') && !trimmed.StartsWith('.'))
            {
                var word = trimmed.Split(':', 2)[0].Trim();
                AddParts(word, words);
            }
            else
            {
                AddParts(trimmed, words);
            }
        }

        // 转为 FrozenSet，之后不可变，查询性能更好喵！
        _words = words.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _sourcePath = path;
        return _words.Count;
    }

    // 按空白符拆分后逐个加入（一行可能有多个单词喵~）
    private static void AddParts(string text, HashSet<string> words)
    {
        foreach (var part in WordSplitRegex().Split(text))
        {
            var p = part.Trim();
            if (p.Length > 0 && !p.StartsWith('#'))
                words.Add(p);
        }
    }

    /// <summary>重新加载当前词库喵~</summary>
    public int Reload()
    {
        return _sourcePath is not null ? LoadFromFile(_sourcePath) : 0;
    }

    /// <summary>查询单词是否在词库或白名单中喵~</summary>
    public bool Check(string word)
    {
        var w = word.Trim();
        if (w.Length == 0) return true;
        return _words.Contains(w) || _whitelist.Contains(w);
    }

    /// <summary>添加单词到白名单喵~</summary>
    public bool AddToWhitelist(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        return w.Length > 0 && _whitelist.Add(w);
    }

    /// <summary>从白名单移除单词喵~</summary>
    public bool RemoveFromWhitelist(string word)
    {
        return _whitelist.Remove(word.Trim().ToLowerInvariant());
    }

    /// <summary>批量设置白名单喵~</summary>
    public void SetWhitelist(IEnumerable<string> words)
    {
        _whitelist = words
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length > 0)
            .ToHashSet();
    }

    /// <summary>清空白名单喵~</summary>
    public void ClearWhitelist()
    {
        _whitelist.Clear();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WordSplitRegex();

    // ===== 批量导入（支持 TXT/CSV/Excel） =====

    /// <summary>从文件导入附加单词，合并到已有词库喵~</summary>
    public int AddFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var newWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 按后缀名选择解析方式喵~
        switch (ext)
        {
            case ".txt":
                LoadFromTxt(path, newWords);
                break;
            case ".csv":
                LoadFromCsv(path, newWords);
                break;
            case ".xlsx":
                LoadFromXlsx(path, newWords);
                break;
            default:
                throw new NotSupportedException($"Unsupported format: {ext}");
        }

        if (newWords.Count == 0) return 0;

        // 合并到已有词库，重新冻结喵~
        var merged = new HashSet<string>(_words, StringComparer.OrdinalIgnoreCase);
        foreach (var w in newWords) merged.Add(w);
        _words = merged.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        return newWords.Count;
    }

    // 读取 TXT：和 LoadFromFile 一样的格式喵~
    private static void LoadFromTxt(string path, HashSet<string> words)
    {
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            if (trimmed.Contains(':') && !trimmed.StartsWith('.'))
            {
                var word = trimmed.Split(':', 2)[0].Trim();
                AddPartsDirect(word, words);
            }
            else
            {
                AddPartsDirect(trimmed, words);
            }
        }
    }

    // 读取 CSV：取第一列（逗号或分号分隔）喵~
    private static void LoadFromCsv(string path, HashSet<string> words)
    {
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(','))
                continue;

            var first = trimmed.Split(',', ';')[0].Trim().Trim('"', '\'');
            if (first.Length > 0)
                words.Add(first.ToLowerInvariant());
        }
    }

    // 读取 Excel：取第一个工作表的 A 列喵~
    private static void LoadFromXlsx(string path, HashSet<string> words)
    {
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet(1);
        var rows = sheet.RangeUsed();

        if (rows is null) return;

        foreach (var row in rows.Rows())
        {
            var cell = row.Cell(1).GetString().Trim();
            if (cell.Length > 0)
                words.Add(cell.ToLowerInvariant());
        }
    }

    private static void AddPartsDirect(string text, HashSet<string> words)
    {
        foreach (var part in WordSplitRegex().Split(text))
        {
            var p = part.Trim();
            if (p.Length > 0 && !p.StartsWith('#'))
                words.Add(p.ToLowerInvariant());
        }
    }
}
