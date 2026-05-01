namespace CassieWordCheck.Models;

public class CheckResult
{
    public string Text { get; }
    public string Original { get; }
    public CheckStatus Status { get; }

    public CheckResult(string text, CheckStatus status, string? original = null)
    {
        Text = text;
        Status = status;
        Original = original ?? text;
    }
}

public enum CheckStatus
{
    Available,
    Unavailable,
    Ignored,
    Separator
}
