using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows;
using RigBadge.Models;

namespace RigBadge.Services;

public class HardwareScanner
{
    private readonly HardwareConfig _config;

    public HardwareScanner(HardwareConfig config)
    {
        _config = config;
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
        specs.Peripherals.Add(GetHeadsetItem());
        specs.Peripherals.Add(GetMicrophoneItem());

        // 4. Cooling and Power
        specs.CoolingAndPower.Add(new HardwareItem(
            Category: "Soğutma",
            IconKey: "IconCooler",
            Label: "Sıvı Soğutma",
            Value: _config.CoolingAndPower.CpuCooler,
            SubValue: "240mm AIO Sıvı Soğutucu"
        ));

        specs.CoolingAndPower.Add(new HardwareItem(
            Category: "Güç",
            IconKey: "IconPsu",
            Label: "Güç Kaynağı (PSU)",
            Value: _config.CoolingAndPower.Psu,
            SubValue: "750W 80+ Gold Modüler"
        ));

        return specs;
    }

    private HardwareItem GetCpuItem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                var rawName = obj["Name"]?.ToString()?.Trim() ?? "Bilinmeyen İşlemci";
                var cleanName = CleanCpuName(rawName);
                var cores = obj["NumberOfCores"]?.ToString();
                var threads = obj["NumberOfLogicalProcessors"]?.ToString();
                var sub = (cores != null && threads != null) ? $"{cores} Çekirdek / {threads} İzlek" : null;

