# Classic Sticky Notes 📌

A modern reimagining of the classic Windows Sticky Notes desktop application, featuring live inline Markdown preview, sleek thin scrollbars, custom pastel themes, auto-saving, and full Inno Setup packaging. Built with .NET and WPF with zero external dependencies.

---

## 🚀 Features

- **Live Inline Markdown Preview**:
  - Inactive lines render beautifully as formatted headings, bold/italic text, checklists, and code.
  - Active line shows the raw markdown formatting for immediate, frictionless editing.
  - Moving the cursor to another line instantly renders the previous line.
  - **Interactive Checklists**: Click checkboxes (`- [ ]` / `- [x]`) directly on your note to toggle them.
  - **Smart List Continuation**: Pressing `Enter` on a task list (`- [ ]`), bullet (`- `), or numbered list (`1. `) automatically inserts the next item. Pressing `Enter` on an empty bullet finishes the list.
- **Unrestricted Selection & Clipboard**:
  - **Select All (`Ctrl+A`)**: Select the entire note with a single keystroke.
  - **Multi-Line Selection**: Drag or use `Shift + Up/Down` to select multiple lines.
  - **Multi-Line Copy & Paste (`Ctrl+C` / `Ctrl+V`)**: Paste large text blocks, paragraphs, and multi-line content seamlessly.
  - **Context Menu**: Modern right-click menu with Cut, Copy, Paste, Select All, Bold, Italic, and Link.
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
| `Click` on any line | Start editing that line directly |
| `Up` / `Down` Arrow | Navigate between lines (auto-renders previous line) |
| `Enter` (on list items) | Auto-continue task/bullet/numbered list |
| `Ctrl + A` | Select all text across the entire note |
| `Ctrl + C` | Copy selection or all text |
| `Ctrl + V` | Paste text (full multi-line support) |
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
iscc "installer\ClassicStickyNotes.iss"
```
The output installer is located at `installer_output\ClassicStickyNotes_Setup_v1.0.1.exe`.
