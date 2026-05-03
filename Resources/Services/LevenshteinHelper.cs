namespace CassieWordCheck.Services;

/// <summary>
/// Levenshtein 编辑距离——计算两个单词的相似度喵~
/// 用于给不可用词推荐最相近的可用词喵！
/// 算法复杂度 O(n*m)，但词库查询最多只取前 3 个结果，很快喵~
/// </summary>
public static class LevenshteinHelper
{
    /// <summary>
    /// 计算两个字符串的编辑距离喵！
    /// 距离越小越相似，0 表示完全相同喵~
    /// </summary>
    public static int Distance(string a, string b)
    {
        var aLen = a.Length;
        var bLen = b.Length;
        var d = new int[aLen + 1, bLen + 1];

        // 初始化边界：空串到任意串的距离就是长度喵~
        for (int i = 0; i <= aLen; i++) d[i, 0] = i;
        for (int j = 0; j <= bLen; j++) d[0, j] = j;

        // 动态规划填表喵~
        for (int i = 1; i <= aLen; i++)
        {
            for (int j = 1; j <= bLen; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), // 删除 or 插入
                    d[i - 1, j - 1] + cost); // 替换
            }
        }

        return d[aLen, bLen];
    }

    /// <summary>
    /// 从候选词中找出与目标词最接近的几个喵~
    /// 距离阈值 = max(3, word.Length / 3)，太长的不考虑喵！
    /// </summary>
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
