using System.Windows;
using System.Windows.Media;

namespace OpenWidget.Services;

public static class ThemeService
{
    public static void ApplyAccent(string themeName)
    {
        var color = themeName?.ToLowerInvariant() switch
        {
            "emerald" => Color.FromRgb(0x34, 0xD3, 0x99),
            "amber" => Color.FromRgb(0xFB, 0xBF, 0x24),
            "rose" => Color.FromRgb(0xFB, 0x71, 0x85),
            "purple" => Color.FromRgb(0xA8, 0x55, 0xF7),
            _ => Color.FromRgb(0x38, 0xBD, 0xF8) // Sky default
        };

        Application.Current.Resources["AccentColor"] = color;
        Application.Current.Resources["AccentBrush"] = new SolidColorBrush(color);
    }
}