using System.IO;
using System.Text.Json;

namespace RigBadge.Models;

public class CoolingAndPowerConfig
{
    public string PcCase { get; set; } = "MSI MAG FORGE M100R";
    public string CpuCooler { get; set; } = "CORSAIR iCUE LINK TITAN 240 RX RGB AIO";
    public string Psu { get; set; } = "MSI MPG A750GF White 750W 80+ Gold";
}

public class PeripheralsConfig
{
    public string? Keyboard { get; set; } = "Corsair K55 RGB PRO XT";
    public string? Mouse { get; set; } = "ROG Harpe Ace Aim Lab";
    public string? Mousepad { get; set; } = "Wraith Spirit of Aim";
    public string? Headset { get; set; } = "Razer Kraken Kitty V2 BT";
    public string? Microphone { get; set; } = "Razer Seiren V3 Chroma";
    public string? Monitor { get; set; }
}

public class AccessoriesConfig
{
    public string MonitorArm { get; set; } = "North Bayou NB F80";
    public string MicArm { get; set; } = "Lastvoice NB39 Masa Standı";
    public string Gamepad { get; set; } = "Xbox Wireless Controller";
    public string EthernetAdapter { get; set; } = "Ugreen USB 3.0 Gigabit RJ45";
    public string EthernetCable { get; set; } = "KabelDirekt 10Gbit/s (0.5m)";
}

public class WidgetConfig
{
    public double Opacity { get; set; } = 1.0;
    public double OffsetX { get; set; } = 20;
    public double OffsetY { get; set; } = 20;
    public double Width { get; set; } = 380;
    public double Height { get; set; } = 780;
    public bool AlwaysOnTop { get; set; } = false;
    public bool IsLocked { get; set; } = false;
    public bool OnlyShowOnDesktop { get; set; } = true;
}

public class TelemetryWidgetConfig
{
    public bool Enabled { get; set; } = true;
    public double OffsetX { get; set; } = 415;
    public double OffsetY { get; set; } = 20;
    public double Width { get; set; } = 260;
}

public class FinanceWidgetConfig
{
    public bool Enabled { get; set; } = true;
    public double OffsetX { get; set; } = 415;
    public double OffsetY { get; set; } = 275;
    public double Width { get; set; } = 260;
}

public class HardwareConfig
{
    public CoolingAndPowerConfig CoolingAndPower { get; set; } = new();
    public PeripheralsConfig Peripherals { get; set; } = new();
    public AccessoriesConfig Accessories { get; set; } = new();
    public WidgetConfig Widget { get; set; } = new();
    public TelemetryWidgetConfig TelemetryWidget { get; set; } = new();
    public FinanceWidgetConfig FinanceWidget { get; set; } = new();

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
        catch { }

        return new HardwareConfig();
    }

    public void Save(string path)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
