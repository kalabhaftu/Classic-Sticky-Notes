using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClassicStickyNotes.Services
{
    public class DocumentLine
    {
        public string RawText { get; set; } = string.Empty;
        public UIElement? DisplayElement { get; set; }
        public TextBox? EditBox { get; set; }
        public Border? Container { get; set; }
    }

    public class LiveMarkdownEditor
    {
        private readonly ScrollViewer _scrollViewer;
        private readonly StackPanel _stackPanel;
        private readonly List<DocumentLine> _lines = new List<DocumentLine>();
        private int _activeLineIndex = -1;

        // Selection state
        private bool _isAllSelected = false;
        private int _selectionStartLine = -1;
        private int _selectionEndLine = -1;
        private int _mouseDragStartLine = -1;

        public NoteTheme Theme { get; set; }
        public string FontFamilyName { get; set; } = "Segoe UI";
        public double BaseFontSize { get; set; } = 15.0;

        public event Action? DocumentChanged;

        public LiveMarkdownEditor(ScrollViewer scrollViewer, StackPanel stackPanel, NoteTheme theme)
        {
            _scrollViewer = scrollViewer;
            _stackPanel = stackPanel;
            Theme = theme;

            // Clicking in the empty area below lines focuses or creates the bottom line
            _scrollViewer.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    if (e.OriginalSource == _scrollViewer || e.OriginalSource == _stackPanel)
                    {
                        ClearSelection();

                        if (_lines.Count == 0)
                        {
                            _lines.Add(new DocumentLine { RawText = "" });
                            RebuildUI();
                            ActivateLine(0, 0);
                            NotifyChanged();
                        }
                        else
                        {
                            var last = _lines[_lines.Count - 1];
                            if (!string.IsNullOrWhiteSpace(last.RawText))
                            {
                                _lines.Add(new DocumentLine { RawText = "" });
                                RebuildUI();
                                ActivateLine(_lines.Count - 1, 0);
                                NotifyChanged();
                            }
                            else
                            {
                                ActivateLine(_lines.Count - 1, last.RawText.Length);
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
                SyncActiveLineText();
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
                ClearSelection();
                _lines.Clear();
                _stackPanel.Children.Clear();

                var rawLines = string.IsNullOrEmpty(markdown)
                    ? new[] { "" }
                    : markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

                foreach (var line in rawLines)
                {
                    _lines.Add(new DocumentLine { RawText = line });
                }

                RebuildUI();

                // If empty note, immediately activate first line so user can start typing
                if (_lines.Count == 1 && string.IsNullOrEmpty(_lines[0].RawText))
                {
                    ActivateLine(0, 0);
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
                            var grid = item.Container.Child as Grid;
                            if (grid != null)
                            {
                                grid.Children.Clear();
                                if (item.DisplayElement != null) grid.Children.Add(item.DisplayElement);
                                if (item.EditBox != null) grid.Children.Add(item.EditBox);
                            }
                        }
                    }
                    else if (item.EditBox != null)
                    {
                        item.EditBox.FontFamily = new FontFamily(fontFamilyName);
                        item.EditBox.Foreground = theme.TextBrush;
                        item.EditBox.CaretBrush = theme.TextBrush;
                        item.EditBox.SelectionBrush = theme.SelectionBrush;
                        StyleActiveEditBox(item.EditBox, item.RawText);
                    }
                }

                UpdateSelectionHighlight();
            }
            catch (Exception ex)
            {
                Logger.LogError("ApplyThemeAndFont", ex);
            }
        }

        private void NotifyChanged()
        {
            DocumentChanged?.Invoke();
        }

        private void SyncActiveLineText()
        {
            if (_activeLineIndex >= 0 && _activeLineIndex < _lines.Count)
            {
                var active = _lines[_activeLineIndex];
                if (active.EditBox != null)
                {
                    active.RawText = active.EditBox.Text;
                }
            }
        }

        // ==========================================
        // UI REBUILD & LINE RENDERING
        // ==========================================

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

                    tb.PreviewKeyDown += (s, e) => EditBox_PreviewKeyDown(tb, index, e);
                    tb.TextChanged += (s, e) =>
                    {
                        try
                        {
                            item.RawText = tb.Text;
                            StyleActiveEditBox(tb, tb.Text);
                            NotifyChanged();
                        }
                        catch { }
                    };

                    tb.ContextMenuOpening += (s, e) =>
                    {
                        tb.ContextMenu = CreateLineContextMenu(index, tb);
                    };

                    item.EditBox = tb;

                    var grid = new Grid();
                    if (item.DisplayElement != null)
                    {
                        item.DisplayElement.Visibility = (index == _activeLineIndex) ? Visibility.Collapsed : Visibility.Visible;
                        grid.Children.Add(item.DisplayElement);
                    }
                    grid.Children.Add(tb);

                    var container = new Border
                    {
                        Background = Brushes.Transparent,
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(2, 1, 2, 1),
                        Margin = new Thickness(0, 1, 0, 1),
                        Child = grid
                    };

                    // Handle mouse selection across lines
                    container.MouseLeftButtonDown += (s, e) =>
                    {
                        _mouseDragStartLine = index;
                        if (index != _activeLineIndex)
                        {
                            ClearSelection();
                            ActivateLine(index);
                        }
                    };

                    container.MouseMove += (s, e) =>
                    {
                        if (e.LeftButton == MouseButtonState.Pressed && _mouseDragStartLine >= 0 && _mouseDragStartLine != index)
                        {
                            _selectionStartLine = _mouseDragStartLine;
                            _selectionEndLine = index;
                            _isAllSelected = false;
                            UpdateSelectionHighlight();
                        }
                    };

                    container.MouseLeftButtonUp += (s, e) =>
                    {
                        _mouseDragStartLine = -1;
                    };

                    item.Container = container;
                    _stackPanel.Children.Add(container);
                }

                UpdateSelectionHighlight();
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

                // Deactivate previous active line
                if (_activeLineIndex >= 0 && _activeLineIndex < _lines.Count && _activeLineIndex != index)
                {
                    var prev = _lines[_activeLineIndex];
                    if (prev.EditBox != null)
                    {
                        prev.RawText = prev.EditBox.Text;
                        prev.EditBox.Visibility = Visibility.Collapsed;

                        RenderInactiveLine(prev, _activeLineIndex);
                        if (prev.Container?.Child is Grid prevGrid)
                        {
                            prevGrid.Children.Clear();
                            if (prev.DisplayElement != null) prevGrid.Children.Add(prev.DisplayElement);
                            if (prev.EditBox != null) prevGrid.Children.Add(prev.EditBox);
                        }
                        if (prev.DisplayElement != null)
                        {
                            prev.DisplayElement.Visibility = Visibility.Visible;
                        }
                    }
                }

                // Activate new target line
                _activeLineIndex = index;
                var active = _lines[index];

                if (active.DisplayElement != null)
                {
                    active.DisplayElement.Visibility = Visibility.Collapsed;
                }

                if (active.EditBox != null)
                {
                    active.EditBox.Text = active.RawText;
                    active.EditBox.Visibility = Visibility.Visible;
                    StyleActiveEditBox(active.EditBox, active.RawText);

                    _scrollViewer.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    {
                        try
                        {
                            active.EditBox.Focus();
                            if (preferredCaretIndex.HasValue)
                            {
                                active.EditBox.CaretIndex = Math.Min(preferredCaretIndex.Value, active.EditBox.Text.Length);
                            }
                            else
                            {
                                active.EditBox.CaretIndex = active.EditBox.Text.Length;
                            }
                        }
                        catch { }
                    }));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"ActivateLine({index})", ex);
            }
        }

        // ==========================================
        // RENDER INACTIVE LINE FORMATTING
        // ==========================================

        private void RenderInactiveLine(DocumentLine item, int index)
        {
            string raw = item.RawText;
            string trimmed = raw.Trim();

            // 1. Empty Line
            if (string.IsNullOrEmpty(trimmed))
            {
                var spacer = new Border
                {
                    Height = Math.Max(18, BaseFontSize * 1.25),
                    Background = Brushes.Transparent,
                    Cursor = Cursors.IBeam
                };
                spacer.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ClearSelection();
                    ActivateLine(index, 0);
                };
                item.DisplayElement = spacer;
                return;
            }

            // 2. Horizontal Rule (---, ***, ___)
            if (trimmed == "---" || trimmed == "***" || trimmed == "___")
            {
                var lineBorder = new Border
                {
                    Height = 1.5,
                    Background = Theme.BorderBrush,
                    Margin = new Thickness(0, 6, 0, 6),
                    Cursor = Cursors.Hand
                };
                lineBorder.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ClearSelection();
                    ActivateLine(index, raw.Length);
                };
                item.DisplayElement = lineBorder;
                return;
            }

            // 3. Headings (# H1, ## H2, ### H3, #### H4)
            if (trimmed.StartsWith("#"))
            {
                var hMatch = Regex.Match(trimmed, @"^(#{1,6})\s+(.*)$");
                if (hMatch.Success)
                {
                    int level = hMatch.Groups[1].Length;
                    string headingText = hMatch.Groups[2].Value;
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
                        FontFamily = new FontFamily(FontFamilyName),
                        Foreground = Theme.TextBrush,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 3, 0, 2),
                        Cursor = Cursors.IBeam
                    };

                    AppendInlineFormatting(tb.Inlines, headingText, Theme, size);
                    tb.MouseLeftButtonDown += (s, e) =>
                    {
                        e.Handled = true;
                        ClearSelection();
                        ActivateLine(index);
                    };
                    item.DisplayElement = tb;
                    return;
                }
            }

            // 4. Task Checkbox Item (- [ ] or - [x])
            var taskMatch = Regex.Match(trimmed, @"^(\s*[-*+]\s+\[)([ xX])(\]\s*)(.*)$");
            if (taskMatch.Success)
            {
                bool isChecked = taskMatch.Groups[2].Value.Trim().Length > 0;
                string taskText = taskMatch.Groups[4].Value;

                var dock = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 1, 0, 1) };

                var cb = new CheckBox
                {
                    IsChecked = isChecked,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 3, 6, 0),
                    Cursor = Cursors.Hand
                };

                // Clicking the checkbox toggles without activating the edit box!
                cb.Click += (s, e) =>
                {
                    e.Handled = true;
                    bool newChecked = cb.IsChecked == true;
                    item.RawText = Regex.Replace(item.RawText, @"(\[\s*)[ xX](\s*\])", newChecked ? "[x]" : "[ ]");
                    RenderInactiveLine(item, index);
                    if (item.Container?.Child is Grid g)
                    {
                        g.Children.Clear();
                        if (item.DisplayElement != null) g.Children.Add(item.DisplayElement);
                        if (item.EditBox != null) g.Children.Add(item.EditBox);
                    }
                    NotifyChanged();
                };

                DockPanel.SetDock(cb, Dock.Left);
                dock.Children.Add(cb);

                var tb = new TextBlock
                {
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

                AppendInlineFormatting(tb.Inlines, taskText, Theme, BaseFontSize);
                dock.Children.Add(tb);

                dock.MouseLeftButtonDown += (s, e) =>
                {
                    if (IsInsideCheckBox(e.OriginalSource as DependencyObject)) return;
                    e.Handled = true;
                    ClearSelection();
                    ActivateLine(index);
                };

                item.DisplayElement = dock;
                return;
            }

            // 5. Bullet Item (- or *)
            var bulletMatch = Regex.Match(raw, @"^(\s*[-*+]\s+)(.*)$");
            if (bulletMatch.Success)
            {
                string bulletText = bulletMatch.Groups[2].Value;
                var dock = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 1, 0, 1) };

                var bulletSymbol = new TextBlock
                {
                    Text = "• ",
                    FontSize = BaseFontSize * 1.1,
                    FontWeight = FontWeights.Bold,
                    Foreground = Theme.AccentBrush,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(2, 0, 4, 0),
                    Cursor = Cursors.IBeam
                };
                DockPanel.SetDock(bulletSymbol, Dock.Left);
                dock.Children.Add(bulletSymbol);

                var tb = new TextBlock
                {
                    FontSize = BaseFontSize,
                    FontFamily = new FontFamily(FontFamilyName),
                    Foreground = Theme.TextBrush,
                    TextWrapping = TextWrapping.Wrap,
                    Cursor = Cursors.IBeam
                };

                AppendInlineFormatting(tb.Inlines, bulletText, Theme, BaseFontSize);
                dock.Children.Add(tb);

                dock.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ClearSelection();
                    ActivateLine(index);
                };

                item.DisplayElement = dock;
                return;
            }

            // 6. Numbered Item (1. )
            var numMatch = Regex.Match(raw, @"^(\s*\d+\.\s+)(.*)$");
            if (numMatch.Success)
            {
                string prefix = numMatch.Groups[1].Value;
                string numText = numMatch.Groups[2].Value;

                var dock = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 1, 0, 1) };

                var numSymbol = new TextBlock
                {
                    Text = prefix,
                    FontSize = BaseFontSize,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Theme.AccentBrush,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, 4, 0),
                    Cursor = Cursors.IBeam
                };
                DockPanel.SetDock(numSymbol, Dock.Left);
                dock.Children.Add(numSymbol);

                var tb = new TextBlock
                {
                    FontSize = BaseFontSize,
                    FontFamily = new FontFamily(FontFamilyName),
                    Foreground = Theme.TextBrush,
                    TextWrapping = TextWrapping.Wrap,
                    Cursor = Cursors.IBeam
                };

                AppendInlineFormatting(tb.Inlines, numText, Theme, BaseFontSize);
                dock.Children.Add(tb);

                dock.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ClearSelection();
                    ActivateLine(index);
                };

                item.DisplayElement = dock;
                return;
            }

            // 7. Blockquote (> )
            if (trimmed.StartsWith(">"))
            {
                string qText = trimmed.Length > 1 && trimmed[1] == ' ' ? trimmed.Substring(2) : trimmed.Substring(1);
                var border = new Border
                {
                    BorderBrush = Theme.AccentBrush,
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(8, 2, 4, 2),
                    Margin = new Thickness(0, 2, 0, 2),
                    Cursor = Cursors.IBeam
                };

                var tb = new TextBlock
                {
                    FontStyle = FontStyles.Italic,
                    Foreground = Theme.SubduedTextBrush,
                    FontSize = BaseFontSize,
                    FontFamily = new FontFamily(FontFamilyName),
                    TextWrapping = TextWrapping.Wrap
                };

                AppendInlineFormatting(tb.Inlines, qText, Theme, BaseFontSize);
                border.Child = tb;

                border.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ClearSelection();
                    ActivateLine(index);
                };

                item.DisplayElement = border;
                return;
            }

            // 8. Standard Line with Markdown formatting
            var defaultTb = new TextBlock
            {
                FontSize = BaseFontSize,
                FontFamily = new FontFamily(FontFamilyName),
                Foreground = Theme.TextBrush,
                TextWrapping = TextWrapping.Wrap,
                Cursor = Cursors.IBeam
            };

            AppendInlineFormatting(defaultTb.Inlines, raw, Theme, BaseFontSize);

            defaultTb.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                ClearSelection();
                ActivateLine(index);
            };

            item.DisplayElement = defaultTb;
        }

        private static bool IsInsideCheckBox(DependencyObject? element)
        {
            var curr = element;
            while (curr != null)
            {
                if (curr is CheckBox) return true;
                curr = curr is Visual v ? VisualTreeHelper.GetParent(v) : (curr is FrameworkContentElement fce ? fce.Parent : null);
            }
            return false;
        }

        private void StyleActiveEditBox(TextBox tb, string text)
        {
            string trimmed = text.Trim();
            if (trimmed.StartsWith("#"))
            {
                var hMatch = Regex.Match(trimmed, @"^(#{1,6})\s+");
                if (hMatch.Success)
                {
                    int level = hMatch.Groups[1].Length;
                    tb.FontSize = level switch
                    {
                        1 => BaseFontSize * 1.5,
                        2 => BaseFontSize * 1.3,
                        3 => BaseFontSize * 1.15,
                        _ => BaseFontSize * 1.05
                    };
                    tb.FontWeight = FontWeights.Bold;
                    return;
                }
            }

            tb.FontSize = BaseFontSize;
            tb.FontWeight = FontWeights.Normal;
        }

        public static void AppendInlineFormatting(InlineCollection inlines, string text, NoteTheme theme, double baseFontSize)
        {
            if (string.IsNullOrEmpty(text)) return;

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

                // Hyperlink: [text](url)
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
                            Cursor = Cursors.Hand
                        };
                        hyperlink.RequestNavigate += (s, e) =>
                        {
                            try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); } catch { }
                        };
                        inlines.Add(hyperlink);
                    }
                }
                // Bold + Italic: ***text***
                else if (val.StartsWith("***") && val.EndsWith("***") && val.Length >= 6)
                {
                    string inner = val.Substring(3, val.Length - 6);
                    inlines.Add(new Run(inner) { FontWeight = FontWeights.Bold, FontStyle = FontStyles.Italic });
                }
                // Bold: **text**
                else if ((val.StartsWith("**") && val.EndsWith("**") && val.Length >= 4) ||
                         (val.StartsWith("__") && val.EndsWith("__") && val.Length >= 4))
                {
                    string inner = val.Substring(2, val.Length - 4);
                    inlines.Add(new Run(inner) { FontWeight = FontWeights.Bold });
                }
                // Strikethrough: ~~text~~
                else if (val.StartsWith("~~") && val.EndsWith("~~") && val.Length >= 4)
                {
                    string inner = val.Substring(2, val.Length - 4);
                    inlines.Add(new Run(inner) { TextDecorations = TextDecorations.Strikethrough });
                }
                // Inline Code: `text`
                else if (val.StartsWith("`") && val.EndsWith("`") && val.Length >= 2)
                {
                    string inner = val.Substring(1, val.Length - 2);
                    var codeRun = new Run(inner)
                    {
                        FontFamily = new FontFamily("Consolas, Courier New"),
                        Background = new SolidColorBrush(Color.FromArgb(35, 0, 0, 0))
                    };
                    inlines.Add(codeRun);
                }
                // Italic: *text*
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

        // ==========================================
        // KEYBOARD SHORTCUTS & INTERACTION
        // ==========================================

        private void EditBox_PreviewKeyDown(TextBox tb, int lineIdx, KeyEventArgs e)
        {
            try
            {
                // 1. SELECT ALL (Ctrl+A): Selects entire document!
                if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    e.Handled = true;
                    SelectAll();
                    return;
                }

                // 2. COPY (Ctrl+C): Copies whole document or selected lines!
                if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (_isAllSelected || (_selectionStartLine >= 0 && _selectionEndLine >= 0))
                    {
                        e.Handled = true;
                        Clipboard.SetText(GetSelectedText());
                        return;
                    }
                }

                // 3. MULTI-LINE PASTE (Ctrl+V): Pastes any number of lines!
                if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    e.Handled = true;
                    HandlePaste(lineIdx, tb);
                    return;
                }

                // If document is in multi-line selection mode, typing or deleting replaces selection
                if (_isAllSelected || (_selectionStartLine >= 0 && _selectionEndLine >= 0 && _selectionStartLine != _selectionEndLine))
                {
                    if (e.Key == Key.Back || e.Key == Key.Delete)
                    {
                        e.Handled = true;
                        DeleteSelectedLines();
                        return;
                    }
                }

                // 4. ENTER: Line break or smart list continuation
                if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
                {
                    e.Handled = true;
                    HandleEnter(lineIdx, tb);
                    return;
                }

                // 5. BACKSPACE at position 0: Merge with previous line
                if (e.Key == Key.Back && Keyboard.Modifiers == ModifierKeys.None)
                {
                    if (tb.SelectionLength == 0 && tb.CaretIndex == 0 && lineIdx > 0)
                    {
                        e.Handled = true;
                        HandleBackspaceAtStart(lineIdx, tb);
                        return;
                    }
                }

                // 6. DELETE at end: Merge with next line
                if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None)
                {
                    if (tb.SelectionLength == 0 && tb.CaretIndex == tb.Text.Length && lineIdx < _lines.Count - 1)
                    {
                        e.Handled = true;
                        HandleDeleteAtEnd(lineIdx, tb);
                        return;
                    }
                }

                // 7. UP ARROW: Navigate to previous line
                if (e.Key == Key.Up && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift))
                {
                    int visualLine = tb.GetLineIndexFromCharacterIndex(tb.CaretIndex);
                    if (visualLine == 0 && lineIdx > 0)
                    {
                        e.Handled = true;
                        int col = tb.CaretIndex;
                        if (Keyboard.Modifiers == ModifierKeys.Shift)
                        {
                            if (_selectionStartLine < 0) _selectionStartLine = lineIdx;
                            _selectionEndLine = lineIdx - 1;
                            UpdateSelectionHighlight();
                        }
                        else
                        {
                            ClearSelection();
                        }
                        ActivateLine(lineIdx - 1, col);
                        return;
                    }
                }

                // 8. DOWN ARROW: Navigate to next line
                if (e.Key == Key.Down && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift))
                {
                    int visualLine = tb.GetLineIndexFromCharacterIndex(tb.CaretIndex);
                    if (visualLine == tb.LineCount - 1 && lineIdx < _lines.Count - 1)
                    {
                        e.Handled = true;
                        int col = tb.CaretIndex;
                        if (Keyboard.Modifiers == ModifierKeys.Shift)
                        {
                            if (_selectionStartLine < 0) _selectionStartLine = lineIdx;
                            _selectionEndLine = lineIdx + 1;
                            UpdateSelectionHighlight();
                        }
                        else
                        {
                            ClearSelection();
                        }
                        ActivateLine(lineIdx + 1, col);
                        return;
                    }
                }

                // 9. Markdown shortcuts (Ctrl+B, Ctrl+I, Ctrl+K)
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

        private void HandlePaste(int lineIdx, TextBox tb)
        {
            try
            {
                if (!Clipboard.ContainsText()) return;
                string pasteText = Clipboard.GetText();
                if (string.IsNullOrEmpty(pasteText)) return;

                // Single line paste: standard TextBox insert
                if (!pasteText.Contains("\r") && !pasteText.Contains("\n"))
                {
                    int selStart = tb.SelectionStart;
                    int selLen = tb.SelectionLength;
                    string current = tb.Text;
                    tb.Text = current.Substring(0, selStart) + pasteText + current.Substring(selStart + selLen);
                    tb.CaretIndex = selStart + pasteText.Length;
                    _lines[lineIdx].RawText = tb.Text;
                    NotifyChanged();
                    return;
                }

                // Multi-line paste! Handles 10, 50, 500 lines seamlessly!
                string[] pasteLines = pasteText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                string currentText = tb.Text;
                int start = tb.SelectionStart;
                int len = tb.SelectionLength;
                string before = currentText.Substring(0, start);
                string after = currentText.Substring(start + len);

                // Line 0 is merged with text before caret
                _lines[lineIdx].RawText = before + pasteLines[0];

                int insertIdx = lineIdx + 1;
                for (int i = 1; i < pasteLines.Length - 1; i++)
                {
                    _lines.Insert(insertIdx, new DocumentLine { RawText = pasteLines[i] });
                    insertIdx++;
                }

                // Last line is merged with text after caret
                string lastLineText = pasteLines[pasteLines.Length - 1] + after;
                _lines.Insert(insertIdx, new DocumentLine { RawText = lastLineText });

                RebuildUI();
                ActivateLine(insertIdx, pasteLines[pasteLines.Length - 1].Length);
                NotifyChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError("HandlePaste", ex);
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

                // Empty task item: Enter finishes the list
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

                var newLine = new DocumentLine { RawText = nextPrefix + afterCaret };
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

                var newLine = new DocumentLine { RawText = prefix + afterCaret };
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

                var newLine = new DocumentLine { RawText = nextPrefix + afterCaret };
                _lines.Insert(lineIdx + 1, newLine);

                RebuildUI();
                ActivateLine(lineIdx + 1, nextPrefix.Length);
                NotifyChanged();
                return;
            }

            // 4. Standard Line Break
            _lines[lineIdx].RawText = beforeCaret;
            tb.Text = beforeCaret;

            var plainNewLine = new DocumentLine { RawText = afterCaret };
            _lines.Insert(lineIdx + 1, plainNewLine);

            RebuildUI();
            ActivateLine(lineIdx + 1, 0);
            NotifyChanged();
        }

        private void HandleBackspaceAtStart(int lineIdx, TextBox tb)
        {
            string currentLineText = tb.Text;
            var prevLine = _lines[lineIdx - 1];
            int prevLen = prevLine.RawText.Length;

            prevLine.RawText += currentLineText;
            _lines.RemoveAt(lineIdx);

            RebuildUI();
            ActivateLine(lineIdx - 1, prevLen);
            NotifyChanged();
        }

        private void HandleDeleteAtEnd(int lineIdx, TextBox tb)
        {
            var nextLine = _lines[lineIdx + 1];
            tb.Text += nextLine.RawText;
            _lines[lineIdx].RawText = tb.Text;
            _lines.RemoveAt(lineIdx + 1);

            RebuildUI();
            ActivateLine(lineIdx, tb.CaretIndex);
            NotifyChanged();
        }

        private void WrapSelection(TextBox tb, string prefix, string suffix)
        {
            string sel = tb.SelectedText;
            int start = tb.SelectionStart;
            if (string.IsNullOrEmpty(sel))
            {
                tb.SelectedText = $"{prefix}{suffix}";
                tb.CaretIndex = start + prefix.Length;
            }
            else
            {
                tb.SelectedText = $"{prefix}{sel}{suffix}";
                tb.Select(start + prefix.Length, sel.Length);
            }
            _lines[_activeLineIndex].RawText = tb.Text;
            NotifyChanged();
        }

        // ==========================================
        // SELECTION & CLIPBOARD
        // ==========================================

        public void SelectAll()
        {
            _isAllSelected = true;
            _selectionStartLine = 0;
            _selectionEndLine = _lines.Count - 1;
            UpdateSelectionHighlight();
        }

        public void ClearSelection()
        {
            if (_isAllSelected || _selectionStartLine >= 0)
            {
                _isAllSelected = false;
                _selectionStartLine = -1;
                _selectionEndLine = -1;
                UpdateSelectionHighlight();
            }
        }

        public string GetSelectedText()
        {
            if (_isAllSelected)
            {
                return GetFullDocumentText();
            }
            if (_selectionStartLine >= 0 && _selectionEndLine >= 0)
            {
                int min = Math.Min(_selectionStartLine, _selectionEndLine);
                int max = Math.Max(_selectionStartLine, _selectionEndLine);
                min = Math.Max(0, min);
                max = Math.Min(_lines.Count - 1, max);
                return string.Join(Environment.NewLine, _lines.Skip(min).Take(max - min + 1).Select(l => l.RawText));
            }
            return string.Empty;
        }

        private void DeleteSelectedLines()
        {
            if (_isAllSelected)
            {
                _lines.Clear();
                _lines.Add(new DocumentLine { RawText = "" });
                ClearSelection();
                RebuildUI();
                ActivateLine(0, 0);
                NotifyChanged();
                return;
            }

            if (_selectionStartLine >= 0 && _selectionEndLine >= 0)
            {
                int min = Math.Min(_selectionStartLine, _selectionEndLine);
                int max = Math.Max(_selectionStartLine, _selectionEndLine);
                min = Math.Max(0, min);
                max = Math.Min(_lines.Count - 1, max);

                _lines.RemoveRange(min, max - min + 1);
                if (_lines.Count == 0)
                {
                    _lines.Add(new DocumentLine { RawText = "" });
                }

                ClearSelection();
                RebuildUI();
                ActivateLine(Math.Min(min, _lines.Count - 1), 0);
                NotifyChanged();
            }
        }

        private void UpdateSelectionHighlight()
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                var container = _lines[i].Container;
                if (container == null) continue;

                bool isSelected = false;
                if (_isAllSelected)
                {
                    isSelected = true;
                }
                else if (_selectionStartLine >= 0 && _selectionEndLine >= 0)
                {
                    int min = Math.Min(_selectionStartLine, _selectionEndLine);
                    int max = Math.Max(_selectionStartLine, _selectionEndLine);
                    isSelected = (i >= min && i <= max);
                }

                container.Background = isSelected ? Theme.SelectionBrush : Brushes.Transparent;
            }
        }

        private ContextMenu CreateLineContextMenu(int lineIdx, TextBox tb)
        {
            bool isDark = Theme.Name == "Charcoal";
            var menu = new ContextMenu
            {
                Background = isDark ? new SolidColorBrush(Color.FromRgb(30, 41, 59)) : Brushes.White,
                BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(51, 65, 85)) : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6)
            };

            MenuItem CreateItem(string title, Action action)
            {
                var item = new MenuItem
                {
                    Header = title,
                    Height = 28,
                    Cursor = Cursors.Hand,
                    FontSize = 12,
                    Foreground = isDark ? new SolidColorBrush(Color.FromRgb(241, 245, 249)) : new SolidColorBrush(Color.FromRgb(30, 41, 59))
                };
                item.Click += (s, e) => action();
                return item;
            }

            menu.Items.Add(CreateItem("✂️  Cut (Ctrl+X)", () => tb.Cut()));
            menu.Items.Add(CreateItem("📋  Copy (Ctrl+C)", () =>
            {
                if (_isAllSelected || (_selectionStartLine >= 0 && _selectionEndLine >= 0))
                    Clipboard.SetText(GetSelectedText());
                else
                    tb.Copy();
            }));
            menu.Items.Add(CreateItem("📥  Paste (Ctrl+V)", () => HandlePaste(lineIdx, tb)));
            menu.Items.Add(CreateItem("🔲  Select All (Ctrl+A)", () => SelectAll()));

            menu.Items.Add(new Separator { Margin = new Thickness(4) });

            menu.Items.Add(CreateItem("𝗕  Bold (Ctrl+B)", () => WrapSelection(tb, "**", "**")));
            menu.Items.Add(CreateItem("𝘐  Italic (Ctrl+I)", () => WrapSelection(tb, "*", "*")));
            menu.Items.Add(CreateItem("🔗  Link (Ctrl+K)", () => WrapSelection(tb, "[", "](https://)")));

            return menu;
        }
    }
}
