using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinContainers_App.Services;

/// <summary>
/// Minimal markdown renderer for the AI chat. Supports bold, italic, inline
/// code, fenced code blocks, and links. Returns WinUI <see cref="Inline"/>
/// elements so a TextBlock can display them without WebView2.
/// </summary>
public sealed class MarkdownFormatter
{
    public IReadOnlyList<Inline> Format(string markdown)
    {
        var result = new List<Inline>();
        var normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);

        foreach (var paragraph in normalized.Split("\n\n", StringSplitOptions.None))
        {
            var trimmed = paragraph.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                result.Add(BuildCodeBlock(trimmed));
            }
            else
            {
                result.AddRange(BuildParagraph(trimmed));
            }

            result.Add(new LineBreak());
        }

        return result;
    }

    private static List<Inline> BuildParagraph(string text)
    {
        var inlines = new List<Inline>();
        var plain = new StringBuilder();

        void FlushPlain()
        {
            if (plain.Length > 0)
            {
                inlines.Add(new Run { Text = plain.ToString() });
                plain.Clear();
            }
        }

        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    FlushPlain();
                    inlines.Add(new Run
                    {
                        Text = text[(i + 1)..end],
                        FontFamily = new FontFamily("Consolas"),
                        Foreground = CodeBrush
                    });
                    i = end + 1;
                    continue;
                }
            }

            if (text[i] == '*' && i + 1 < text.Length)
            {
                if (text[i + 1] == '*')
                {
                    var end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                    if (end > i + 2)
                    {
                        FlushPlain();
                        inlines.Add(new Run { Text = text[(i + 2)..end], FontWeight = FontWeights.SemiBold });
                        i = end + 2;
                        continue;
                    }
                }
                else
                {
                    var end = text.IndexOf('*', i + 1);
                    if (end > i + 1)
                    {
                        FlushPlain();
                        inlines.Add(new Run { Text = text[(i + 1)..end], FontStyle = FontStyle.Italic });
                        i = end + 1;
                        continue;
                    }
                }
            }

            if (text[i] == '[')
            {
                var close = text.IndexOf(']', i + 1);
                if (close > i && close + 1 < text.Length && text[close + 1] == '(')
                {
                    var urlEnd = text.IndexOf(')', close + 2);
                    if (urlEnd > close + 2)
                    {
                        FlushPlain();
                        var link = new Hyperlink();
                        link.Inlines.Add(new Run { Text = text[(i + 1)..close] });
                        link.NavigateUri = Uri.TryCreate(text[(close + 2)..urlEnd], UriKind.Absolute, out var uri)
                            ? uri
                            : null;
                        inlines.Add(link);
                        i = urlEnd + 1;
                        continue;
                    }
                }
            }

            if (text[i] == '\n')
            {
                FlushPlain();
                inlines.Add(new LineBreak());
                i++;
                continue;
            }

            plain.Append(text[i]);
            i++;
        }

        FlushPlain();
        return inlines;
    }

    private static InlineUIContainer BuildCodeBlock(string block)
    {
        var lines = block.Split('\n').Skip(1).ToList();
        if (lines.Count > 0 && lines[^1].Trim() == "```")
        {
            lines.RemoveAt(lines.Count - 1);
        }

        var border = new Border
        {
            Background = CodeBlockBrush,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            MaxWidth = 680,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        border.Child = new TextBlock
        {
            Text = string.Join("\n", lines),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };

        return new InlineUIContainer { Child = border };
    }

    private static Brush CodeBrush =>
        TryGetBrush("AccentTextFillColorPrimaryBrush", Color.FromArgb(255, 0, 188, 212));

    private static Brush CodeBlockBrush =>
        TryGetBrush("LayerFillColorAltBrush", Color.FromArgb(24, 128, 128, 128));

    private static Brush TryGetBrush(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(fallback);
    }
}
