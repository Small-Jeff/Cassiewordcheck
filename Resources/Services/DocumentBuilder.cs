using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CassieWordCheck.Models;

namespace CassieWordCheck.Services;

public static class DocumentBuilder
{
    private static readonly Brush AvailableBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
    private static readonly Brush UnavailableBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
    private static readonly Brush IgnoredBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly Brush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));

    public static FlowDocument BuildResultDocument(List<CheckResult> results, double width, double fontSize = 14)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif"),
            FontSize = fontSize,
            PageWidth = Math.Max(width - 32, 100),
            PagePadding = new Thickness(0),
            TextAlignment = TextAlignment.Left,
            LineHeight = 1.5,
        };

        var paragraph = new Paragraph { Margin = new Thickness(0) };

        foreach (var r in results)
        {
            if (r.Status == CheckStatus.Separator && r.Text == "\n")
            {
                doc.Blocks.Add(paragraph);
                paragraph = new Paragraph { Margin = new Thickness(0), LineHeight = 1.5 };
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
