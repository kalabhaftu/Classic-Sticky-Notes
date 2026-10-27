using System;

namespace ClassicStickyNotes.Models
{
    public class NoteModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public string ColorName { get; set; } = "Yellow";
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 15.0;
        public bool IsPinned { get; set; } = false;
        public bool IsPreview { get; set; } = false;
        public double Left { get; set; } = 200;
        public double Top { get; set; } = 200;
        public double Width { get; set; } = 340;
        public double Height { get; set; } = 380;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
