using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows;
using OpenWidget.Models;

namespace OpenWidget.Services;

public class HardwareScanner
{
    private readonly HardwareConfig _config;
    private readonly PriceService _priceService;

    public HardwareScanner(HardwareConfig config, PriceService priceService)
    {
        _config = config;
        _priceService = priceService;
    }

    public HardwareSpecs Scan()
    {
        var specs = new HardwareSpecs();

        // 1. Core System
        specs.CoreSystem.Add(GetCpuItem());
        specs.CoreSystem.Add(GetGpuItem());
        specs.CoreSystem.Add(GetMotherboardItem());
        specs.CoreSystem.Add(GetMemoryItem());

        // 2. Storage
        var storageDrives = GetStorageItems();
        foreach (var drive in storageDrives)
        {
            specs.Storage.Add(drive);
        }

        // 3. Peripherals
        specs.Peripherals.Add(GetMonitorItem());
        specs.Peripherals.Add(GetKeyboardItem());
        specs.Peripherals.Add(GetMouseItem());
        specs.Peripherals.Add(GetMousepadItem());
        specs.Peripherals.Add(GetHeadsetItem());
        specs.Peripherals.Add(GetMicrophoneItem());

        // 4. Cooling and Power
        var caseName = _config.CoolingAndPower.PcCase ?? "MSI MAG FORGE M100R";
        specs.CoolingAndPower.Add(new HardwareItem(
            Category: "Kasa",
            IconKey: "IconCase",
            Label: "Kasa",
            Value: caseName,
            SubValue: "Temperli Cam • 4x ARGB Fan mATX",
            PriceFormatted: _priceService.GetPriceFormatted("case"),
            Url: "https://www.amazon.com.tr/dp/B09VCC46F7",
            LinkDisplay: "amazon.com.tr ↗"
        ));

        specs.CoolingAndPower.Add(new HardwareItem(
            Category: "Soğutma",
            IconKey: "IconCooler",
            Label: "Soğutucu",
            Value: "Corsair iCUE LINK TITAN 240",
            SubValue: "CW-9061016-WW • 240mm AIO",
            PriceFormatted: _priceService.GetPriceFormatted("cooler"),
            Url: "https://www.amazon.com.tr/dp/B0DC21M89J",
            LinkDisplay: "amazon.com.tr ↗"
        ));

        specs.CoolingAndPower.Add(new HardwareItem(
            Category: "Güç",
            IconKey: "IconPsu",
            Label: "PSU",
            Value: "MSI MPG A750GF White",
            SubValue: "750W 80+ Gold Modüler",
            PriceFormatted: _priceService.GetPriceFormatted("psu"),
            Url: "https://www.amazon.com.tr/dp/B0991WL16F",
            LinkDisplay: "amazon.com.tr ↗"
        ));

        // 5. Accessories
        specs.Accessories.Add(new HardwareItem(
            Category: "Aksesuar",
            IconKey: "IconArm",
            Label: "Monitör Kolu",
            Value: _config.Accessories.MonitorArm,
            SubValue: "Amortisörlü VESA Kol (2-9 kg)",
            PriceFormatted: _priceService.GetPriceFormatted("monitor_arm"),
            Url: "https://www.amazon.com.tr/dp/B01AI2YGK4",
            LinkDisplay: "amazon.com.tr ↗"
        ));

        specs.Accessories.Add(new HardwareItem(
            Category: "Aksesuar",
            IconKey: "IconArm",
            Label: "Mikrofon Kolu",
            Value: _config.Accessories.MicArm,
            SubValue: "Mafsallı Akrobat Masa Standı",
            PriceFormatted: _priceService.GetPriceFormatted("mic_arm"),
            Url: "https://www.amazon.com.tr/dp/B07NSS429L",
            LinkDisplay: "amazon.com.tr ↗"
        ));

        specs.Accessories.Add(new HardwareItem(
            Category: "Aksesuar",
            IconKey: "IconGamepad",
            Label: "Gamepad",
            Value: _config.Accessories.Gamepad,
            SubValue: "9. Nesil • Microsoft TR Garantili",
            PriceFormatted: _priceService.GetPriceFormatted("gamepad"),
            Url: "https://www.amazon.com.tr/Xbox-Wireless-Controller-Microsoft-Garantili/dp/B0F2NC69KK",
            LinkDisplay: "amazon.com.tr ↗"
        ));

        specs.Accessories.Add(new HardwareItem(
            Category: "Aksesuar",
            IconKey: "IconNetwork",
            Label: "Ağ Adaptörü",
            Value: _config.Accessories.EthernetAdapter,
            SubValue: "1000 Mbps USB 3.0 Gigabit",
            PriceFormatted: _priceService.GetPriceFormatted("ethernet_adapter"),
            Url: "https://www.amazon.com.tr/dp/B00MYTSN18",
            LinkDisplay: "amazon.com.tr ↗"
        ));

        specs.Accessories.Add(new HardwareItem(
            Category: "Aksesuar",
            IconKey: "IconCable",
            Label: "Ağ Kablosu",
            Value: _config.Accessories.EthernetCable,
            SubValue: "Cat 7 10Gbit/s Siyah Kablo",
            PriceFormatted: _priceService.GetPriceFormatted("ethernet_cable"),
            Url: "https://www.amazon.com.tr/dp/B018M7ST42",
            LinkDisplay: "amazon.com.tr ↗"
        ));

        // Summary Values
        specs.TotalPriceFormatted = _priceService.GetTotalPriceFormatted();
        specs.LastUpdatedText = _priceService.GetLastUpdatedFormatted();
        specs.PriceSource = _priceService.GetSource();

        return specs;
    }

