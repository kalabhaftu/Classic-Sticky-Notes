# Classic Sticky Notes 📌

A modern reimagining of the classic Windows Sticky Notes desktop application, featuring **Obsidian-style** live Markdown preview, sleek thin scrollbars, custom pastel themes, auto-saving, and full Inno Setup packaging. Built with .NET and WPF with zero external dependencies.

---

## 🚀 Features

- **Obsidian-Style Live Preview (Unified Surface)**:
  - No confusing "Edit" vs "Preview" modal switchers or badges.
  - The note is always displayed as a beautiful, rendered Markdown document.
  - **Click anywhere to edit**: Click into the note to start typing immediately with zero friction.
  - **Auto-preview on blur**: Click outside or press `Esc` / `Ctrl+Enter` to seamlessly render the Markdown.
  - **Interactive Checklists**: Click checkboxes (`- [ ]` / `- [x]`) directly on your note without even entering edit mode.
  - **Smart List Continuation**: Pressing `Enter` on a task list (`- [ ]`), bullet (`- `), or numbered list (`1. `) automatically inserts the next item! Pressing Enter on an empty bullet finishes the list.
- **Sleek Thin Scrollbar**: Modern 5px semi-transparent floating scrollbar with rounded edges, replacing chunky default Windows scrollbars.
- **Classic Aesthetic**: Defaults to the classic warm yellow sticky note look with a smooth drop shadow and borderless design.
- **Full Markdown Support**:
  - Headings (`#`, `##`, `###`)
  - **Bold** (`**text**`), *Italic* (`*text*`), and ~~Strikethrough~~ (`~~text~~`)
  - Code blocks (```` ``` ````) and inline code (`` `code` ``)
  - Blockquotes (`> quote`)
  - Clickable Hyperlinks (`[label](url)`)
  - Horizontal rules (`---`)
- **Auto-Save**: Every keystroke, window move, and resize is debounced and auto-saved to `%APPDATA%\ClassicStickyNotes\notes.json`.
- **Multiple Color Themes**:
  - 🟡 Classic Yellow (Default)
  - 🟢 Mint Green
  - 🌸 Pastel Pink
  - 🔵 Sky Blue
  - 🟣 Lavender
  - ⚫ Dark Charcoal (Dark Mode)
  - ⚪ Clean White
- **Typography Controls**:
  - Font families: `Segoe UI`, `Segoe Print` (Handwritten style), `Consolas` (Monospace), `Arial`, `Georgia`, `Comic Sans MS`
  - Font sizes: `12 pt`, `14 pt`, `16 pt`, `18 pt`, `20 pt`, `24 pt`
- **Always on Top (Pin)**: Quick toggle via the header pin (`📌`) or `Ctrl+P`.
- **Freely Resizable**: Smooth Win32-assisted edge and corner resize borders plus bottom-right grip.
- **Multi-Note Support**: Click `➕` or press `Ctrl+N` to spawn multiple notes anywhere on your desktop.
- **System Tray Integration**: Stays docked in the system tray with a right-click menu:
  - New Note
  - Show All Notes
  - Open Notes Folder
  - About
  - Exit
- **Exporting**: Export any note directly to a `.md` or `.txt` file via the more menu (`⋮`).

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Click` on text | Start editing directly |
| `Esc` or `Ctrl + Enter` | Finish editing and render preview |
| `Enter` (on list items) | Auto-continue task/bullet/numbered list |
| `Ctrl + N` | Create a **New Sticky Note** |
| `Ctrl + P` | Toggle **Always on Top** (Pin) |
| `Ctrl + B` | Wrap selected text with `**bold**` |
| `Ctrl + I` | Wrap selected text with `*italic*` |
| `Ctrl + K` | Insert markdown hyperlink template |
| `Ctrl + S` | Force instant save |

---

## 🛠️ Building & Packaging

### Run / Debug:
```powershell
dotnet run
```

### Publish Release:
```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

### Build Inno Setup Installer:
```powershell
& "C:\Users\SlimShady\AppData\Local\Programs\Inno Setup 6\ISCC.exe" "installer\ClassicStickyNotes.iss"
```
The output installer is located at `installer_output\ClassicStickyNotes_Setup_v1.0.0.exe`.
