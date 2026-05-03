using System.Text.RegularExpressions;

namespace CassieWordCheck.Models;

/// <summary>
/// 检查引擎——分词 → 过滤 → 逐词比对词库，生成检查结果喵
/// 支持格式标记过滤、命名过滤、中文忽略喵~
/// </summary>
public partial class Checker
{
    private readonly WordList _wordlist;

    // 相关专有名词（O(1) 查找喵）
    private static readonly HashSet<string> FactionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "MTF", "UIU", "GOC", "CI", "NTF", "GRU", "FBI"
    };

    // 北约代号和希腊字母喵~
    private static readonly HashSet<string> GreekLetters = new(StringComparer.OrdinalIgnoreCase)
    {
        "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta",
        "Iota", "Kappa", "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi",
        "Rho", "Sigma", "Tau", "Upsilon", "Phi", "Chi", "Psi", "Omega"
    };

    public bool IgnoreChinese { get; set; } = true;
    public bool FilterFormatting { get; set; } = true;
    public bool FilterNaming { get; set; } = true;

    public Checker(WordList wordlist)
    {
        _wordlist = wordlist;
    }

    /// <summary>
    /// 核心方法：逐行逐词检查文本喵~
    /// </summary>
    public List<CheckResult> CheckText(string text)
    {
        var results = new List<CheckResult>();

        if (string.IsNullOrEmpty(text))
            return results;

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            // 每行之间插入换行分隔符喵~
            if (i > 0)
                results.Add(new CheckResult("\n", CheckStatus.Separator));

            var tokens = Tokenize(lines[i]);
            foreach (var (kind, value) in tokens)
            {
                // 格式标记过滤优先喵
                if (FilterFormatting && IsIgnoredToken(value))
                {
                    results.Add(new CheckResult(value, CheckStatus.Ignored));
                }
                else if (FilterNaming && IsNamingToken(value))
                {
                    results.Add(new CheckResult(value, CheckStatus.Ignored));
                }
                else if (kind == "word")
                {
                    // 查词库喵~
                    results.Add(CheckWord(value));
                }
                else
                {
                    // 非单词（标点、中文等）and 检查是否要忽略中文喵
                    if (IgnoreChinese && ChineseRegex().IsMatch(value))
                        results.Add(new CheckResult(value, CheckStatus.Ignored));
                    else
                        results.Add(new CheckResult(value, CheckStatus.Separator));
                }
            }
        }

        return results;
    }

    /// <summary>判断是否应该被格式过滤忽略喵~</summary>
    private static bool IsIgnoredToken(string value)
    {
        // 纯标点符号喵喵喵
        if (value.Length == 1 && "。，.,?？".Contains(value))
            return true;

        var lower = value.ToLowerInvariant();

        // CASSIE 格式标签 <...>
        if (value.StartsWith("<") && value.EndsWith(">"))
        {
            var inner = value[1..^1].ToLowerInvariant();
            return inner == "split"
                || inner.StartsWith("size=")
                || inner.StartsWith("color=");
        }

        // 裸词格式标记喵~
        if (lower is "link" or "split" or "color")
            return true;

        // pitch 音高标记
        if (PitchRegex().IsMatch(value))
            return true;

        // 十六进制色值（如 #FF0000）
        if (HexRegex().IsMatch(value))
            return true;

        // .G4 .g3 等八度记号
        if (NoteRegex().IsMatch(value))
            return true;

        // JAM_xxx 音效引用
        if (JamRegex().IsMatch(value))
            return true;

        return false;
    }

    /// <summary>判断是否应被命名过滤忽略喵~</summary>
    private static bool IsNamingToken(string value)
    {
        // 阵营缩写（HashSet O(1) 查找）
        if (FactionNames.Contains(value))
            return true;

        // 希腊字母喵
        if (GreekLetters.Contains(value))
            return true;

        // 北约代号-x/y/z 如 Alpha-1 Echo-3
        if (NatoRegex().IsMatch(value))
            return true;

        // MtfUnit/MTFUnit 变体
        var lower = value.ToLowerInvariant();
        if (lower.StartsWith("mtf") && lower.Contains("unit"))
            return true;

        return false;
    }

    /// <summary>将一行文本拆分为单词和非单词 token 喵~</summary>
    private static List<(string kind, string value)> Tokenize(string line)
    {
        var tokens = new List<(string kind, string value)>();
        int pos = 0;

        foreach (Match m in WordRegex().Matches(line))
        {
            if (m.Index > pos)
                tokens.Add(("other", line[pos..m.Index]));
            tokens.Add(("word", m.Value));
            pos = m.Index + m.Length;
        }

        if (pos < line.Length)
            tokens.Add(("other", line[pos..]));

        return tokens;
    }

    /// <summary>查词库，返回可用/不可用喵~</summary>
    private CheckResult CheckWord(string word)
    {
        return _wordlist.Check(word)
            ? new CheckResult(word, CheckStatus.Available)
            : new CheckResult(word, CheckStatus.Unavailable);
    }

    /// <summary>计算统计信息：总数、可用、不可用、覆盖率喵~</summary>
    public Dictionary<string, object> GetStatistics(string text)
        => GetStatistics(CheckText(text), text);

    public Dictionary<string, object> GetStatistics(List<CheckResult> results, string? originalText = null)
    {
        int total = results.Count(r => r.Status is CheckStatus.Available or CheckStatus.Unavailable);
        int available = results.Count(r => r.Status == CheckStatus.Available);
        int unavailable = results.Count(r => r.Status == CheckStatus.Unavailable);
        int ignored = results.Count(r => r.Status == CheckStatus.Ignored);
        double coverage = total > 0 ? (double)available / total * 100.0 : 100.0;

        return new()
        {
            ["total"] = total,
            ["available"] = available,
            ["unavailable"] = unavailable,
            ["ignored"] = ignored,
            ["coverage"] = coverage,
            ["char_count"] = originalText?.Length ?? 0,
        };
    }

    // ===== 正则表达式（编译时生成，性能更好喵） =====

    /// <summary>匹配中文字符喵~</summary>
    [GeneratedRegex(@"[\u4e00-\u9fff\u3400-\u4dbf\uf900-\ufaff]")]
    private static partial Regex ChineseRegex();

    /// <summary>匹配英文单词（含数字和常见符号）喵~</summary>
    [GeneratedRegex(@"[a-zA-Z0-9_.-]+")]
    private static partial Regex WordRegex();

    /// <summary>匹配 pitch_ 音高标记喵~</summary>
    [GeneratedRegex(@"^pitch_\.?\d+(\.\d+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex PitchRegex();

    /// <summary>匹配十六进制色值（至少含一个字母）喵~</summary>
    [GeneratedRegex(@"^(?=.*[a-f])[0-9a-f]{3,8}$", RegexOptions.IgnoreCase)]
    private static partial Regex HexRegex();

    /// <summary>匹配 .G4 .g3 等八度记号喵~</summary>
    [GeneratedRegex(@"^\.G\d$", RegexOptions.IgnoreCase)]
    private static partial Regex NoteRegex();

    /// <summary>匹配 JAM_xxx 音效引用喵~</summary>
    [GeneratedRegex(@"^JAM_\d+(_\d+)*$", RegexOptions.IgnoreCase)]
    private static partial Regex JamRegex();

    /// <summary>匹配北约代号（Alpha-1, Echo-3 等）喵~</summary>
    [GeneratedRegex(@"^(Alpha|Bravo|Charlie|Delta|Echo|Foxtrot|Golf|Hotel|India|Juliett|Kilo|Lima|Mike|November|Oscar|Papa|Quebec|Romeo|Sierra|Tango|Uniform|Victor|Whiskey|Xray|Yankee|Zulu)[-\s]\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex NatoRegex();
}