                return new HardwareItem("Sistem", "IconCpu", "İşlemci (CPU)", cleanName, sub);
            }
        }
        catch
        {
            // Fallback
        }
        return new HardwareItem("Sistem", "IconCpu", "İşlemci (CPU)", "AMD Ryzen 7 5700X", "8 Çekirdek / 16 İzlek");
    }

    private string CleanCpuName(string name)
    {
        // Strip common redundant suffixes
        return name
            .Replace(" 8-Core Processor", "")
            .Replace(" 6-Core Processor", "")
            .Replace(" 12-Core Processor", "")
            .Replace(" 16-Core Processor", "")
            .Replace(" Six-Core Processor", "")
            .Replace(" Eight-Core Processor", "")
            .Trim();
    }

    private HardwareItem GetGpuItem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            var gpus = new List<string>();
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name) &&
                    !name.Contains("Remote", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("Virtual", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase))
                {
                    gpus.Add(name);
                }
            }

            var primaryGpu = gpus.FirstOrDefault(g => g.Contains("RTX") || g.Contains("GeForce") || g.Contains("Radeon"))
                             ?? gpus.FirstOrDefault()
                             ?? "NVIDIA GeForce RTX 3060";

            return new HardwareItem("Sistem", "IconGpu", "Ekran Kartı (GPU)", primaryGpu, "Ayrık Grafik Kartı");
        }
        catch
        {
            return new HardwareItem("Sistem", "IconGpu", "Ekran Kartı (GPU)", "NVIDIA GeForce RTX 3060", "Ayrık Grafik Kartı");
        }
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
                return new HardwareItem("Sistem", "IconMotherboard", "Anakart", full, "AM4 Platform");
            }
        }
        catch
        {
            // Fallback
        }
        return new HardwareItem("Sistem", "IconMotherboard", "Anakart", "MSI B550M PRO-VDH WIFI", "AM4 Platform");
    }

    private HardwareItem GetMemoryItem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed, Manufacturer, PartNumber FROM Win32_PhysicalMemory");
            long totalBytes = 0;
            int count = 0;
            uint speed = 0;
            string? mfg = null;

            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["Capacity"] != null && long.TryParse(obj["Capacity"].ToString(), out var bytes))
                {
                    totalBytes += bytes;
                    count++;
                }
                if (obj["Speed"] != null && uint.TryParse(obj["Speed"].ToString(), out var spd) && spd > speed)
                {
                    speed = spd;
                }
                if (string.IsNullOrEmpty(mfg) && obj["Manufacturer"] != null)
                {
                    mfg = obj["Manufacturer"].ToString()?.Trim();
                }
            }

            var totalGb = (int)Math.Round((double)totalBytes / (1024 * 1024 * 1024));
            var stickGb = count > 0 ? totalGb / count : 16;
            var speedStr = speed > 0 ? $"{speed} MHz" : "DDR4";
            var brandStr = !string.IsNullOrEmpty(mfg) ? mfg : "Kingston";

            var value = $"{totalGb} GB DDR4";
            var subValue = $"{count}x {stickGb} GB ({brandStr} @ {speedStr})";

            return new HardwareItem("Sistem", "IconRam", "Bellek (RAM)", value, subValue);
        }
        catch
        {
            return new HardwareItem("Sistem", "IconRam", "Bellek (RAM)", "48 GB DDR4", "3x 16 GB Kingston @ 3600 MHz");
        }
    }

    private List<HardwareItem> GetStorageItems()
    {
        var items = new List<HardwareItem>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Model, Size, MediaType FROM Win32_DiskDrive");
            int driveIndex = 1;
            foreach (ManagementObject obj in searcher.Get())
            {
                var model = obj["Model"]?.ToString()?.Trim() ?? $"Sürücü #{driveIndex}";
                long size = 0;
                if (obj["Size"] != null) long.TryParse(obj["Size"].ToString(), out size);

                var sizeGb = (int)Math.Round((double)size / (1000 * 1000 * 1000));
                string sizeStr;
                if (sizeGb >= 900 && sizeGb <= 1100) sizeStr = "1 TB";
                else if (sizeGb >= 1900 && sizeGb <= 2100) sizeStr = "2 TB";
                else sizeStr = $"{sizeGb} GB";

                string type = "NVMe / SSD";
                string cleanModel = model;

                if (model.Contains("SNV2S1000G", StringComparison.OrdinalIgnoreCase))
                {
                    cleanModel = "Kingston NV2 NVMe SSD";
                    type = "M.2 PCIe 4.0";
                }
                else if (model.StartsWith("SSD", StringComparison.OrdinalIgnoreCase))
                {
                    cleanModel = "SATA III High-Speed SSD";
                    type = "2.5\" SSD";
                }

                items.Add(new HardwareItem(
                    Category: "Depolama",
                    IconKey: "IconStorage",
                    Label: $"Depolama ({sizeStr})",
                    Value: cleanModel,
                    SubValue: $"{sizeStr} • {type}"
                ));
                driveIndex++;
            }
        }
        catch
        {
            // Fallback to detected values
        }

        if (items.Count == 0)
        {
            items.Add(new HardwareItem("Depolama", "IconStorage", "Depolama (1 TB)", "Kingston NV2 NVMe SSD", "1 TB • M.2 PCIe 4.0"));
            items.Add(new HardwareItem("Depolama", "IconStorage", "Depolama (512 GB)", "SATA III SSD", "512 GB • 2.5\" SSD"));
        }

        return items;
    }

    private HardwareItem GetMonitorItem()
    {
        if (!string.IsNullOrEmpty(_config.Peripherals.Monitor))
        {
            return new HardwareItem("Çevre Birimleri", "IconMonitor", "Monitör", _config.Peripherals.Monitor, "Birincil Ekran");
        }

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
                        var width = (int)SystemParameters.PrimaryScreenWidth;
                        var height = (int)SystemParameters.PrimaryScreenHeight;
                        var resStr = (width > 0 && height > 0) ? $"{width}x{height} QHD" : "Curved Gaming";

                        return new HardwareItem("Çevre Birimleri", "IconMonitor", "Monitör", name, resStr);
                    }
                }
            }
        }
        catch
        {
            // Ignore WMI WmiMonitorID permission / availability issues
        }

        return new HardwareItem("Çevre Birimleri", "IconMonitor", "Monitör", "MSI MAG 27CQ6F", "2560x1440 180Hz Curved");
    }

    private HardwareItem GetKeyboardItem()
    {
        var name = _config.Peripherals.Keyboard ?? "Corsair K55 RGB PRO XT";
        return new HardwareItem("Çevre Birimleri", "IconKeyboard", "Klavye", name, "RGB Gaming Keyboard");
    }

    private HardwareItem GetMouseItem()
    {
        var name = _config.Peripherals.Mouse ?? "ROG Harpe Ace Aim Lab Edition";
        return new HardwareItem("Çevre Birimleri", "IconMouse", "Fare", name, "Wireless Ultralight 54g");
    }

    private HardwareItem GetHeadsetItem()
    {
        var name = _config.Peripherals.Headset ?? "Razer Kraken Kitty V2 BT";
        return new HardwareItem("Çevre Birimleri", "IconHeadset", "Kulaklık", name, "7.1 Surround Sound");
    }

    private HardwareItem GetMicrophoneItem()
    {
        var name = _config.Peripherals.Microphone ?? "Razer Seiren V3 Chroma";
        return new HardwareItem("Çevre Birimleri", "IconMic", "Mikrofon", name, "USB Kondenser & Chroma RGB");
    }
}
