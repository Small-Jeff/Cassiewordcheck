using System.Text.RegularExpressions;

namespace CassieWordCheck.Models;

public partial class Checker
{
    private readonly WordList _wordlist;

    public bool IgnoreChinese { get; set; } = true;
    public bool IgnoreAngleBrackets { get; set; } = true;

    public Checker(WordList wordlist)
    {
        _wordlist = wordlist;
    }

    public List<CheckResult> CheckText(string text)
    {
        var results = new List<CheckResult>();

        if (string.IsNullOrEmpty(text))
            return results;

        var textToCheck = text;
        if (IgnoreAngleBrackets)
            textToCheck = AngleBracketRegex().Replace(textToCheck, "");

        var lines = textToCheck.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                results.Add(new CheckResult("\n", CheckStatus.Separator));

            var tokens = Tokenize(lines[i]);
            foreach (var (kind, value) in tokens)
            {
                if (kind == "word")
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
    {
        var results = CheckText(text);
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
            ["char_count"] = text.Length,
        };
    }

    [GeneratedRegex(@"[\u4e00-\u9fff\u3400-\u4dbf\uf900-\ufaff]")]
    private static partial Regex ChineseRegex();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex AngleBracketRegex();

    [GeneratedRegex(@"[a-zA-Z0-9_.-]+")]
    private static partial Regex WordRegex();
}
