using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CassieWordCheck.Services;

/// <summary>
/// 简单的 Markdown → FlowDocument 转换器（支持标题/粗体/斜体/代码/链接/列表/分割线）
/// </summary>
public static class MarkdownConverter
{
    private static readonly Brush AccentBrush = new SolidColorBrush(Color.FromRgb(0x6C, 0x5C, 0xE7));
    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));

    private static readonly FontFamily BodyFont = new("Segoe UI Variable Text, Segoe UI, sans-serif");
    private static readonly FontFamily MonoFont = new("Cascadia Code, Consolas, monospace");

    public static FlowDocument Convert(string markdown, double width)
    {
        var doc = new FlowDocument
        {
            FontFamily = BodyFont,
            FontSize = 13,
            PageWidth = Math.Max(width - 32, 100),
            PagePadding = new Thickness(0),
            LineHeight = 1.6,
            TextAlignment = TextAlignment.Left,
        };

        var lines = markdown.Split('\n');
        var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 4) };

        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                FlushParagraph(doc, ref paragraph);
                continue;
            }

            // ![alt](path) — 图片（支持后跟文字并列显示）
            if (trimmed.StartsWith("![") && trimmed.Contains("]("))
            {
                FlushParagraph(doc, ref paragraph);
                var altEnd = trimmed.IndexOf("](");
                var pathEnd = trimmed.IndexOf(')', altEnd + 2);
                if (altEnd > 1 && pathEnd > altEnd)
                {
                    var imgPath = trimmed[(altEnd + 2)..pathEnd];
                    var img = TryLoadImage(imgPath);
                    var remaining = trimmed[(pathEnd + 1)..].TrimStart();
                    var hasText = remaining.Length > 0;

                    if (img is not null)
                    {
                        var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };

                        var imgBorder = new Border
                        {
                            Child = img,
                            CornerRadius = new CornerRadius(8),
                            ClipToBounds = true,
                            Width = 80, Height = 80,
                            VerticalAlignment = VerticalAlignment.Top,
                        };
                        container.Children.Add(imgBorder);

                        // 文字并列（支持 \n 换行和 **加粗**）
                        if (hasText)
                        {
                            var textPanel = new StackPanel
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(12, 0, 0, 0),
                            };
                            // 先按 \n 拆行（文字中的字面反斜杠+n）
                            var subLines = remaining.Split(["\\n"], StringSplitOptions.None);
                            foreach (var lineText in subLines)
                            {
                                var tb = new TextBlock
                                {
                                    FontFamily = BodyFont,
                                    TextWrapping = TextWrapping.Wrap,
                                    Margin = new Thickness(0, 1, 0, 1),
                                };

                                var raw = lineText;

                                // # 标题行
                                if (raw.StartsWith('#'))
                                {
                                    raw = raw[1..].TrimStart();
                                    tb.FontSize = 16;
                                    tb.FontWeight = FontWeights.SemiBold;
                                    tb.Foreground = TextBrush;
                                }
                                // > 引用行
                                else if (raw.StartsWith('>'))
                                {
                                    raw = raw[1..].TrimStart();
                                    tb.FontSize = 13;
                                    tb.FontStyle = FontStyles.Italic;
                                    tb.Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x9A));
                                }
                                // ~ 特殊行（斜体小字粉色）
                                else if (raw.StartsWith('~'))
                                {
                                    raw = raw[1..].TrimStart();
                                    tb.FontSize = 12;
                                    tb.FontStyle = FontStyles.Italic;
                                    tb.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7E, 0xB3));
                                }
                                else
                                {
                                    tb.FontSize = 13;
                                    tb.Foreground = TextBrush;
                                }

                                // 解析 **加粗**
                                var parts = raw.Split(["**"], StringSplitOptions.None);
                                for (int i = 0; i < parts.Length; i++)
                                {
                                    if (string.IsNullOrEmpty(parts[i])) continue;
                                    if (i % 2 == 1)
                                        tb.Inlines.Add(new Run(parts[i]) { FontWeight = FontWeights.Bold });
                                    else
                                        tb.Inlines.Add(new Run(parts[i]));
                                }
                                textPanel.Children.Add(tb);
                            }
                            container.Children.Add(textPanel);
                        }

                        doc.Blocks.Add(new BlockUIContainer(container));
                    }
                    else if (hasText)
                    {
                        // 图片加载失败，纯显示文字
                        ParseInline(remaining, paragraph);
                        FlushParagraph(doc, ref paragraph);
                    }
                }
                continue;
            }

            // 分割线
            if (trimmed is "---" or "———" or "——" or "___")
            {
                FlushParagraph(doc, ref paragraph);
                doc.Blocks.Add(new BlockUIContainer(
                    new Border { Height = 1, Background = MutedBrush, Margin = new Thickness(0, 6, 0, 6), Opacity = 0.4 }));
                continue;
            }

            // 标题 ###
            // 【xxx】→ section header
            if (trimmed.StartsWith('【') && trimmed.Contains('】'))
            {
                FlushParagraph(doc, ref paragraph);
                var end = trimmed.IndexOf('】');
                doc.Blocks.Add(MakeHeader(trimmed[(end + 1)..].TrimStart(), 14, FontWeights.SemiBold));
                continue;
            }

            if (trimmed.StartsWith("### "))
            {
                FlushParagraph(doc, ref paragraph);
                doc.Blocks.Add(MakeHeader(trimmed[4..], 13, FontWeights.SemiBold));
                continue;
            }
            if (trimmed.StartsWith("## "))
            {
                FlushParagraph(doc, ref paragraph);
                doc.Blocks.Add(MakeHeader(trimmed[3..], 15, FontWeights.SemiBold));
                continue;
            }
            if (trimmed.StartsWith("# "))
            {
                FlushParagraph(doc, ref paragraph);
                doc.Blocks.Add(MakeHeader(trimmed[2..], 18, FontWeights.Bold));
                continue;
            }

            // 列表项
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("• "))
            {
                FlushParagraph(doc, ref paragraph);
                var itemPara = new Paragraph { Margin = new Thickness(18, 0, 0, 4) };
                itemPara.Inlines.Add(new Run("  •  ") { Foreground = AccentBrush });
                ParseInline(trimmed[2..], itemPara);
                doc.Blocks.Add(itemPara);
                continue;
            }

            // 普通行
            if (paragraph.Inlines.Count > 0)
                paragraph.Inlines.Add(new Run(" ")); // 同一段落内空格
            ParseInline(trimmed, paragraph);
        }

        FlushParagraph(doc, ref paragraph);
        return doc;
    }

    private static void FlushParagraph(FlowDocument doc, ref Paragraph p)
    {
        if (p.Inlines.Count > 0)
        {
            doc.Blocks.Add(p);
            p = new Paragraph { Margin = new Thickness(0, 0, 0, 4) };
        }
    }

    private static Paragraph MakeHeader(string text, double fontSize, FontWeight weight)
    {
        return new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 6),
            Inlines = { new Run(text.Trim()) { FontSize = fontSize, FontWeight = weight } },
        };
    }

    /// <summary>
    /// 解析行内格式：**粗体** *斜体* `代码` [链接文字](url)
    /// </summary>
    private static void ParseInline(string text, Paragraph para)
    {
        int pos = 0;
        while (pos < text.Length)
        {
            // `code` — 优先匹配（避免与其他格式冲突）
            int codeStart = text.IndexOf('`', pos);
            if (codeStart >= 0 && codeStart == pos)
            {
                int codeEnd = text.IndexOf('`', codeStart + 1);
                int close = codeEnd > codeStart ? codeEnd : text.Length;
                var code = text[(codeStart + 1)..close];
                para.Inlines.Add(new Run(code)
                {
                    FontFamily = MonoFont,
                    FontSize = 12,
                    Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                });
                pos = close + 1;
                continue;
            }
            if (codeStart >= 0 && codeStart > pos)
            {
                // 有 `code` 在后面，先处理前面的文本
                ParseBoldItalicLink(text[pos..codeStart], para);
                pos = codeStart;
                continue;
            }

            // 剩余的纯文本
            ParseBoldItalicLink(text[pos..], para);
            pos = text.Length;
        }
    }

    private static void ParseBoldItalicLink(string text, Paragraph para)
    {
        int pos = 0;
        while (pos < text.Length)
        {
            // [link](url)
            int linkStart = text.IndexOf('[');
            if (linkStart >= 0 && linkStart == pos)
            {
                int linkEnd = text.IndexOf("](", linkStart);
                int urlEnd = linkEnd > 0 ? text.IndexOf(')', linkEnd + 2) : -1;
                if (linkEnd > 0 && urlEnd > linkEnd)
                {
                    var linkText = text[(linkStart + 1)..linkEnd];
                    var url = text[(linkEnd + 2)..urlEnd];
                    var hyperlink = new Hyperlink(new Run(linkText))
                    {
                        NavigateUri = new Uri(url),
                        Foreground = AccentBrush,
                        TextDecorations = TextDecorations.Underline,
                    };
                    hyperlink.RequestNavigate += (s, e) =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = e.Uri.ToString(),
                            UseShellExecute = true,
                        });
                        e.Handled = true;
                    };
                    para.Inlines.Add(hyperlink);
                    pos = urlEnd + 1;
                    continue;
                }
            }
            if (linkStart > pos)
            {
                ParseBoldItalic(text[pos..linkStart], para);
                pos = linkStart;
                continue;
            }

            // **粗体** 和 *斜体*
            ParseBoldItalic(text[pos..], para);
            pos = text.Length;
        }
    }

    /// <summary>尝试加载图片（本地文件多路径 fallback + 网络 URL）</summary>
    private static Image? TryLoadImage(string path)
    {
        try
        {
            // 网络 URL
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                return new Image
                {
                    Source = bitmap,
                    Width = 80,
                    Height = 80,
                    Stretch = Stretch.UniformToFill,
                };
            }

            // 本地文件多路径 fallback
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var processDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
            var candidates = new[]
            {
                Path.Combine(dir, path),
                Path.Combine(processDir, path),
                Path.Combine(dir, "data", path),
                Path.Combine(processDir, "data", path),
                Path.Combine(dir, "Resources", "Locales", path),
            };

            var fullPath = candidates.FirstOrDefault(File.Exists);
            if (fullPath is null) return null;

            var localBitmap = new BitmapImage();
            localBitmap.BeginInit();
            localBitmap.CacheOption = BitmapCacheOption.OnLoad;
            localBitmap.UriSource = new Uri(fullPath);
            localBitmap.EndInit();

            return new Image
            {
                Source = localBitmap,
                Width = 80,
                Height = 80,
                Stretch = Stretch.UniformToFill,
            };
        }
        catch { return null; }
    }

    private static void ParseBoldItalic(string text, Paragraph para)
    {
        int pos = 0;
        while (pos < text.Length)
        {
            // **bold**
            int bStart = text.IndexOf("**", pos);
            if (bStart >= 0)
            {
                int bEnd = text.IndexOf("**", bStart + 2);
                if (bEnd > bStart)
                {
                    if (bStart > pos)
                        para.Inlines.Add(new Run(text[pos..bStart]));
                    para.Inlines.Add(new Run(text[(bStart + 2)..bEnd]) { FontWeight = FontWeights.Bold });
                    pos = bEnd + 2;
                    continue;
                }
            }

            // *italic*
            int iStart = text.IndexOf('*', pos);
            if (iStart >= 0)
            {
                int iEnd = text.IndexOf('*', iStart + 1);
                if (iEnd > iStart && !text.Substring(iStart, Math.Min(2, text.Length - iStart)).Contains("**"))
                {
                    if (iStart > pos)
                        para.Inlines.Add(new Run(text[pos..iStart]));
                    para.Inlines.Add(new Run(text[(iStart + 1)..iEnd]) { FontStyle = FontStyles.Italic });
                    pos = iEnd + 1;
                    continue;
                }
            }

            // 剩余文本
            para.Inlines.Add(new Run(text[pos..]));
            pos = text.Length;
        }
    }
}
