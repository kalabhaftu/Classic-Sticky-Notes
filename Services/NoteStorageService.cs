using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClassicStickyNotes.Models;

namespace ClassicStickyNotes.Services
{
    public class NoteStorageService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClassicStickyNotes");

        private static readonly string NotesFilePath = Path.Combine(AppDataFolder, "notes.json");
        private static readonly object FileLock = new object();

        static NoteStorageService()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
        }

        public static string GetStorageFolder() => AppDataFolder;

        public static List<NoteModel> LoadNotes()
        {
            lock (FileLock)
            {
                try
                {
                    if (File.Exists(NotesFilePath))
                    {
                        string json = File.ReadAllText(NotesFilePath);
                        var notes = JsonSerializer.Deserialize<List<NoteModel>>(json);
                        if (notes != null && notes.Count > 0)
                        {
                            return notes;
                        }
                    }
                }
                catch
                {
                    // If corrupted, fallback to default starter note
                }

                // Default starter note
                var defaultNote = CreateStarterNote();
                SaveNotes(new List<NoteModel> { defaultNote });
                return new List<NoteModel> { defaultNote };
            }
        }

        public static void SaveNotes(IEnumerable<NoteModel> notes)
        {
            lock (FileLock)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(notes, options);
                    string tempPath = NotesFilePath + ".tmp";
                    File.WriteAllText(tempPath, json);
                    File.Move(tempPath, NotesFilePath, true);
                }
                catch
                {
                    // Log or handle quietly
                }
            }
        }

        public static void SaveSingleNote(NoteModel note)
        {
            lock (FileLock)
            {
                var existing = LoadNotes();
                int idx = existing.FindIndex(n => n.Id == note.Id);
                if (idx >= 0)
                {
                    existing[idx] = note;
                }
                else
                {
                    existing.Add(note);
                }
                SaveNotes(existing);
            }
        }

        public static void DeleteNote(string id)
        {
            lock (FileLock)
            {
                var existing = LoadNotes();
                existing.RemoveAll(n => n.Id == id);
                SaveNotes(existing);
            }
        }

        private static NoteModel CreateStarterNote()
        {
            return new NoteModel
            {
                Id = Guid.NewGuid().ToString(),
                ColorName = "Yellow",
                FontFamily = "Segoe UI",
                FontSize = 15.0,
                IsPinned = true,
                IsPreview = true,
                Left = 240,
                Top = 180,
                Width = 360,
                Height = 420,
                Content = @"# Welcome to Sticky Notes! 📌

Simple, lightweight desktop notes with **Obsidian-style** live Markdown.

### ✨ How it works:
- [x] Click any checkbox to check it off instantly!
- [ ] Click anywhere on this text to edit directly
- [ ] Press **Esc** or click away to preview
- [x] Auto-saves every keystroke

### ⌨️ Shortcuts:
- **Esc** or **Ctrl+Enter**: Render preview
- **Ctrl + N**: New note
- **Ctrl + P**: Pin (Always on Top)
- **Ctrl + B**: **Bold**
- **Ctrl + I**: *Italic*
- **Enter on list items**: Automatically continues checklists & bullets!

> ""Simplicity is the soul of efficiency."" ✨"
            };
        }
    }
}
