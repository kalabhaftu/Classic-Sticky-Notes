using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace ClassicStickyNotes.Helpers
{
    public static class SystemThemeHelper
    {
        public static bool IsDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    object? val = key.GetValue("AppsUseLightTheme");
                    if (val is int intVal)
                    {
                        return intVal == 0;
                    }
                }
            }
            catch { }
            return false;
        }

        // Dropdown & Menu Colors
        public static Brush MenuBackground => IsDarkTheme() 
            ? new SolidColorBrush(Color.FromRgb(30, 41, 59)) 
            : new SolidColorBrush(Color.FromRgb(255, 255, 255));

        public static Brush MenuBorderBrush => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(51, 65, 85)) 
            : new SolidColorBrush(Color.FromRgb(226, 232, 240));

        public static Brush MenuForeground => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(241, 245, 249)) 
            : new SolidColorBrush(Color.FromRgb(30, 41, 59));

        public static Brush MenuSubduedForeground => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(148, 163, 184)) 
            : new SolidColorBrush(Color.FromRgb(100, 116, 139));

        public static Brush MenuSeparatorBrush => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(51, 65, 85)) 
            : new SolidColorBrush(Color.FromRgb(226, 232, 240));

        // Dialog Card Colors
        public static Brush DialogBackground => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(30, 41, 59))
            : new SolidColorBrush(Color.FromRgb(255, 255, 255));

        public static Brush DialogBorderBrush => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(51, 65, 85))
            : new SolidColorBrush(Color.FromRgb(226, 232, 240));

        public static Brush DialogTitleForeground => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(255, 255, 255))
            : new SolidColorBrush(Color.FromRgb(30, 41, 59));

        public static Brush DialogMessageForeground => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(203, 213, 225)) 
            : new SolidColorBrush(Color.FromRgb(71, 85, 105));

        public static Brush DialogGhostBtnBackground => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(51, 65, 85))
            : new SolidColorBrush(Color.FromRgb(243, 244, 246));

        public static Brush DialogGhostBtnBorder => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(71, 85, 105))
            : new SolidColorBrush(Color.FromRgb(229, 231, 235));

        public static Brush DialogGhostBtnForeground => IsDarkTheme()
            ? new SolidColorBrush(Color.FromRgb(226, 232, 240))
            : new SolidColorBrush(Color.FromRgb(55, 65, 81));
    }
}
