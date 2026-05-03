namespace CassieWordCheck.Models;

/// <summary>
/// 检查结果——每个单词/标记检查完毕后会生成一个 CheckResult 喵~
/// </summary>
public class CheckResult
{
    /// <summary>单词/标记的原文喵~</summary>
    public string Text { get; }

    /// <summary>原始文本（与 Text 相同，保留给后续扩展喵）</summary>
    public string Original { get; }

    /// <summary>检查状态：可用/不可用/已忽略/分隔符喵~</summary>
    public CheckStatus Status { get; }

    public CheckResult(string text, CheckStatus status, string? original = null)
    {
        Text = text;
        Status = status;
        Original = original ?? text;
    }
}

/// <summary>
/// 单词检查的状态枚举喵~
/// </summary>
public enum CheckStatus
{
    /// <summary>词库中有这个词，CASSIE 可以读出来喵</summary>
    Available,

    /// <summary>词库里没有，需要特别注意喵...</summary>
    Unavailable,

    /// <summary>被过滤规则跳过了（格式标记/中文等）喵~</summary>
    Ignored,

    /// <summary>换行符或标点分隔，不计入统计喵~</summary>
    Separator
}
