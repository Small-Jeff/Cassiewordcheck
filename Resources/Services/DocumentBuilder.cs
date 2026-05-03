using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CassieWordCheck.Models;

namespace CassieWordCheck.Services;

/// <summary>
/// 结果文档构建器——将检测结果 List&lt;CheckResult&gt; 转为 WPF FlowDocument 富文本喵~
/// 绿色=可用、红色=不可用（下划线+悬停提示）、灰色=已忽略喵！
/// </summary>
public static class DocumentBuilder
{
    // 四个状态对应的颜色喵~
    private static readonly Brush AvailableBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)); // 翡翠绿
    private static readonly Brush UnavailableBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)); // 错误红
    private static readonly Brush IgnoredBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)); // 浅灰
    private static readonly Brush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)); // 白

    /// <summary>构建结果 FlowDocument，输入框宽度变化时外部会重新调用喵~</summary>
    public static FlowDocument BuildResultDocument(List<CheckResult> results, double width, double fontSize = 14)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Cascadia Code, Consolas, monospace"),
            FontSize = fontSize,
            PageWidth = Math.Max(width - 32, 100), // 留出内边距喵~
            PagePadding = new Thickness(0),
            TextAlignment = TextAlignment.Left,
            LineStackingStrategy = LineStackingStrategy.MaxHeight,
        };

        var paragraph = new Paragraph { Margin = new Thickness(0) };

        foreach (var r in results)
        {
            // 换行符 → LineBreak（比换段落轻量，不产生额外间距喵！）
            if (r.Status == CheckStatus.Separator && r.Text == "\n")
            {
                paragraph.Inlines.Add(new LineBreak());
                continue;
            }

            var run = new Run(r.Text)
            {
                Foreground = r.Status switch
                {
                    CheckStatus.Available => AvailableBrush,
                    CheckStatus.Unavailable => UnavailableBrush,
                    CheckStatus.Ignored => IgnoredBrush,
                    _ => DefaultBrush,
                },
                FontWeight = r.Status == CheckStatus.Unavailable ? FontWeights.SemiBold : FontWeights.Normal,
            };

            // 不可用词加下划线 + 悬停提示喵~
            if (r.Status == CheckStatus.Unavailable)
            {
                run.TextDecorations = TextDecorations.Underline;
                run.ToolTip = "Not in CASSIE vocabulary";
            }

            paragraph.Inlines.Add(run);
        }

        if (paragraph.Inlines.Count > 0)
            doc.Blocks.Add(paragraph);

        return doc;
    }
}
