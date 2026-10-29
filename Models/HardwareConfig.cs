using System.IO;
using System.Text.Json;

namespace RigBadge.Models;

public class CoolingAndPowerConfig
{
    public string CpuCooler { get; set; } = "CORSAIR iCUE LINK TITAN 240 RX RGB AIO";
    public string Psu { get; set; } = "MSI MPG A750GF White 750W 80+ Gold";
}

public class PeripheralsConfig
{
    public string? Keyboard { get; set; } = "Corsair K55 RGB PRO XT";
    public string? Mouse { get; set; } = "ROG Harpe Ace Aim Lab Edition";
    public string? Headset { get; set; } = "Razer Kraken Kitty V2 BT";
    public string? Microphone { get; set; } = "Razer Seiren V3 Chroma";
    public string? Monitor { get; set; }
}

public class WidgetConfig
{
    public double Opacity { get; set; } = 0.94;
    public double OffsetX { get; set; } = 24;
    public double OffsetY { get; set; } = 24;
    public double Width { get; set; } = 380;
    public double Height { get; set; } = 780;
    public bool AlwaysOnTop { get; set; } = false;
    public bool IsLocked { get; set; } = false;
}

public class HardwareConfig
{
    public CoolingAndPowerConfig CoolingAndPower { get; set; } = new();
    public PeripheralsConfig Peripherals { get; set; } = new();
    public WidgetConfig Widget { get; set; } = new();

    public static HardwareConfig Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<HardwareConfig>(json, options);
                if (config != null) return config;
            }
        }
        catch
        {
            // Fall back to default config if file is corrupted or unreadable
        }
        return new HardwareConfig();
    }
}
