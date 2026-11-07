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

        if (Application.Current?.Resources != null)
        {
            Application.Current.Resources["AccentColor"] = color;
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush(color);
        }
    }

    public const double DefaultTextScale = 1.0;
    public const double MinTextScale = 0.8;
    public const double MaxTextScale = 1.5;

    public static void ApplyTextScale(double scale)
    {
        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0)
        {
            scale = DefaultTextScale;
        }

        scale = System.Math.Clamp(scale, MinTextScale, MaxTextScale);

        if (Application.Current?.Resources == null) return;

        Application.Current.Resources["FontSizeMicro"] = ComputeScaled(9.0, scale);
        Application.Current.Resources["FontSizeMicroPlus"] = ComputeScaled(9.5, scale);
        Application.Current.Resources["FontSizeBadge"] = ComputeScaled(10.0, scale);
        Application.Current.Resources["FontSizeSecondary"] = ComputeScaled(10.5, scale);
        Application.Current.Resources["FontSizeBody"] = ComputeScaled(11.0, scale);
        Application.Current.Resources["FontSizeBodyMedium"] = ComputeScaled(11.5, scale);
        Application.Current.Resources["FontSizeHeader"] = ComputeScaled(12.0, scale);
        Application.Current.Resources["FontSizeTitle"] = ComputeScaled(13.5, scale);
        Application.Current.Resources["FontSizeClock"] = ComputeScaled(21.0, scale);
    }

    private static double ComputeScaled(double baseSize, double scale)
    {
        return (double)System.Math.Round((decimal)baseSize * (decimal)scale, 1, System.MidpointRounding.AwayFromZero);
    }
}