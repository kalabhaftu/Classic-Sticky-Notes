using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ClassicStickyNotes.Helpers;
using ClassicStickyNotes.Models;
using ClassicStickyNotes.Services;
using Microsoft.Win32;

namespace ClassicStickyNotes
{
    public partial class NoteWindow : Window
    {
        private readonly NoteModel _note;
        private readonly DispatcherTimer _autoSaveTimer;
        private ObsidianEditor? _editor;
        private bool _isLoaded = false;
        private Action? _dialogConfirmAction = null;

        public NoteModel Note => _note;

        public NoteWindow(NoteModel? note = null)
        {
            InitializeComponent();

            _note = note ?? new NoteModel();

            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            _autoSaveTimer.Tick += (s, e) =>
            {
                _autoSaveTimer.Stop();
                PerformSave();
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position and Size
            Width = Math.Max(MinWidth, _note.Width);
            Height = Math.Max(MinHeight, _note.Height);

            // Ensure window is placed on screen (multi-monitor virtual screen aware)
            double virtualLeft = SystemParameters.VirtualScreenLeft;
            double virtualTop = SystemParameters.VirtualScreenTop;
            double virtualWidth = SystemParameters.VirtualScreenWidth;
            double virtualHeight = SystemParameters.VirtualScreenHeight;

            if (_note.Left >= virtualLeft - 20 && _note.Left + 50 <= virtualLeft + virtualWidth)
                Left = _note.Left;
            else
                Left = Math.Max(virtualLeft + 60, 120);

            if (_note.Top >= virtualTop - 20 && _note.Top + 50 <= virtualTop + virtualHeight)
                Top = _note.Top;
            else
                Top = Math.Max(virtualTop + 60, 120);

            // Initialize Obsidian Live Preview Editor
            var theme = ThemePalette.GetTheme(_note.ColorName);
            _editor = new ObsidianEditor(EditorScrollViewer, LinesStackPanel, theme)
            {
                FontFamilyName = _note.FontFamily,
                BaseFontSize = _note.FontSize
            };
            _editor.DocumentChanged += OnDocumentChanged;
            _editor.LoadDocument(_note.Content);

            // Apply Theme, Font, Pin
            ApplyTheme(_note.ColorName);
            ApplyPinState(_note.IsPinned);

            UpdateStats();
            _isLoaded = true;
        }

        private void OnDocumentChanged()
        {
            if (!_isLoaded) return;
            TxtFooterStats.Text = "Typing...";
            _autoSaveTimer.Stop();
            _autoSaveTimer.Start();
        }

        private void PerformSave()
        {
            if (_editor == null) return;

            _note.Content = _editor.GetFullDocumentText();
            _note.Left = Left;
            _note.Top = Top;
            _note.Width = ActualWidth;
            _note.Height = ActualHeight;
            _note.UpdatedAt = DateTime.Now;

            NoteStorageService.SaveSingleNote(_note);
            UpdateStats();
        }

        private void UpdateStats()
        {
            if (_editor == null) return;
            string text = _editor.GetFullDocumentText();
            int chars = text.Length;
            int words = string.IsNullOrWhiteSpace(text) 
                ? 0 
                : text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            TxtFooterStats.Text = $"Words: {words} | Chars: {chars}  •  Auto-saved";
        }

        // ==========================================
        // THEMES & STYLING
        // ==========================================

        private void ApplyTheme(string colorName)
        {
            _note.ColorName = colorName;
            var theme = ThemePalette.GetTheme(colorName);

            MainCard.Background = theme.BodyBrush;
            MainCard.BorderBrush = theme.BorderBrush;
            HeaderBar.Background = theme.HeaderBrush;
            FooterBar.Background = theme.HeaderBrush;
            FooterBar.BorderBrush = theme.BorderBrush;

            TxtFooterStats.Foreground = theme.SubduedTextBrush;

            // Update Dialog theme
            bool isDark = colorName == "Charcoal";
            DialogCard.Background = isDark ? new SolidColorBrush(Color.FromRgb(30, 41, 59)) : Brushes.White;
            DialogCard.BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(51, 65, 85)) : new SolidColorBrush(Color.FromRgb(226, 232, 240));
            TxtDialogTitle.Foreground = isDark ? Brushes.White : new SolidColorBrush(Color.FromRgb(30, 41, 59));
            TxtDialogMessage.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(203, 213, 225)) : new SolidColorBrush(Color.FromRgb(71, 85, 105));

            _editor?.ApplyThemeAndFont(theme, _note.FontFamily, _note.FontSize);
        }