    private HardwareItem GetCpuItem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                var rawName = obj["Name"]?.ToString()?.Trim() ?? "AMD Ryzen 7 5700X";
                var cleanName = rawName
                    .Replace(" 8-Core Processor", "")
                    .Replace(" 6-Core Processor", "")
                    .Replace(" 12-Core Processor", "")
                    .Replace(" 16-Core Processor", "")
                    .Trim();

                var cores = obj["NumberOfCores"]?.ToString() ?? "8";
                var threads = obj["NumberOfLogicalProcessors"]?.ToString() ?? "16";

                return new HardwareItem("Sistem", "IconCpu", "İşlemci", cleanName, $"{cores} Çekirdek • {threads} İzlek (AM4)", _priceService.GetPriceFormatted("cpu"), "https://www.amazon.com.tr/dp/B09VCHR1VH", "amazon.com.tr ↗");
            }
        }
        catch { }
        return new HardwareItem("Sistem", "IconCpu", "İşlemci", "AMD Ryzen 7 5700X", "8 Çekirdek • 16 İzlek (AM4)", _priceService.GetPriceFormatted("cpu"), "https://www.amazon.com.tr/dp/B09VCHR1VH", "amazon.com.tr ↗");
    }

    private HardwareItem GetGpuItem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
            string bestGpu = string.Empty;
            ulong maxRam = 0;

            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(name)) continue;

                if (name.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase)) continue;

                ulong ram = 0;
                if (obj["AdapterRAM"] != null)
                {
                    ulong.TryParse(obj["AdapterRAM"].ToString(), out ram);
                }

                if (string.IsNullOrEmpty(bestGpu) || ram > maxRam ||
                    name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("RTX", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Arc", StringComparison.OrdinalIgnoreCase))
                {
                    bestGpu = name;
                    maxRam = ram;
                }
            }

            if (!string.IsNullOrEmpty(bestGpu))
            {
                string cleanGpu = bestGpu
                    .Replace("NVIDIA ", "")
                    .Replace("AMD ", "")
                    .Trim();

                double vramGb = maxRam > 0 ? Math.Round((double)maxRam / (1024 * 1024 * 1024)) : 0;
                string vramText = vramGb > 0 ? $"{vramGb:F0} GB VRAM" : "Ekran Kartı";

                return new HardwareItem(
                    Category: "Sistem",
                    IconKey: "IconGpu",
                    Label: "Ekran Kartı",
                    Value: cleanGpu,
                    SubValue: vramText,
                    PriceFormatted: _priceService.GetPriceFormatted("gpu")
                );
            }
        }
        catch { }

        return new HardwareItem("Sistem", "IconGpu", "Ekran Kartı", "Harici Ekran Kartı", "GPU", _priceService.GetPriceFormatted("gpu"));
    }

    private HardwareItem GetMotherboardItem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                var mfg = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                var prod = obj["Product"]?.ToString()?.Trim() ?? "";

                if (mfg.Contains("Micro-Star", StringComparison.OrdinalIgnoreCase)) mfg = "MSI";
                if (mfg.Contains("ASUSTeK", StringComparison.OrdinalIgnoreCase)) mfg = "ASUS";

                var full = string.IsNullOrEmpty(mfg) ? prod : $"{mfg} {prod}";
                full = full.Replace(" (MS-7C95)", "").Trim();

                return new HardwareItem("Sistem", "IconMotherboard", "Anakart", full, "Anakart", _priceService.GetPriceFormatted("motherboard"));
            }
        }
        catch { }
        return new HardwareItem("Sistem", "IconMotherboard", "Anakart", "Sistem Anakartı", "Mainboard", _priceService.GetPriceFormatted("motherboard"));
    }

    private HardwareItem GetMemoryItem()
    {
        try
        {
            ulong totalBytes = 0;
            int stickCount = 0;
            uint speed = 0;
            using var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed FROM Win32_PhysicalMemory");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["Capacity"] != null && ulong.TryParse(obj["Capacity"].ToString(), out var cap))
                {
                    totalBytes += cap;
                    stickCount++;
                }
                if (speed == 0 && obj["Speed"] != null && uint.TryParse(obj["Speed"].ToString(), out var sp))
                {
                    speed = sp;
                }
            }

            if (totalBytes > 0)
            {
                double totalGb = Math.Round((double)totalBytes / (1024 * 1024 * 1024));
                string stickText = stickCount > 1 ? $"{stickCount}x Modül" : "Tek Modül";
                string speedText = speed > 0 ? $"{speed} MHz" : "DDR";

                return new HardwareItem(
                    Category: "Sistem",
                    IconKey: "IconRam",
                    Label: "RAM",
                    Value: $"{totalGb:F0} GB RAM ({stickText})",
                    SubValue: speedText,
                    PriceFormatted: _priceService.GetPriceFormatted("ram")
                );
            }
        }
        catch { }

        return new HardwareItem("Sistem", "IconRam", "RAM", "32 GB RAM", "DDR4", _priceService.GetPriceFormatted("ram"));
    }

    private List<HardwareItem> GetStorageItems()
    {
        var items = new List<HardwareItem>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Model, Size, MediaType FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
            {
                var model = obj["Model"]?.ToString()?.Trim() ?? "Disk";

                if (model.Contains("SNV2S1000G", StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new HardwareItem(
                        Category: "Depolama",
                        IconKey: "IconStorage",
                        Label: "SSD",
                        Value: "Kingston NV2 (SNV2S1000G)",
                        SubValue: "1 TB PCIe 4.0 NVMe M.2",
                        PriceFormatted: _priceService.GetPriceFormatted("ssd_nvme"),
                        Url: "https://www.amazon.com.tr/dp/B0BBWJH1HG",
                        LinkDisplay: "amazon.com.tr ↗"
                    ));
                }
                else if (model.StartsWith("SSD", StringComparison.OrdinalIgnoreCase) || model.Contains("512GB", StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new HardwareItem(
                        Category: "Depolama",
                        IconKey: "IconStorage",
                        Label: "SSD",
                        Value: "Izoly S280 Gaming SSD",
                        SubValue: "512 GB SATA3 550/520",
                        PriceFormatted: _priceService.GetPriceFormatted("ssd_sata"),
                        Url: "https://izoly.com/izoly-512gb-gaming-sata3-s280-550-520mb-s-ssd/",
                        LinkDisplay: "izoly.com ↗"
                    ));
                }
            }
        }
        catch { }

        if (items.Count == 0)
        {
            items.Add(new HardwareItem("Depolama", "IconStorage", "SSD", "Kingston NV2 (SNV2S1000G)", "1 TB PCIe 4.0 NVMe M.2", _priceService.GetPriceFormatted("ssd_nvme"), "https://www.amazon.com.tr/dp/B0BBWJH1HG", "amazon.com.tr ↗"));
            items.Add(new HardwareItem("Depolama", "IconStorage", "SSD", "Izoly S280 Gaming SSD", "512 GB SATA3 550/520", _priceService.GetPriceFormatted("ssd_sata"), "https://izoly.com/izoly-512gb-gaming-sata3-s280-550-520mb-s-ssd/", "izoly.com ↗"));
        }

        return items;
    }

    private HardwareItem GetMonitorItem()
    {
        if (!string.IsNullOrEmpty(_config.Peripherals.Monitor))
        {
            return new HardwareItem("Çevre Birimleri", "IconMonitor", "Monitör", _config.Peripherals.Monitor, "27\" 180Hz 1440p QHD", _priceService.GetPriceFormatted("monitor"), "https://www.amazon.com.tr/dp/B0CYZ2B3D8", "amazon.com.tr ↗");
        }

        int resX = (int)SystemParameters.PrimaryScreenWidth;
        int resY = (int)SystemParameters.PrimaryScreenHeight;
        string resText = $"{resX}x{resY} Çözünürlük";

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT UserFriendlyName FROM WmiMonitorID");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["UserFriendlyName"] is ushort[] nameBytes)
                {
                    var chars = nameBytes.Where(b => b != 0).Select(b => (char)b).ToArray();
                    var name = new string(chars).Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        return new HardwareItem("Çevre Birimleri", "IconMonitor", "Monitör", name, resText, _priceService.GetPriceFormatted("monitor"));
                    }
                }
            }
        }
        catch { }

        return new HardwareItem("Çevre Birimleri", "IconMonitor", "Monitör", "Birincil Ekran", resText, _priceService.GetPriceFormatted("monitor"));
    }

    private HardwareItem GetKeyboardItem()
    {
        var name = _config.Peripherals.Keyboard ?? "Corsair K55 RGB PRO XT";
        return new HardwareItem("Çevre Birimleri", "IconKeyboard", "Klavye", name, "Per-Key RGB Gaming", _priceService.GetPriceFormatted("keyboard"), "https://www.amazon.com.tr/dp/B08YC76FRP", "amazon.com.tr ↗");
    }

    private HardwareItem GetMouseItem()
    {
        var name = _config.Peripherals.Mouse ?? "ROG Harpe Ace Aim Lab";
        return new HardwareItem("Çevre Birimleri", "IconMouse", "Fare", name, "Ultralight 54g Wireless", _priceService.GetPriceFormatted("mouse"), "https://www.amazon.com.tr/dp/B0BQ7D9B4Y", "amazon.com.tr ↗");
    }

    private HardwareItem GetMousepadItem()
    {
        var name = _config.Peripherals.Mousepad ?? "Wraith Spirit of Aim";
        return new HardwareItem("Çevre Birimleri", "IconMousepad", "Mousepad", name, "Speed / White • 470x420x3mm", _priceService.GetPriceFormatted("mousepad"), "https://wraithesports.com/products/wraith-spirit-of-aim-mousepad?variant=47478471262401", "wraithesports.com ↗");
    }

    private HardwareItem GetHeadsetItem()
    {
        var name = _config.Peripherals.Headset ?? "Razer Kraken Kitty V2 BT";
        return new HardwareItem("Çevre Birimleri", "IconHeadset", "Kulaklık", name, "Bluetooth 5.2 • 7.1 Surround", _priceService.GetPriceFormatted("headset"), "https://www.amazon.com.tr/dp/B0CC9584V9", "amazon.com.tr ↗");
    }

    private HardwareItem GetMicrophoneItem()
    {
        var name = _config.Peripherals.Microphone ?? "Razer Seiren V3 Chroma";
        return new HardwareItem("Çevre Birimleri", "IconMic", "Mikrofon", name, "USB Kondenser & Chroma RGB", _priceService.GetPriceFormatted("microphone"), "https://www.amazon.com.tr/dp/B0CRH7Q3L1", "amazon.com.tr ↗");
    }
}
