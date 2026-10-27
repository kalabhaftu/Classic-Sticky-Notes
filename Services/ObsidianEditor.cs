using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClassicStickyNotes.Services
{
    public class LineItem
    {
        public string RawText { get; set; } = string.Empty;
        public UIElement? DisplayView { get; set; }
        public TextBox? EditBox { get; set; }
        public Grid? Container { get; set; }
    }

    public class ObsidianEditor
    {
        private readonly ScrollViewer _scrollViewer;
        private readonly StackPanel _stackPanel;
        private readonly List<LineItem> _lines = new List<LineItem>();
        private int _activeLineIndex = -1;

        public NoteTheme Theme { get; set; }
        public string FontFamilyName { get; set; } = "Segoe UI";
        public double BaseFontSize { get; set; } = 15.0;

        public event Action? DocumentChanged;

        public ObsidianEditor(ScrollViewer scrollViewer, StackPanel stackPanel, NoteTheme theme)
        {
            _scrollViewer = scrollViewer;
            _stackPanel = stackPanel;
            Theme = theme;

            // Clicking in empty space below lines focuses or creates the last line
            _scrollViewer.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    if (e.OriginalSource == _scrollViewer || e.OriginalSource == _stackPanel)
                    {
                        if (_lines.Count == 0)
                        {
                            AddLine("");
                            ActivateLine(0);
                        }
                        else
                        {
                            var last = _lines[_lines.Count - 1];
                            if (!string.IsNullOrWhiteSpace(last.RawText))
                            {
                                AddLine("");
                                ActivateLine(_lines.Count - 1);
                            }
                            else
                            {
                                ActivateLine(_lines.Count - 1);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("ScrollViewer.MouseLeftButtonDown", ex);
                }
            };
        }

        public string GetFullDocumentText()
        {
            try
            {
                if (_activeLineIndex >= 0 && _activeLineIndex < _lines.Count)
                {
                    var active = _lines[_activeLineIndex];
                    if (active.EditBox != null)
                    {
                        active.RawText = active.EditBox.Text;
                    }
                }
                return string.Join(Environment.NewLine, _lines.Select(l => l.RawText));
            }
            catch (Exception ex)
            {
                Logger.LogError("GetFullDocumentText", ex);
                return string.Empty;
            }
        }

        public void LoadDocument(string markdown)
        {
            try
            {
                _activeLineIndex = -1;
                _lines.Clear();
                _stackPanel.Children.Clear();

                var rawLines = string.IsNullOrEmpty(markdown) 
                    ? new[] { "" } 
                    : markdown.Replace("\r\n", "\n").Split('\n');

                foreach (var rawLine in rawLines)
                {
                    _lines.Add(new LineItem { RawText = rawLine });
                }

                RebuildUI();

                // If empty, activate first line immediately so user can type
                if (_lines.Count == 1 && string.IsNullOrEmpty(_lines[0].RawText))
                {
                    ActivateLine(0);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("LoadDocument", ex);
            }
        }

        public void ApplyThemeAndFont(NoteTheme theme, string fontFamilyName, double fontSize)
        {
            try
            {
                Theme = theme;
                FontFamilyName = fontFamilyName;
                BaseFontSize = fontSize;

                for (int i = 0; i < _lines.Count; i++)
                {
                    var item = _lines[i];
                    if (i != _activeLineIndex)
                    {
                        RenderInactiveLine(item, i);
                        if (item.Container != null)
                        {
                            item.Container.Children.Clear();
                            if (item.DisplayView != null) item.Container.Children.Add(item.DisplayView);
                            if (item.EditBox != null) item.Container.Children.Add(item.EditBox);
                        }
                    }
                    else if (item.EditBox != null)
                    {
                        item.EditBox.FontFamily = new FontFamily(fontFamilyName);
                        item.EditBox.Foreground = theme.TextBrush;
                        item.EditBox.CaretBrush = theme.TextBrush;
                        StyleActiveEditBox(item.EditBox, item.RawText);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ApplyThemeAndFont", ex);
            }
        }

        public void AddLine(string text)
        {
            var item = new LineItem { RawText = text };
            _lines.Add(item);
            RebuildUI();
            NotifyChanged();
        }

        private void RebuildUI()
        {
            try
            {
                _stackPanel.Children.Clear();

                for (int i = 0; i < _lines.Count; i++)
                {
                    var item = _lines[i];
                    int index = i;

                    RenderInactiveLine(item, index);

                    var tb = new TextBox
                    {
                        Text = item.RawText,
                        AcceptsReturn = false,
                        AcceptsTab = true,
                        TextWrapping = TextWrapping.Wrap,
                        BorderThickness = new Thickness(0),
                        Background = Brushes.Transparent,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                        FontFamily = new FontFamily(FontFamilyName),
                        Foreground = Theme.TextBrush,
                        CaretBrush = Theme.TextBrush,
                        SelectionBrush = Theme.SelectionBrush,
                        Visibility = (index == _activeLineIndex) ? Visibility.Visible : Visibility.Collapsed
                    };

                    StyleActiveEditBox(tb, item.RawText);

                    tb.PreviewKeyDown += EditBox_PreviewKeyDown;
                    tb.TextChanged += (s, e) =>
                    {
                        try
                        {
                            item.RawText = tb.Text;
                            StyleActiveEditBox(tb, tb.Text);
                            NotifyChanged();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("EditBox.TextChanged", ex);
                        }
                    };

                    item.EditBox = tb;

                    var grid = new Grid { Margin = new Thickness(0, 1, 0, 1) };
                    if (item.DisplayView != null)
                    {
                        item.DisplayView.Visibility = (index == _activeLineIndex) ? Visibility.Collapsed : Visibility.Visible;
                        grid.Children.Add(item.DisplayView);
                    }
                    grid.Children.Add(tb);
                    item.Container = grid;

                    _stackPanel.Children.Add(grid);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("RebuildUI", ex);
            }
        }

        public void ActivateLine(int index, int? preferredCaretIndex = null)
        {
            try
            {
                if (index < 0 || index >= _lines.Count) return;

                // 1. Deactivate previous active line
                if (_activeLineIndex >= 0 && _activeLineIndex < _lines.Count && _activeLineIndex != index)
                {
                    var prevLine = _lines[_activeLineIndex];
                    if (prevLine.EditBox != null)
                    {
                        prevLine.RawText = prevLine.EditBox.Text;
                        prevLine.EditBox.Visibility = Visibility.Collapsed;

                        RenderInactiveLine(prevLine, _activeLineIndex);
                        if (prevLine.Container != null)
                        {
                            prevLine.Container.Children.Clear();
                            if (prevLine.DisplayView != null) prevLine.Container.Children.Add(prevLine.DisplayView);
                            if (prevLine.EditBox != null) prevLine.Container.Children.Add(prevLine.EditBox);
                        }
                        if (prevLine.DisplayView != null)
                            prevLine.DisplayView.Visibility = Visibility.Visible;
                    }
                }

                // 2. Activate target line
                _activeLineIndex = index;
                var activeLine = _lines[index];

                if (activeLine.DisplayView != null)
                    activeLine.DisplayView.Visibility = Visibility.Collapsed;

                if (activeLine.EditBox != null)
                {
                    activeLine.EditBox.Text = activeLine.RawText;
                    activeLine.EditBox.Visibility = Visibility.Visible;
                    StyleActiveEditBox(activeLine.EditBox, activeLine.RawText);

                    _scrollViewer.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    {
                        try
                        {
                            activeLine.EditBox.Focus();
                            if (preferredCaretIndex.HasValue)
                            {
                                activeLine.EditBox.CaretIndex = Math.Min(preferredCaretIndex.Value, activeLine.EditBox.Text.Length);
                            }
                            else
                            {
                                activeLine.EditBox.CaretIndex = activeLine.EditBox.Text.Length;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("ActivateLine.DispatcherFocus", ex);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"ActivateLine({index})", ex);
            }
        }

        private void EditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                var tb = (TextBox)sender;
                int lineIdx = _lines.FindIndex(l => l.EditBox == tb);
                if (lineIdx < 0) return;

                // Enter key: Split or continue list
                if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
                {
                    e.Handled = true;
                    HandleEnter(lineIdx, tb);
                    return;
                }

                // Backspace at position 0: Merge with previous line
                if (e.Key == Key.Back && Keyboard.Modifiers == ModifierKeys.None)
                {
                    if (tb.SelectionLength == 0 && tb.CaretIndex == 0 && lineIdx > 0)
                    {
                        e.Handled = true;
                        HandleBackspaceAtStart(lineIdx, tb);
                        return;
                    }
                }

                // Delete at end: Merge with next line
                if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None)
                {
                    if (tb.SelectionLength == 0 && tb.CaretIndex == tb.Text.Length && lineIdx < _lines.Count - 1)
                    {
                        e.Handled = true;
                        HandleDeleteAtEnd(lineIdx, tb);
                        return;
                    }
                }

                // Up Arrow
                if (e.Key == Key.Up && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Alt))
                {
                    int visualLine = tb.GetLineIndexFromCharacterIndex(tb.CaretIndex);
                    if (visualLine == 0 && lineIdx > 0)
                    {
                        e.Handled = true;
                        int col = tb.CaretIndex;
                        ActivateLine(lineIdx - 1, col);
                        return;
                    }
                }

                // Down Arrow
                if (e.Key == Key.Down && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Alt))
                {
                    int visualLine = tb.GetLineIndexFromCharacterIndex(tb.CaretIndex);
                    if (visualLine == tb.LineCount - 1 && lineIdx < _lines.Count - 1)
                    {
                        e.Handled = true;
                        int col = tb.CaretIndex;
                        ActivateLine(lineIdx + 1, col);
                        return;
                    }
                }

                // Markdown shortcut wrappers
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (e.Key == Key.B)
                    {
                        e.Handled = true;
                        WrapSelection(tb, "**", "**");
                        return;
                    }
                    if (e.Key == Key.I)
                    {
                        e.Handled = true;
                        WrapSelection(tb, "*", "*");
                        return;
                    }
                    if (e.Key == Key.K)
                    {
                        e.Handled = true;
                        WrapSelection(tb, "[", "](https://)");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("EditBox_PreviewKeyDown", ex);
            }
        }

        private void HandleEnter(int lineIdx, TextBox tb)
        {
            string currentText = tb.Text;
            int caret = tb.CaretIndex;

            string beforeCaret = currentText.Substring(0, caret);
            string afterCaret = currentText.Substring(caret);

            // 1. Task Item: - [ ] or - [x]
            var taskMatch = Regex.Match(beforeCaret, @"^(\s*[-*+]\s+\[[ xX]\]\s*)(.*)");
            if (taskMatch.Success)
            {
                string prefix = taskMatch.Groups[1].Value;
                string remainder = taskMatch.Groups[2].Value;

                // If empty task item, pressing Enter clears the prefix
                if (string.IsNullOrWhiteSpace(remainder) && string.IsNullOrWhiteSpace(afterCaret))
                {
                    tb.Text = "";
                    _lines[lineIdx].RawText = "";
                    RebuildUI();
                    ActivateLine(lineIdx, 0);
                    NotifyChanged();
                    return;
                }

                string nextPrefix = Regex.Replace(prefix, @"\[[xX]\]", "[ ]");
                _lines[lineIdx].RawText = beforeCaret;
                tb.Text = beforeCaret;

                var newLine = new LineItem { RawText = nextPrefix + afterCaret };
                _lines.Insert(lineIdx + 1, newLine);

                RebuildUI();
                ActivateLine(lineIdx + 1, nextPrefix.Length);
                NotifyChanged();
                return;
            }

            // 2. Bullet Item: - or *
            var bulletMatch = Regex.Match(beforeCaret, @"^(\s*[-*+]\s+)(.*)");
            if (bulletMatch.Success)
            {
                string prefix = bulletMatch.Groups[1].Value;
                string remainder = bulletMatch.Groups[2].Value;

                if (string.IsNullOrWhiteSpace(remainder) && string.IsNullOrWhiteSpace(afterCaret))
                {
                    tb.Text = "";
                    _lines[lineIdx].RawText = "";
                    RebuildUI();
                    ActivateLine(lineIdx, 0);
                    NotifyChanged();
                    return;
                }

                _lines[lineIdx].RawText = beforeCaret;
                tb.Text = beforeCaret;

                var newLine = new LineItem { RawText = prefix + afterCaret };
                _lines.Insert(lineIdx + 1, newLine);

                RebuildUI();
                ActivateLine(lineIdx + 1, prefix.Length);
                NotifyChanged();
                return;
            }

            // 3. Numbered Item: 1.
            var numMatch = Regex.Match(beforeCaret, @"^(\s*)(\d+)(\.\s+)(.*)");
            if (numMatch.Success)
            {
                string indent = numMatch.Groups[1].Value;
                int num = int.Parse(numMatch.Groups[2].Value);
                string remainder = numMatch.Groups[4].Value;

                if (string.IsNullOrWhiteSpace(remainder) && string.IsNullOrWhiteSpace(afterCaret))
                {
                    tb.Text = "";
                    _lines[lineIdx].RawText = "";
                    RebuildUI();
                    ActivateLine(lineIdx, 0);
                    NotifyChanged();
                    return;
                }

                string nextPrefix = $"{indent}{num + 1}. ";
                _lines[lineIdx].RawText = beforeCaret;
                tb.Text = beforeCaret;

                var newLine = new LineItem { RawText = nextPrefix + afterCaret };
                _lines.Insert(lineIdx + 1, newLine);

                RebuildUI();
                ActivateLine(lineIdx + 1, nextPrefix.Length);
                NotifyChanged();
                return;
            }

            // 4. Standard Line Split
            _lines[lineIdx].RawText = beforeCaret;
            tb.Text = beforeCaret;

            var plainNewLine = new LineItem { RawText = afterCaret };
            _lines.Insert(lineIdx + 1, plainNewLine);

            RebuildUI();
            ActivateLine(lineIdx + 1, 0);
            NotifyChanged();
        }

        private void HandleBackspaceAtStart(int lineIdx, TextBox tb)
        {
            if (lineIdx <= 0) return;

            string currentText = tb.Text;
            string prevText = _lines[lineIdx - 1].RawText;
            int mergePoint = prevText.Length;

            _lines[lineIdx - 1].RawText = prevText + currentText;
            _lines.RemoveAt(lineIdx);

            RebuildUI();
            ActivateLine(lineIdx - 1, mergePoint);
            NotifyChanged();
        }

        private void HandleDeleteAtEnd(int lineIdx, TextBox tb)
        {
            if (lineIdx >= _lines.Count - 1) return;

            string currentText = tb.Text;
            string nextText = _lines[lineIdx + 1].RawText;

            _lines[lineIdx].RawText = currentText + nextText;
            _lines.RemoveAt(lineIdx + 1);

            RebuildUI();
            ActivateLine(lineIdx, currentText.Length);
            NotifyChanged();
        }

        private void WrapSelection(TextBox tb, string prefix, string suffix)
        {
            int start = tb.SelectionStart;
            int len = tb.SelectionLength;
            string selected = tb.SelectedText;

            if (string.IsNullOrEmpty(selected))
            {
                tb.SelectedText = prefix + "text" + suffix;
                tb.Select(start + prefix.Length, 4);
            }
            else
            {
                tb.SelectedText = prefix + selected + suffix;
                tb.Select(start + prefix.Length, len);
            }
        }

        private void StyleActiveEditBox(TextBox tb, string text)
        {
            string trimmed = text.TrimStart();
            if (trimmed.StartsWith("# "))
            {
                tb.FontSize = BaseFontSize * 1.5;
                tb.FontWeight = FontWeights.Bold;
            }
            else if (trimmed.StartsWith("## "))
            {
                tb.FontSize = BaseFontSize * 1.3;
                tb.FontWeight = FontWeights.Bold;
            }
            else if (trimmed.StartsWith("### "))
            {
                tb.FontSize = BaseFontSize * 1.15;
                tb.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                tb.FontSize = BaseFontSize;
                tb.FontWeight = FontWeights.Normal;
            }
        }

        private void RenderInactiveLine(LineItem item, int index)
        {
            string text = item.RawText;
            string trimmed = text.Trim();

            // 1. Horizontal Rule (---, ***)
            if (trimmed == "---" || trimmed == "***" || trimmed == "___")
            {
                var rule = new Border
                {
                    Height = 1.5,
                    Background = new SolidColorBrush(Color.FromArgb(90, Theme.BorderColor.R, Theme.BorderColor.G, Theme.BorderColor.B)),
                    Margin = new Thickness(2, 6, 2, 6),
                    Cursor = Cursors.IBeam
                };
                AttachClickToActivate(rule, item);
                item.DisplayView = rule;
                return;
            }

            // 2. Headings (# H1, ## H2, ### H3)
            if (trimmed.StartsWith("#"))
            {
                var match = Regex.Match(trimmed, @"^(#{1,6})\s+(.*)$");
                if (match.Success)
                {
                    int level = match.Groups[1].Length;
                    string headingText = match.Groups[2].Value;
                    double size = level switch
                    {
                        1 => BaseFontSize * 1.5,
                        2 => BaseFontSize * 1.3,
                        3 => BaseFontSize * 1.15,
                        _ => BaseFontSize * 1.05
                    };

                    var tb = new TextBlock
                    {
                        FontSize = size,
                        FontWeight = FontWeights.Bold,
                        Foreground = Theme.TextBrush,
                        FontFamily = new FontFamily(FontFamilyName),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 3, 0, 2),
                        Cursor = Cursors.IBeam
                    };
                    MarkdownRenderer.AppendInlineFormatting(tb.Inlines, headingText, Theme, size);
                    AttachClickToActivate(tb, item);
                    item.DisplayView = tb;
                    return;
                }
            }

            // 3. Task list item (- [ ] or - [x])
            var taskMatch = Regex.Match(trimmed, @"^[-*+]\s+\[([ xX])\]\s*(.*)$");
            if (taskMatch.Success)
            {
                bool isChecked = taskMatch.Groups[1].Value.Trim().Length > 0;
                string itemContent = taskMatch.Groups[2].Value;

                var dock = new DockPanel
                {
                    LastChildFill = true,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var cb = new CheckBox
                {
                    IsChecked = isChecked,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 3, 6, 0),
                    Cursor = Cursors.Hand
                };
                DockPanel.SetDock(cb, Dock.Left);

                cb.Click += (s, e) =>
                {
                    e.Handled = true;
                    try
                    {
                        bool nowChecked = cb.IsChecked == true;
                        if (nowChecked)
                        {
                            item.RawText = Regex.Replace(item.RawText, @"^([ \t]*[-*+]\s+\[) \]", "${1}x]");
                        }
                        else
                        {
                            item.RawText = Regex.Replace(item.RawText, @"^([ \t]*[-*+]\s+\[)[xX]\]", "${1} ]");
                        }

                        int currIdx = _lines.IndexOf(item);
                        if (currIdx >= 0)
                        {
                            RenderInactiveLine(item, currIdx);
                            if (item.Container != null)
                            {
                                item.Container.Children.Clear();
                                if (item.DisplayView != null) item.Container.Children.Add(item.DisplayView);
                                if (item.EditBox != null) item.Container.Children.Add(item.EditBox);
                            }
                        }
                        NotifyChanged();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("CheckBox.Click", ex);
                    }
                };

                var tb = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    FontSize = BaseFontSize,
                    FontFamily = new FontFamily(FontFamilyName),
                    Foreground = isChecked ? Theme.SubduedTextBrush : Theme.TextBrush,
                    TextWrapping = TextWrapping.Wrap,
                    Cursor = Cursors.IBeam
                };

                if (isChecked)
                {
                    tb.TextDecorations = TextDecorations.Strikethrough;
                }

                MarkdownRenderer.AppendInlineFormatting(tb.Inlines, itemContent, Theme, BaseFontSize);
                AttachClickToActivate(tb, item);

                dock.Children.Add(cb);
                dock.Children.Add(tb);

                item.DisplayView = dock;
                return;
            }

            // 4. Bullet list (- or *)
            if (Regex.IsMatch(trimmed, @"^[-*+]\s+"))
            {
                string bulletContent = Regex.Replace(trimmed, @"^[-*+]\s+", "");
                var dock = new DockPanel
                {
                    LastChildFill = true,
                    Margin = new Thickness(4, 1, 0, 1),
                    Cursor = Cursors.IBeam
                };

                var bulletSymbol = new TextBlock
                {
                    Text = "• ",
                    FontWeight = FontWeights.Bold,
                    FontSize = BaseFontSize * 1.1,
                    Foreground = Theme.AccentBrush,
                    Margin = new Thickness(0, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };
                DockPanel.SetDock(bulletSymbol, Dock.Left);

                var tb = new TextBlock
                {
                    FontSize = BaseFontSize,
                    FontFamily = new FontFamily(FontFamilyName),
                    Foreground = Theme.TextBrush,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Top
                };
                MarkdownRenderer.AppendInlineFormatting(tb.Inlines, bulletContent, Theme, BaseFontSize);

                dock.Children.Add(bulletSymbol);
                dock.Children.Add(tb);

                AttachClickToActivate(dock, item);
                item.DisplayView = dock;
                return;
            }

            // 4b. Numbered list (1. item)
            var numMatch = Regex.Match(trimmed, @"^(\d+\.\s+)(.*)$");
            if (numMatch.Success)
            {
                string numPrefix = numMatch.Groups[1].Value;
                string numContent = numMatch.Groups[2].Value;

                var dock = new DockPanel
                {
                    LastChildFill = true,
                    Margin = new Thickness(4, 1, 0, 1),
                    Cursor = Cursors.IBeam
                };

                var numSymbol = new TextBlock
                {
                    Text = numPrefix,
                    FontWeight = FontWeights.Bold,
                    FontSize = BaseFontSize,
                    FontFamily = new FontFamily(FontFamilyName),
                    Foreground = Theme.AccentBrush,
                    Margin = new Thickness(0, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };
                DockPanel.SetDock(numSymbol, Dock.Left);

                var tb = new TextBlock
                {
                    FontSize = BaseFontSize,
                    FontFamily = new FontFamily(FontFamilyName),
                    Foreground = Theme.TextBrush,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Top
                };
                MarkdownRenderer.AppendInlineFormatting(tb.Inlines, numContent, Theme, BaseFontSize);

                dock.Children.Add(numSymbol);
                dock.Children.Add(tb);

                AttachClickToActivate(dock, item);
                item.DisplayView = dock;
                return;
            }

            // 5. Blockquote (> quote)
            if (trimmed.StartsWith(">"))
            {
                string quoteContent = trimmed.Length > 1 && trimmed[1] == ' ' ? trimmed.Substring(2) : trimmed.Substring(1);
                var border = new Border
                {
                    BorderBrush = Theme.AccentBrush,
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(8, 2, 4, 2),
                    Margin = new Thickness(2, 2, 2, 2),
                    Cursor = Cursors.IBeam
                };

                var tb = new TextBlock
                {
                    FontSize = BaseFontSize,
                    FontFamily = new FontFamily(FontFamilyName),
                    FontStyle = FontStyles.Italic,
                    Foreground = Theme.SubduedTextBrush,
                    TextWrapping = TextWrapping.Wrap
                };
                MarkdownRenderer.AppendInlineFormatting(tb.Inlines, quoteContent, Theme, BaseFontSize);
                border.Child = tb;

                AttachClickToActivate(border, item);
                item.DisplayView = border;
                return;
            }

            // 6. Blank line
            if (string.IsNullOrEmpty(text))
            {
                var spacer = new TextBlock
                {
                    Height = BaseFontSize * 1.25,
                    Background = Brushes.Transparent,
                    Cursor = Cursors.IBeam
                };
                AttachClickToActivate(spacer, item);
                item.DisplayView = spacer;
                return;
            }

            // 7. Standard Text line
            var pText = new TextBlock
            {
                FontSize = BaseFontSize,
                FontFamily = new FontFamily(FontFamilyName),
                Foreground = Theme.TextBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 1, 0, 1),
                Cursor = Cursors.IBeam
            };
            MarkdownRenderer.AppendInlineFormatting(pText.Inlines, text, Theme, BaseFontSize);
            AttachClickToActivate(pText, item);
            item.DisplayView = pText;
        }

        private void AttachClickToActivate(UIElement element, LineItem item)
        {
            element.MouseLeftButtonUp += (s, e) =>
            {
                try
                {
                    if (IsInsideCheckBox(e.OriginalSource)) return;

                    int currentIdx = _lines.IndexOf(item);
                    if (currentIdx >= 0)
                    {
                        ActivateLine(currentIdx);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("AttachClickToActivate.MouseLeftButtonUp", ex);
                }
            };
        }

        private static bool IsInsideCheckBox(object? source)
        {
            if (source is not DependencyObject dep) return false;
            DependencyObject? curr = dep;
            while (curr != null)
            {
                if (curr is CheckBox) return true;
                if (curr is Visual v)
                    curr = VisualTreeHelper.GetParent(v);
                else if (curr is FrameworkContentElement fce)
                    curr = fce.Parent;
                else
                    break;
            }
            return false;
        }

        private void NotifyChanged()
        {
            DocumentChanged?.Invoke();
        }
    }
}
