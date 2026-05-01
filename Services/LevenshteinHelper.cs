namespace CassieWordCheck.Services;

public static class LevenshteinHelper
{
    public static int Distance(string a, string b)
    {
        var aLen = a.Length;
        var bLen = b.Length;
        var d = new int[aLen + 1, bLen + 1];

        for (int i = 0; i <= aLen; i++) d[i, 0] = i;
        for (int j = 0; j <= bLen; j++) d[0, j] = j;

        for (int i = 1; i <= aLen; i++)
        {
            for (int j = 1; j <= bLen; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[aLen, bLen];
    }

    public static List<(string word, int distance)> FindClosest(string word, IEnumerable<string> candidates, int maxResults = 3)
    {
        return candidates
            .Select(c => (word: c, distance: Distance(word.ToLowerInvariant(), c.ToLowerInvariant())))
            .Where(x => x.distance <= Math.Max(3, word.Length / 3))
            .OrderBy(x => x.distance)
            .ThenBy(x => x.word)
            .Take(maxResults)
            .ToList();
    }
}
