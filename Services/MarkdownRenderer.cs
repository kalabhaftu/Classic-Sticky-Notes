using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ClassicStickyNotes.Services
{
    public class MarkdownRenderer
    {
        public static FlowDocument Render(
            string markdown, 
            NoteTheme theme, 
            string fontFamilyName, 
            double baseFontSize,
            Action<int, bool>? onTaskToggled = null)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(14, 10, 14, 10),
                FontFamily = new FontFamily(fontFamilyName),
                FontSize = baseFontSize,
                Foreground = theme.TextBrush,
                Background = Brushes.Transparent,
                TextAlignment = TextAlignment.Left,
                IsHyphenationEnabled = false
            };

            if (string.IsNullOrWhiteSpace(markdown))
            {
                var emptyPara = new Paragraph(new Run("Empty note. Start typing..."))
                {
                    FontStyle = FontStyles.Italic,
                    Foreground = theme.SubduedTextBrush,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                doc.Blocks.Add(emptyPara);
                return doc;
            }

            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            int i = 0;

            while (i < lines.Length)
            {
                string line = lines[i];

                // 1. Code Block (```)
                if (line.TrimStart().StartsWith("```"))
                {
                    int startLine = i;
                    i++;
                    var codeLines = new List<string>();
                    while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                    {
                        codeLines.Add(lines[i]);
                        i++;
                    }
                    if (i < lines.Length) i++; // Skip closing ```

                    var codeBlock = CreateCodeBlock(string.Join(Environment.NewLine, codeLines), theme, baseFontSize);
                    doc.Blocks.Add(codeBlock);
                    continue;
                }

                // 2. Horizontal Rule (---, ***, ___)
                string trimmed = line.Trim();
                if (trimmed == "---" || trimmed == "***" || trimmed == "___")
                {
                    doc.Blocks.Add(CreateHorizontalRule(theme));
                    i++;
                    continue;
                }

                // 3. Headings (# H1, ## H2, ### H3, #### H4)
                if (trimmed.StartsWith("#"))
                {
                    var match = Regex.Match(trimmed, @"^(#{1,6})\s+(.*)$");
                    if (match.Success)
                    {
                        int level = match.Groups[1].Length;
                        string headingText = match.Groups[2].Value;
                        doc.Blocks.Add(CreateHeading(headingText, level, theme, baseFontSize));
                        i++;
                        continue;
                    }
                }

                // 4. Blockquotes (> quote)
                if (trimmed.StartsWith(">"))
                {
                    var quoteLines = new List<string>();
                    while (i < lines.Length && lines[i].TrimStart().StartsWith(">"))
                    {
                        string qLine = lines[i].TrimStart();
                        quoteLines.Add(qLine.Length > 1 && qLine[1] == ' ' ? qLine.Substring(2) : qLine.Substring(1));
                        i++;
                    }
                    doc.Blocks.Add(CreateBlockquote(string.Join(" ", quoteLines), theme, baseFontSize));
                    continue;
                }

                // 5. Task List / Checkbox (- [ ] or - [x])
                var taskMatch = Regex.Match(trimmed, @"^[-*+]\s+\[([ xX])\]\s+(.*)$");
                if (taskMatch.Success)
                {
                    int lineIndex = i;
                    bool isChecked = taskMatch.Groups[1].Value.Trim().Length > 0;
                    string itemText = taskMatch.Groups[2].Value;

                    doc.Blocks.Add(CreateTaskItem(itemText, isChecked, lineIndex, theme, baseFontSize, onTaskToggled));
                    i++;
                    continue;
                }

                // 6. Bulleted List (- item or * item)
                if (Regex.IsMatch(trimmed, @"^[-*+]\s+"))
                {
                    var list = new List
                    {
                        MarkerStyle = TextMarkerStyle.Disc,
                        Margin = new Thickness(14, 2, 0, 4),
                        Padding = new Thickness(0)
                    };

                    while (i < lines.Length && Regex.IsMatch(lines[i].Trim(), @"^[-*+]\s+"))
                    {
                        // Check if it's a task item; if so, break to handle separately
                        if (Regex.IsMatch(lines[i].Trim(), @"^[-*+]\s+\[([ xX])\]\s+"))
                            break;

                        string itemText = Regex.Replace(lines[i].Trim(), @"^[-*+]\s+", "");
                        var li = new ListItem();
                        var p = new Paragraph { Margin = new Thickness(0, 1, 0, 1) };
                        AppendInlineFormatting(p.Inlines, itemText, theme, baseFontSize);
                        li.Blocks.Add(p);
                        list.ListItems.Add(li);
                        i++;
                    }

                    if (list.ListItems.Count > 0)
                    {
                        doc.Blocks.Add(list);
                        continue;
                    }
                }

                // 7. Numbered List (1. item)
                if (Regex.IsMatch(trimmed, @"^\d+\.\s+"))
                {
                    var list = new List
                    {
                        MarkerStyle = TextMarkerStyle.Decimal,
                        Margin = new Thickness(18, 2, 0, 4),
                        Padding = new Thickness(0)
                    };

                    while (i < lines.Length && Regex.IsMatch(lines[i].Trim(), @"^\d+\.\s+"))
                    {
                        string itemText = Regex.Replace(lines[i].Trim(), @"^\d+\.\s+", "");
                        var li = new ListItem();
                        var p = new Paragraph { Margin = new Thickness(0, 1, 0, 1) };
                        AppendInlineFormatting(p.Inlines, itemText, theme, baseFontSize);
                        li.Blocks.Add(p);
                        list.ListItems.Add(li);
                        i++;
                    }

                    doc.Blocks.Add(list);
                    continue;
                }

                // 8. Empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    var spacer = new Paragraph
                    {
                        FontSize = baseFontSize * 0.5,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    doc.Blocks.Add(spacer);
                    i++;
                    continue;
                }

                // 9. Standard Paragraph
                var para = new Paragraph
                {
                    Margin = new Thickness(0, 2, 0, 4),
                    LineHeight = baseFontSize * 1.35
                };
                AppendInlineFormatting(para.Inlines, line, theme, baseFontSize);
                doc.Blocks.Add(para);
                i++;
            }

            return doc;
        }

        private static Block CreateHeading(string text, int level, NoteTheme theme, double baseFontSize)
        {
            double size = level switch
            {
                1 => baseFontSize * 1.5,
                2 => baseFontSize * 1.3,
                3 => baseFontSize * 1.15,
                _ => baseFontSize * 1.05
            };

            var para = new Paragraph
            {
                FontSize = size,
                FontWeight = FontWeights.Bold,
                Foreground = theme.TextBrush,
                Margin = new Thickness(0, 6, 0, 3)
            };

            AppendInlineFormatting(para.Inlines, text, theme, size);
            return para;
        }

        private static Block CreateHorizontalRule(NoteTheme theme)
        {
            var separator = new Border
            {
                Height = 1.5,
                Background = new SolidColorBrush(Color.FromArgb(90, theme.BorderColor.R, theme.BorderColor.G, theme.BorderColor.B)),
                Margin = new Thickness(4, 6, 4, 6)
            };
            return new BlockUIContainer(separator) { Margin = new Thickness(0) };
        }

        private static Block CreateBlockquote(string text, NoteTheme theme, double baseFontSize)
        {
            var border = new Border
            {
                BorderBrush = theme.AccentBrush,
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(8, 2, 4, 2),
                Margin = new Thickness(2, 4, 2, 4)
            };

            var p = new Paragraph
            {
                FontStyle = FontStyles.Italic,
                Foreground = theme.SubduedTextBrush,
                FontSize = baseFontSize,
                Margin = new Thickness(0)
            };

            AppendInlineFormatting(p.Inlines, text, theme, baseFontSize);
            border.Child = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Inlines = { }
            };

            // Convert inlines to TextBlock
            var tb = (TextBlock)border.Child;
            while (p.Inlines.Count > 0)
            {
                var inline = p.Inlines.FirstInline;
                p.Inlines.Remove(inline);
                tb.Inlines.Add(inline);
            }
            tb.FontStyle = FontStyles.Italic;
            tb.Foreground = theme.SubduedTextBrush;

            return new BlockUIContainer(border);
        }

        private static Block CreateCodeBlock(string code, NoteTheme theme, double baseFontSize)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(35, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 4, 0, 4)
            };

            var tb = new TextBlock
            {
                Text = code,
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = Math.Max(11, baseFontSize * 0.9),
                Foreground = theme.TextBrush,
                TextWrapping = TextWrapping.Wrap
            };

            border.Child = tb;
            return new BlockUIContainer(border);
        }

        private static Block CreateTaskItem(
            string text, 
            bool isChecked, 
            int lineIndex, 
            NoteTheme theme, 
            double baseFontSize,
            Action<int, bool>? onTaskToggled)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            var cb = new CheckBox
            {
                IsChecked = isChecked,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            cb.MouseLeftButtonDown += (s, e) => e.Handled = true;
            cb.MouseLeftButtonUp += (s, e) => e.Handled = true;

            if (onTaskToggled != null)
            {
                cb.Click += (s, e) =>
                {
                    e.Handled = true;
                    onTaskToggled(lineIndex, cb.IsChecked == true);
                };
            }

            var tb = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = baseFontSize,
                Foreground = isChecked ? theme.SubduedTextBrush : theme.TextBrush,
                TextWrapping = TextWrapping.Wrap
            };

            if (isChecked)
            {
                tb.TextDecorations = TextDecorations.Strikethrough;
            }

            AppendInlineFormatting(tb.Inlines, text, theme, baseFontSize);

            panel.Children.Add(cb);
            panel.Children.Add(tb);

            return new BlockUIContainer(panel) { Margin = new Thickness(0, 1, 0, 1) };
        }

        public static void AppendInlineFormatting(InlineCollection inlines, string text, NoteTheme theme, double baseFontSize)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Pattern for:
            // 1. Links: [text](url)
            // 2. Bold/Italic: ***text***
            // 3. Bold: **text** or __text__
            // 4. Italic: *text* or _text_
            // 5. Strikethrough: ~~text~~
            // 6. Inline code: `text`
            string pattern = @"(\[.+?\]\(.+?\))|(\*\*\*.+?\*\*\*)|(\*\*.+?\*\*)|(__.+?__)|(\*.+?\*)|(_.+?_)|(~~.+?~~)|(`.+?`)";
            var matches = Regex.Matches(text, pattern);

            int lastIndex = 0;
            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                }

                string val = match.Value;

                // 1. Link: [text](url)
                if (val.StartsWith("[") && val.Contains("](") && val.EndsWith(")"))
                {
                    var linkMatch = Regex.Match(val, @"^\[(.*?)\]\((.*?)\)$");
                    if (linkMatch.Success)
                    {
                        string linkText = linkMatch.Groups[1].Value;
                        string linkUrl = linkMatch.Groups[2].Value;

                        var hyperlink = new Hyperlink(new Run(linkText))
                        {
                            Foreground = theme.AccentBrush,
                            NavigateUri = Uri.TryCreate(linkUrl, UriKind.Absolute, out var uri) ? uri : null,
                            ToolTip = linkUrl,
                            Cursor = System.Windows.Input.Cursors.Hand
                        };
                        hyperlink.RequestNavigate += (s, e) =>
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                            }
                            catch { }
                        };
                        inlines.Add(hyperlink);
                    }
                }
                // 2. Bold + Italic: ***text***
                else if (val.StartsWith("***") && val.EndsWith("***") && val.Length >= 6)
                {
                    string inner = val.Substring(3, val.Length - 6);
                    inlines.Add(new Run(inner) { FontWeight = FontWeights.Bold, FontStyle = FontStyles.Italic });
                }
                // 3. Bold: **text** or __text__
                else if ((val.StartsWith("**") && val.EndsWith("**") && val.Length >= 4) ||
                         (val.StartsWith("__") && val.EndsWith("__") && val.Length >= 4))
                {
                    string inner = val.Substring(2, val.Length - 4);
                    inlines.Add(new Run(inner) { FontWeight = FontWeights.Bold });
                }
                // 4. Strikethrough: ~~text~~
                else if (val.StartsWith("~~") && val.EndsWith("~~") && val.Length >= 4)
                {
                    string inner = val.Substring(2, val.Length - 4);
                    inlines.Add(new Run(inner) { TextDecorations = TextDecorations.Strikethrough });
                }
                // 5. Inline Code: `text`
                else if (val.StartsWith("`") && val.EndsWith("`") && val.Length >= 2)
                {
                    string inner = val.Substring(1, val.Length - 2);
                    var codeRun = new Run(inner)
                    {
                        FontFamily = new FontFamily("Consolas, Courier New"),
                        Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0))
                    };
                    inlines.Add(codeRun);
                }
                // 6. Italic: *text* or _text_
                else if ((val.StartsWith("*") && val.EndsWith("*") && val.Length >= 2) ||
                         (val.StartsWith("_") && val.EndsWith("_") && val.Length >= 2))
                {
                    string inner = val.Substring(1, val.Length - 2);
                    inlines.Add(new Run(inner) { FontStyle = FontStyles.Italic });
                }
                else
                {
                    inlines.Add(new Run(val));
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                inlines.Add(new Run(text.Substring(lastIndex)));
            }
        }
    }
}
