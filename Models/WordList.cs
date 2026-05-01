using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace CassieWordCheck.Models;

public partial class WordList
{
    private FrozenSet<string> _words = FrozenSet<string>.Empty;
    private HashSet<string> _whitelist = [];
    private string? _sourcePath;

    public int WordCount => _words.Count;
    public int WhitelistCount => _whitelist.Count;
    public string? SourcePath => _sourcePath;
    public IReadOnlySet<string> Words => _words;
    public IReadOnlySet<string> Whitelist => _whitelist;

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

            // word:phoneme format
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

        _words = words.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _sourcePath = path;
        return _words.Count;
    }

    private static void AddParts(string text, HashSet<string> words)
    {
        foreach (var part in WordSplitRegex().Split(text))
        {
            var p = part.Trim();
            if (p.Length > 0 && !p.StartsWith('#'))
                words.Add(p);
        }
    }

    public int Reload()
    {
        return _sourcePath is not null ? LoadFromFile(_sourcePath) : 0;
    }

    public bool Check(string word)
    {
        var w = word.Trim();
        if (w.Length == 0) return true;
        return _words.Contains(w) || _whitelist.Contains(w);
    }

    public bool AddToWhitelist(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        return w.Length > 0 && _whitelist.Add(w);
    }

    public bool RemoveFromWhitelist(string word)
    {
        return _whitelist.Remove(word.Trim().ToLowerInvariant());
    }

    public void SetWhitelist(IEnumerable<string> words)
    {
        _whitelist = words
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length > 0)
            .ToHashSet();
    }

    public void ClearWhitelist()
    {
        _whitelist.Clear();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WordSplitRegex();
}
