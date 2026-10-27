using System.Collections.Generic;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace ClassicStickyNotes.Services
{
    public class NoteTheme
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Color HeaderColor { get; set; }
        public Color BodyColor { get; set; }
        public Color BorderColor { get; set; }
        public Color TextColor { get; set; }
        public Color SubduedTextColor { get; set; }
        public Color SelectionColor { get; set; }
        public Color AccentColor { get; set; }

        public SolidColorBrush HeaderBrush => new SolidColorBrush(HeaderColor);
        public SolidColorBrush BodyBrush => new SolidColorBrush(BodyColor);
        public SolidColorBrush BorderBrush => new SolidColorBrush(BorderColor);
        public SolidColorBrush TextBrush => new SolidColorBrush(TextColor);
        public SolidColorBrush SubduedTextBrush => new SolidColorBrush(SubduedTextColor);
        public SolidColorBrush SelectionBrush => new SolidColorBrush(SelectionColor);
        public SolidColorBrush AccentBrush => new SolidColorBrush(AccentColor);
    }

    public static class ThemePalette
    {
        public static readonly Dictionary<string, NoteTheme> Themes = new Dictionary<string, NoteTheme>
        {
            ["Yellow"] = new NoteTheme
            {
                Name = "Yellow",
                DisplayName = "Classic Yellow",
                HeaderColor = Color.FromRgb(255, 241, 118),       // #FFF176
                BodyColor = Color.FromRgb(255, 249, 196),         // #FFF9C4
                BorderColor = Color.FromRgb(249, 215, 76),
                TextColor = Color.FromRgb(33, 33, 33),
                SubduedTextColor = Color.FromRgb(117, 117, 117),
                SelectionColor = Color.FromArgb(120, 255, 214, 0),
                AccentColor = Color.FromRgb(245, 127, 23)
            },
            ["Green"] = new NoteTheme
            {
                Name = "Green",
                DisplayName = "Mint Green",
                HeaderColor = Color.FromRgb(165, 214, 167),       // #A5D6A7
                BodyColor = Color.FromRgb(232, 245, 233),         // #E8F5E9
                BorderColor = Color.FromRgb(129, 199, 132),
                TextColor = Color.FromRgb(27, 94, 32),
                SubduedTextColor = Color.FromRgb(56, 142, 60),
                SelectionColor = Color.FromArgb(120, 102, 187, 106),
                AccentColor = Color.FromRgb(46, 125, 50)
            },
            ["Pink"] = new NoteTheme
            {
                Name = "Pink",
                DisplayName = "Pastel Pink",
                HeaderColor = Color.FromRgb(244, 143, 177),       // #F48FB1
                BodyColor = Color.FromRgb(252, 228, 236),         // #FCE4EC
                BorderColor = Color.FromRgb(240, 98, 146),
                TextColor = Color.FromRgb(136, 14, 79),
                SubduedTextColor = Color.FromRgb(173, 20, 87),
                SelectionColor = Color.FromArgb(120, 236, 64, 122),
                AccentColor = Color.FromRgb(194, 24, 91)
            },
            ["Blue"] = new NoteTheme
            {
                Name = "Blue",
                DisplayName = "Sky Blue",
                HeaderColor = Color.FromRgb(144, 202, 249),       // #90CAF9
                BodyColor = Color.FromRgb(227, 242, 253),         // #E3F2FD
                BorderColor = Color.FromRgb(100, 181, 246),
                TextColor = Color.FromRgb(13, 71, 161),
                SubduedTextColor = Color.FromRgb(21, 101, 192),
                SelectionColor = Color.FromArgb(120, 66, 165, 245),
                AccentColor = Color.FromRgb(25, 118, 210)
            },
            ["Purple"] = new NoteTheme
            {
                Name = "Purple",
                DisplayName = "Lavender",
                HeaderColor = Color.FromRgb(206, 147, 216),       // #CE93D8
                BodyColor = Color.FromRgb(243, 229, 245),         // #F3E5F5
                BorderColor = Color.FromRgb(186, 104, 200),
                TextColor = Color.FromRgb(74, 20, 140),
                SubduedTextColor = Color.FromRgb(106, 27, 154),
                SelectionColor = Color.FromArgb(120, 171, 71, 188),
                AccentColor = Color.FromRgb(123, 31, 162)
            },
            ["Charcoal"] = new NoteTheme
            {
                Name = "Charcoal",
                DisplayName = "Dark Charcoal",
                HeaderColor = Color.FromRgb(55, 65, 81),          // #374151
                BodyColor = Color.FromRgb(31, 41, 55),            // #1F2937
                BorderColor = Color.FromRgb(75, 85, 99),
                TextColor = Color.FromRgb(243, 244, 246),
                SubduedTextColor = Color.FromRgb(156, 163, 175),
                SelectionColor = Color.FromArgb(120, 99, 102, 241),
                AccentColor = Color.FromRgb(129, 140, 248)
            },
            ["White"] = new NoteTheme
            {
                Name = "White",
                DisplayName = "Clean White",
                HeaderColor = Color.FromRgb(235, 235, 235),
                BodyColor = Color.FromRgb(255, 255, 255),
                BorderColor = Color.FromRgb(200, 200, 200),
                TextColor = Color.FromRgb(33, 33, 33),
                SubduedTextColor = Color.FromRgb(117, 117, 117),
                SelectionColor = Color.FromArgb(120, 180, 200, 220),
                AccentColor = Color.FromRgb(66, 133, 244)
            }
        };

        public static NoteTheme GetTheme(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !Themes.TryGetValue(name, out var theme))
            {
                return Themes["Yellow"];
            }
            return theme;
        }
    }
}
