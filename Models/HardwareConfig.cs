using System.IO;
using System.Collections.Generic;
using System.Text.Json;

namespace OpenWidget.Models;

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
    public string? Mousepad { get; set; }
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
    public double OffsetX { get; set; } = 28;
    public double OffsetY { get; set; } = 28;
    public double? CustomLeft { get; set; }
    public double? CustomTop { get; set; }
    public double Width { get; set; } = 360;
    public double Height { get; set; } = 780;
    public bool AlwaysOnTop { get; set; } = false;
    public bool PinToDesktop { get; set; } = false;
    public bool AutoGameMode { get; set; } = true;
    public bool IsLocked { get; set; } = false;
    public bool IsCompact { get; set; } = false;
    public bool UseModularCards { get; set; } = false;
    public bool LimitClipboardHeight { get; set; } = true;
    public double ClipboardHeight { get; set; } = 220;
    public bool IsClipboardHistoryExpanded { get; set; } = true;
    public bool OnlyShowOnDesktop { get; set; } = false;
    public string Language { get; set; } = "tr";
    public string City { get; set; } = "auto";
    public string AccentTheme { get; set; } = "Sky";
    public string Currency { get; set; } = "TRY";
}

public class WidgetSelectionConfig
{
    public bool ShowHeader { get; set; } = true;
    public bool ShowWeather { get; set; } = true;
    public bool ShowClipboard { get; set; } = true;
    public bool ShowTelemetry { get; set; } = true;
    public bool ShowFinance { get; set; } = true;
    public bool ShowHardware { get; set; } = true;
    public bool ShowMedia { get; set; } = true;
    public bool ShowQuickActions { get; set; } = true;
}

public class HardwareVisibilityConfig
{
    public bool CoreSystem { get; set; } = true;
    public bool Storage { get; set; } = true;
    public bool Peripherals { get; set; } = true;
    public bool CoolingAndPower { get; set; } = true;
    public bool Accessories { get; set; } = true;
}

public class ManualHardwareItem
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = HardwareCategory.Accessories;
    public decimal Price { get; set; }
    public string AmazonUrl { get; set; } = string.Empty;
}

public static class HardwareCategory
{
    public const string CoreSystem = "CoreSystem";
    public const string Storage = "Storage";
    public const string Peripherals = "Peripherals";
    public const string CoolingAndPower = "CoolingAndPower";
    public const string Accessories = "Accessories";

    public static string Normalize(string? category) => category switch
    {
        "Sistem" or CoreSystem => CoreSystem,
        "Depolama" or Storage => Storage,
        "Çevre Birimleri" or Peripherals => Peripherals,
        "Soğutma ve Güç" or CoolingAndPower => CoolingAndPower,
        "Aksesuar" or Accessories => Accessories,
        _ => Accessories
    };
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
    public double OffsetY { get; set; } = 205;
    public double Width { get; set; } = 260;
}

public class HardwareConfig
{
    public CoolingAndPowerConfig CoolingAndPower { get; set; } = new();
    public PeripheralsConfig Peripherals { get; set; } = new();
    public AccessoriesConfig Accessories { get; set; } = new();
    public WidgetConfig Widget { get; set; } = new();
    public WidgetSelectionConfig Widgets { get; set; } = new();
    public TelemetryWidgetConfig TelemetryWidget { get; set; } = new();
    public FinanceWidgetConfig FinanceWidget { get; set; } = new();
    public HardwarePrices Prices { get; set; } = HardwarePrices.GetDefaults();
    public PriceProviderConfig PriceProvider { get; set; } = new();
    public HardwareVisibilityConfig HardwareVisibility { get; set; } = new();
    public List<ManualHardwareItem> ManualHardware { get; set; } = new();
    public List<string> HiddenHardware { get; set; } = new();
    public Dictionary<string, string> HardwareLinks { get; set; } = new();

    public static HardwareConfig Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<HardwareConfig>(json, options);
                if (config != null)
                {
                    foreach (var item in config.ManualHardware)
                    {
                        item.Category = HardwareCategory.Normalize(item.Category);
                    }

                    // Sync legacy flags if present
                    if (!config.TelemetryWidget.Enabled) config.Widgets.ShowTelemetry = false;
                    if (!config.FinanceWidget.Enabled) config.Widgets.ShowFinance = false;
                    return config;
                }
            }

        }
        catch { }

        return new HardwareConfig();
    }

    public void Save(string path)
    {
        try
        {
            // Sync legacy
            TelemetryWidget.Enabled = Widgets.ShowTelemetry;
            FinanceWidget.Enabled = Widgets.ShowFinance;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving config to {path}: {ex.Message}");
        }
    }
}

public class PriceProviderConfig
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxAgeHours { get; set; } = 24;
}
