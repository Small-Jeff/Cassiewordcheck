using System.Text.RegularExpressions;

namespace CassieWordCheck.Models;

public partial class Checker
{
    private readonly WordList _wordlist;

    private static readonly HashSet<string> FactionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "MTF", "UIU", "GOC", "CI", "NTF", "GRU", "FBI"
    };

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

    public List<CheckResult> CheckText(string text)
    {
        var results = new List<CheckResult>();

        if (string.IsNullOrEmpty(text))
            return results;

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                results.Add(new CheckResult("\n", CheckStatus.Separator));

            var tokens = Tokenize(lines[i]);
            foreach (var (kind, value) in tokens)
            {
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
                    results.Add(CheckWord(value));
                }
                else
                {
                    if (IgnoreChinese && ChineseRegex().IsMatch(value))
                        results.Add(new CheckResult(value, CheckStatus.Ignored));
                    else
                        results.Add(new CheckResult(value, CheckStatus.Separator));
                }
            }
        }

        return results;
    }

    private static bool IsIgnoredToken(string value)
    {
        // 纯标点符号
        if (value.Length == 1 && "。，.,?？".Contains(value))
            return true;

        var lower = value.ToLowerInvariant();

        // 格式标签 <...>
        if (value.StartsWith("<") && value.EndsWith(">"))
        {
            var inner = value[1..^1].ToLowerInvariant();
            return inner == "split"
                || inner.StartsWith("size=")
                || inner.StartsWith("color=");
        }

        // 裸词
        if (lower is "link" or "split" or "color")
            return true;

        // pitch_x / pitch_.x / pitch_x.y
        if (PitchRegex().IsMatch(value))
            return true;

        // 十六进制色值
        if (HexRegex().IsMatch(value))
            return true;

        // .G4 .g3 等音高记号
        if (NoteRegex().IsMatch(value))
            return true;

        // JAM_xxx 音效引用
        if (JamRegex().IsMatch(value))
            return true;

        return false;
    }

    private static bool IsNamingToken(string value)
    {
        // 阵营缩写（HashSet O(1) 查找）
        if (FactionNames.Contains(value))
            return true;

        // 希腊字母
        if (GreekLetters.Contains(value))
            return true;

        // 北约代号-x/y/z 如 Alpha-1 Echo-3
        if (NatoRegex().IsMatch(value))
            return true;

        // MtfUnit/MTFUnit 等变体
        var lower = value.ToLowerInvariant();
        if (lower.StartsWith("mtf") && lower.Contains("unit"))
            return true;

        return false;
    }

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

    private CheckResult CheckWord(string word)
    {
        return _wordlist.Check(word)
            ? new CheckResult(word, CheckStatus.Available)
            : new CheckResult(word, CheckStatus.Unavailable);
    }

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

    [GeneratedRegex(@"[\u4e00-\u9fff\u3400-\u4dbf\uf900-\ufaff]")]
    private static partial Regex ChineseRegex();

    [GeneratedRegex(@"[a-zA-Z0-9_.-]+")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"^pitch_\.?\d+(\.\d+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex PitchRegex();

    [GeneratedRegex(@"^(?=.*[a-f])[0-9a-f]{3,8}$", RegexOptions.IgnoreCase)]
    private static partial Regex HexRegex();

    [GeneratedRegex(@"^\.G\d$", RegexOptions.IgnoreCase)]
    private static partial Regex NoteRegex();

    [GeneratedRegex(@"^JAM_\d+(_\d+)*$", RegexOptions.IgnoreCase)]
    private static partial Regex JamRegex();

    [GeneratedRegex(@"^(Alpha|Bravo|Charlie|Delta|Echo|Foxtrot|Golf|Hotel|India|Juliett|Kilo|Lima|Mike|November|Oscar|Papa|Quebec|Romeo|Sierra|Tango|Uniform|Victor|Whiskey|Xray|Yankee|Zulu)[-\s]\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex NatoRegex();
}
