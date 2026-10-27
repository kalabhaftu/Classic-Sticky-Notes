using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using ClassicStickyNotes.Models;
using ClassicStickyNotes.Services;
using WpfApplication = System.Windows.Application;

namespace ClassicStickyNotes
{
    public partial class App : WpfApplication
    {
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Logger.InitializeGlobalExceptionHandling();
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            InitializeTrayIcon();

            // Load saved notes
            var notes = NoteStorageService.LoadNotes();
            if (notes.Count == 0)
            {
                var starter = new NoteWindow();
                starter.Show();
            }
            else
            {
                foreach (var note in notes)
                {
                    var win = new NoteWindow(note);
                    win.Show();
                }
            }
        }

        private void InitializeTrayIcon()
        {
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                var icon = File.Exists(iconPath)
                    ? new System.Drawing.Icon(iconPath)
                    : System.Drawing.SystemIcons.Application;

                _notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = icon,
                    Visible = true,
                    Text = "Classic Sticky Notes"
                };

                _notifyIcon.DoubleClick += (s, args) => ShowOrSpawnNotes();

                var contextMenu = new System.Windows.Forms.ContextMenuStrip();

                var itemNew = new System.Windows.Forms.ToolStripMenuItem("➕ New Note");
                itemNew.Click += (s, args) =>
                {
                    var newNote = new NoteModel
                    {
                        Left = 200 + (Windows.Count * 25),
                        Top = 200 + (Windows.Count * 25)
                    };
                    NoteStorageService.SaveSingleNote(newNote);
                    var win = new NoteWindow(newNote);
                    win.Show();
                    win.Activate();
                };

                var itemShowAll = new System.Windows.Forms.ToolStripMenuItem("📋 Show All Notes");
                itemShowAll.Click += (s, args) => ShowOrSpawnNotes();

                var itemFolder = new System.Windows.Forms.ToolStripMenuItem("📁 Open Notes Folder");
                itemFolder.Click += (s, args) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe", NoteStorageService.GetStorageFolder()) { UseShellExecute = true });
                    }
                    catch { }
                };

                var itemAbout = new System.Windows.Forms.ToolStripMenuItem("ℹ️ About");
                itemAbout.Click += (s, args) =>
                {
                    foreach (Window win in Windows)
                    {
                        if (win is NoteWindow nw)
                        {
                            nw.Activate();
                            nw.ShowAboutDialog();
                            return;
                        }
                    }
                    var newWin = new NoteWindow();
                    newWin.Show();
                    newWin.ShowAboutDialog();
                };

                var itemExit = new System.Windows.Forms.ToolStripMenuItem("❌ Exit");
                itemExit.Click += (s, args) =>
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    Shutdown();
                };

                contextMenu.Items.Add(itemNew);
                contextMenu.Items.Add(itemShowAll);
                contextMenu.Items.Add(itemFolder);
                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                contextMenu.Items.Add(itemAbout);
                contextMenu.Items.Add(itemExit);

                _notifyIcon.ContextMenuStrip = contextMenu;
            }
            catch
            {
                // Tray icon fallback gracefully
            }
        }

        private void ShowOrSpawnNotes()
        {
            if (Windows.Count == 0)
            {
                var newNote = new NoteModel();
                NoteStorageService.SaveSingleNote(newNote);
                var win = new NoteWindow(newNote);
                win.Show();
                win.Activate();
            }
            else
            {
                foreach (Window win in Windows)
                {
                    if (win.WindowState == WindowState.Minimized)
                    {
                        win.WindowState = WindowState.Normal;
                    }
                    win.Show();
                    win.Activate();
                }
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }
    }
}