        private void ApplyFont(string fontFamilyName, double fontSize)
        {
            _note.FontFamily = fontFamilyName;
            _note.FontSize = fontSize;

            var theme = ThemePalette.GetTheme(_note.ColorName);
            _editor?.ApplyThemeAndFont(theme, fontFamilyName, fontSize);
        }

        private void ApplyPinState(bool isPinned)
        {
            _note.IsPinned = isPinned;
            Topmost = isPinned;

            if (isPinned)
            {
                TxtPinIcon.Opacity = 1.0;
                BtnPin.ToolTip = "Always on Top: ON (Ctrl+P)";
            }
            else
            {
                TxtPinIcon.Opacity = 0.35;
                BtnPin.ToolTip = "Always on Top: OFF (Ctrl+P)";
            }
        }

        // ==========================================
        // MODERN DROPDOWNS & POPUPS
        // ==========================================

        private ContextMenu CreateModernMenu()
        {
            bool isDark = _note.ColorName == "Charcoal";
            var menu = new ContextMenu
            {
                Background = isDark ? new SolidColorBrush(Color.FromRgb(30, 41, 59)) : Brushes.White,
                BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(51, 65, 85)) : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                ClipToBounds = false
            };

            var shadow = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 16,
                Opacity = 0.22,
                ShadowDepth = 4,
                Direction = 270,
                Color = Colors.Black
            };
            menu.Effect = shadow;

            return menu;
        }

        private MenuItem CreateModernMenuItem(string title, object? icon = null, Action? onClick = null, bool isChecked = false, bool isDanger = false)
        {
            bool isDark = _note.ColorName == "Charcoal";
            var item = new MenuItem
            {
                Header = title,
                Height = 30,
                Cursor = Cursors.Hand,
                Foreground = isDanger 
                    ? new SolidColorBrush(Color.FromRgb(239, 68, 68)) 
                    : (isDark ? new SolidColorBrush(Color.FromRgb(241, 245, 249)) : new SolidColorBrush(Color.FromRgb(30, 41, 59))),
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 1, 0, 1)
            };

            if (icon != null)
            {
                item.Icon = icon;
            }

            if (isChecked)
            {
                item.Header = $"{title}  ✓";
                item.FontWeight = FontWeights.SemiBold;
            }

            if (onClick != null)
            {
                item.Click += (s, e) => onClick();
            }

            return item;
        }

        private Separator CreateModernSeparator()
        {
            bool isDark = _note.ColorName == "Charcoal";
            return new Separator
            {
                Height = 1,
                Margin = new Thickness(4, 4, 4, 4),
                Background = isDark ? new SolidColorBrush(Color.FromRgb(51, 65, 85)) : new SolidColorBrush(Color.FromRgb(226, 232, 240))
            };
        }

        private void Palette_Click(object sender, RoutedEventArgs e)
        {
            var menu = CreateModernMenu();

            var headerItem = new MenuItem
            {
                Header = "NOTE COLOR",
                IsEnabled = false,
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Padding = new Thickness(8, 2, 8, 4)
            };
            menu.Items.Add(headerItem);

            var swatchPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(4, 4, 4, 4)
            };

            foreach (var kvp in ThemePalette.Themes)
            {
                var theme = kvp.Value;
                string colorKey = kvp.Key;

                var circle = new Border
                {
                    Width = 26,
                    Height = 26,
                    CornerRadius = new CornerRadius(13),
                    Background = theme.HeaderBrush,
                    BorderBrush = _note.ColorName == colorKey 
                        ? new SolidColorBrush(Color.FromRgb(37, 99, 235)) 
                        : theme.BorderBrush,
                    BorderThickness = _note.ColorName == colorKey ? new Thickness(2.5) : new Thickness(1),
                    Margin = new Thickness(3, 0, 3, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = theme.DisplayName
                };

                circle.MouseLeftButtonUp += (s, args) =>
                {
                    menu.IsOpen = false;
                    ApplyTheme(colorKey);
                    PerformSave();
                };

                swatchPanel.Children.Add(circle);
            }

            var swatchMenuItem = new MenuItem
            {
                Header = swatchPanel,
                Padding = new Thickness(2),
                Focusable = false
            };
            menu.Items.Add(swatchMenuItem);

            menu.PlacementTarget = sender as Button;
            menu.IsOpen = true;
        }

        private void Font_Click(object sender, RoutedEventArgs e)
        {
            var menu = CreateModernMenu();

            // Font Families Header
            menu.Items.Add(new MenuItem 
            { 
                Header = "FONT FAMILY", 
                IsEnabled = false, 
                FontWeight = FontWeights.Bold, 
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Padding = new Thickness(8, 2, 8, 4)
            });

            string[] fonts = { "Segoe UI", "Segoe Print", "Consolas", "Arial", "Georgia", "Comic Sans MS" };
            foreach (var fontName in fonts)
            {
                bool isSelected = _note.FontFamily.Equals(fontName, StringComparison.OrdinalIgnoreCase);
                var item = CreateModernMenuItem(
                    fontName, 
                    null, 
                    () => {
                        ApplyFont(fontName, _note.FontSize);
                        PerformSave();
                    }, 
                    isChecked: isSelected);
                item.FontFamily = new FontFamily(fontName);
                menu.Items.Add(item);
            }

            menu.Items.Add(CreateModernSeparator());

            // Font Sizes Header
            menu.Items.Add(new MenuItem 
            { 
                Header = "FONT SIZE", 
                IsEnabled = false, 
                FontWeight = FontWeights.Bold, 
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Padding = new Thickness(8, 2, 8, 4)
            });

            double[] sizes = { 12, 14, 16, 18, 20, 24 };
            foreach (var size in sizes)
            {
                bool isSelected = Math.Abs(_note.FontSize - size) < 0.5;
                var item = CreateModernMenuItem(
                    $"{size} pt", 
                    null, 
                    () => {
                        ApplyFont(_note.FontFamily, size);
                        PerformSave();
                    }, 
                    isChecked: isSelected);
                menu.Items.Add(item);
            }

            menu.PlacementTarget = sender as Button;
            menu.IsOpen = true;
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            var menu = CreateModernMenu();

            menu.Items.Add(CreateModernMenuItem("📥  Export to Markdown (.md)...", null, () => ExportToMarkdown()));
            menu.Items.Add(CreateModernMenuItem("📋  Copy All Text", null, () => {
                if (_editor != null)
                {
                    Clipboard.SetText(_editor.GetFullDocumentText());
                    TxtFooterStats.Text = "Text copied to clipboard!";
                }
            }));
            menu.Items.Add(CreateModernMenuItem("🧹  Clear Note", null, () => {
                ShowModernDialog(
                    "Clear Note",
                    "Are you sure you want to clear all contents of this note?",
                    "Clear Content",
                    "🧹",
                    () => {
                        _editor?.LoadDocument("");
                        PerformSave();
                    },
                    isDanger: true);
            }));

            menu.Items.Add(CreateModernSeparator());

            menu.Items.Add(CreateModernMenuItem("📁  Open Storage Folder", null, () => {
                try {
                    Process.Start(new ProcessStartInfo("explorer.exe", NoteStorageService.GetStorageFolder()) { UseShellExecute = true });
                } catch { }
            }));

            menu.Items.Add(CreateModernMenuItem("📄  Open Activity & Error Log", null, () => {
                try {
                    string logPath = Logger.GetLogPath();
                    if (!File.Exists(logPath)) File.WriteAllText(logPath, "No errors logged.\n");
                    Process.Start(new ProcessStartInfo("notepad.exe", logPath) { UseShellExecute = true });
                } catch { }
            }));

            menu.Items.Add(CreateModernMenuItem("ℹ️  About Classic Sticky Notes", null, () => ShowAboutDialog()));

            menu.Items.Add(CreateModernSeparator());

            menu.Items.Add(CreateModernMenuItem("🗑  Delete This Note", null, () => {
                ShowModernDialog(
                    "Delete Note",
                    "Are you sure you want to permanently delete this sticky note?",
                    "Delete Note",
                    "🗑",
                    () => DeleteCurrentNote(),
                    isDanger: true);
            }, isDanger: true));

            menu.PlacementTarget = sender as Button;
            menu.IsOpen = true;
        }

        // ==========================================
        // MODERN IN-WINDOW MODAL DIALOGS
        // ==========================================

        public void ShowAboutDialog()
        {
            ShowModernDialog(
                "About Classic Sticky Notes",
                "A lightweight Windows sticky notes app with true Obsidian-style live Markdown preview, thin scrollbars, auto-save, and custom themes.\n\n• Line-by-line Live Preview (active line shows code, inactive lines preview)\n• Click checkboxes directly to check them off\n• Enter auto-continues task/bullet lists\n• Ctrl+N: New Note  |  Ctrl+P: Pin Always on Top",
                "Got it",
                "📌",
                null,
                isDanger: false,
                isAlertOnly: true);
        }

        private void ShowModernDialog(
            string title, 
            string message, 
            string confirmText, 
            string icon, 
            Action? onConfirm, 
            bool isDanger = false, 
            bool isAlertOnly = false)
        {
            TxtDialogTitle.Text = title;
            TxtDialogMessage.Text = message;
            TxtDialogIcon.Text = icon;
            BtnDialogConfirm.Content = confirmText;
            _dialogConfirmAction = onConfirm;

            if (isDanger)
            {
                BtnDialogConfirm.Background = new SolidColorBrush(Color.FromRgb(229, 57, 53));
            }
            else
            {
                BtnDialogConfirm.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            }

            BtnDialogCancel.Visibility = isAlertOnly ? Visibility.Collapsed : Visibility.Visible;
            DialogOverlay.Visibility = Visibility.Visible;
        }

        private void DialogConfirm_Click(object sender, RoutedEventArgs e)
        {
            DialogOverlay.Visibility = Visibility.Collapsed;
            _dialogConfirmAction?.Invoke();
        }

        private void DialogCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogOverlay.Visibility = Visibility.Collapsed;
        }

        private void DeleteCurrentNote()
        {
            NoteStorageService.DeleteNote(_note.Id);
            _isLoaded = false;
            Close();

            if (Application.Current.Windows.Count == 0)
            {
                var starter = new NoteWindow();
                starter.Show();
            }
        }

        private void ExportToMarkdown()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Markdown Files (*.md)|*.md|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = "StickyNote.md",
                DefaultExt = ".md"
            };

            if (dialog.ShowDialog() == true && _editor != null)
            {
                File.WriteAllText(dialog.FileName, _editor.GetFullDocumentText());
                ShowModernDialog(
                    "Export Complete",
                    $"Note successfully saved to:\n{Path.GetFileName(dialog.FileName)}",
                    "OK",
                    "✅",
                    null,
                    isDanger: false,
                    isAlertOnly: true);
            }
        }

        // ==========================================
        // KEYBOARD SHORTCUTS
        // ==========================================

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Esc closes dialog overlay if open
            if (e.Key == Key.Escape && DialogOverlay.Visibility == Visibility.Visible)
            {
                DialogOverlay.Visibility = Visibility.Collapsed;
                e.Handled = true;
                return;
            }

            // Ctrl+N: New Note
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                NewNote_Click(this, new RoutedEventArgs());
                return;
            }

            // Ctrl+P: Toggle Pin
            if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                ApplyPinState(!_note.IsPinned);
                PerformSave();
                return;
            }

            // Ctrl+S: Force Save
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                PerformSave();
                return;
            }
        }

        // ==========================================
        // WINDOW ACTIONS & RESIZING
        // ==========================================

        private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
                _note.Left = Left;
                _note.Top = Top;
                _autoSaveTimer.Stop();
                _autoSaveTimer.Start();
            }
        }

        private void NewNote_Click(object sender, RoutedEventArgs e)
        {
            var newNote = new NoteModel
            {
                Left = Left + 30,
                Top = Top + 30,
                Width = Width,
                Height = Height,
                ColorName = _note.ColorName,
                FontFamily = _note.FontFamily,
                FontSize = _note.FontSize,
                IsPinned = _note.IsPinned
            };
            NoteStorageService.SaveSingleNote(newNote);
            var win = new NoteWindow(newNote);
            win.Show();
        }

        private void Pin_Click(object sender, RoutedEventArgs e)
        {
            ApplyPinState(!_note.IsPinned);
            PerformSave();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Resize_Top(object sender, MouseButtonEventArgs e) => WindowResizer.ResizeWindow(this, WindowResizer.ResizeDirection.Top);
        private void Resize_Bottom(object sender, MouseButtonEventArgs e) => WindowResizer.ResizeWindow(this, WindowResizer.ResizeDirection.Bottom);
        private void Resize_Left(object sender, MouseButtonEventArgs e) => WindowResizer.ResizeWindow(this, WindowResizer.ResizeDirection.Left);
        private void Resize_Right(object sender, MouseButtonEventArgs e) => WindowResizer.ResizeWindow(this, WindowResizer.ResizeDirection.Right);
        private void Resize_TopLeft(object sender, MouseButtonEventArgs e) => WindowResizer.ResizeWindow(this, WindowResizer.ResizeDirection.TopLeft);
        private void Resize_TopRight(object sender, MouseButtonEventArgs e) => WindowResizer.ResizeWindow(this, WindowResizer.ResizeDirection.TopRight);
        private void Resize_BottomLeft(object sender, MouseButtonEventArgs e) => WindowResizer.ResizeWindow(this, WindowResizer.ResizeDirection.BottomLeft);
        private void Resize_BottomRight(object sender, MouseButtonEventArgs e) => WindowResizer.ResizeWindow(this, WindowResizer.ResizeDirection.BottomRight);
        private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => WindowResizer.ResizeWindow(this, WindowResizer.ResizeDirection.BottomRight);

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isLoaded)
            {
                PerformSave();
            }
        }
    }
}
