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
            Label: "Soğutucu",
            Value: CleanItemName(_config.CoolingAndPower.CpuCooler, "CORSAIR iCUE LINK TITAN 240 RX"),
            SubValue: "240mm AIO"
        ));

        specs.CoolingAndPower.Add(new HardwareItem(
            Category: "Güç",
            IconKey: "IconPsu",
            Label: "PSU",
            Value: CleanItemName(_config.CoolingAndPower.Psu, "MSI MPG A750GF White"),
            SubValue: "750W Gold"
        ));

        return specs;
    }

    private static string CleanItemName(string? raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        return raw.Replace(" POWER SUPPLY-BEYAZ", "")
                  .Replace(" RGB AIO 240mm İşlemci Soğutucusu (CW-9061016-WW)", "")
                  .Trim();
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

                return new HardwareItem("Sistem", "IconCpu", "İşlemci", cleanName, $"{cores}C / {threads}T");
            }
        }
        catch { }
        return new HardwareItem("Sistem", "IconCpu", "İşlemci", "AMD Ryzen 7 5700X", "8C / 16T");
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

            var cleanGpu = primaryGpu.Replace("NVIDIA ", "").Trim();

            return new HardwareItem("Sistem", "IconGpu", "Ekran Kartı", cleanGpu, "PCIe");
        }
        catch
        {
            return new HardwareItem("Sistem", "IconGpu", "Ekran Kartı", "GeForce RTX 3060", "PCIe");
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
                full = full.Replace(" (MS-7C95)", "").Trim();

                return new HardwareItem("Sistem", "IconMotherboard", "Anakart", full, "AM4");
            }
        }
        catch { }
        return new HardwareItem("Sistem", "IconMotherboard", "Anakart", "MSI B550M PRO-VDH WIFI", "AM4");
    }

    private HardwareItem GetMemoryItem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed, Manufacturer FROM Win32_PhysicalMemory");
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
            var brandStr = !string.IsNullOrEmpty(mfg) ? mfg : "Kingston";
            var speedStr = speed > 0 ? $"{speed} MHz" : "3600 MHz";

            return new HardwareItem("Sistem", "IconRam", "RAM", $"{totalGb} GB {brandStr}", speedStr);
        }
        catch
        {
            return new HardwareItem("Sistem", "IconRam", "RAM", "48 GB Kingston", "3600 MHz");
        }
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
                long size = 0;
                if (obj["Size"] != null) long.TryParse(obj["Size"].ToString(), out size);

                var sizeGb = (int)Math.Round((double)size / (1000 * 1000 * 1000));
                string sizeStr = sizeGb >= 900 && sizeGb <= 1100 ? "1 TB" : $"{sizeGb} GB";

                string cleanModel = model;
                string sub = sizeStr;

                if (model.Contains("SNV2S1000G", StringComparison.OrdinalIgnoreCase))
                {
                    cleanModel = "Kingston NV2 NVMe";
                    sub = "1 TB M.2";
                }
                else if (model.StartsWith("SSD", StringComparison.OrdinalIgnoreCase))
                {
                    cleanModel = "SATA III SSD";
                    sub = "512 GB";
                }

                items.Add(new HardwareItem(
                    Category: "Depolama",
                    IconKey: "IconStorage",
                    Label: "SSD",
                    Value: cleanModel,
                    SubValue: sub
                ));
            }
        }
        catch { }

        if (items.Count == 0)
        {
            items.Add(new HardwareItem("Depolama", "IconStorage", "SSD", "Kingston NV2 NVMe", "1 TB M.2"));
            items.Add(new HardwareItem("Depolama", "IconStorage", "SSD", "SATA III SSD", "512 GB"));
        }

        return items;
    }

    private HardwareItem GetMonitorItem()
    {
        if (!string.IsNullOrEmpty(_config.Peripherals.Monitor))
        {
            return new HardwareItem("Çevre Birimleri", "IconMonitor", "Monitör", _config.Peripherals.Monitor, "1440p");
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
                        return new HardwareItem("Çevre Birimleri", "IconMonitor", "Monitör", name, "180Hz 1440p");
                    }
                }
            }
        }
        catch { }

        return new HardwareItem("Çevre Birimleri", "IconMonitor", "Monitör", "MSI MAG 27CQ6F", "180Hz 1440p");
    }

    private HardwareItem GetKeyboardItem()
    {
        var name = _config.Peripherals.Keyboard ?? "Corsair K55 RGB PRO XT";
        return new HardwareItem("Çevre Birimleri", "IconKeyboard", "Klavye", name, "Klavye");
    }

    private HardwareItem GetMouseItem()
    {
        var name = _config.Peripherals.Mouse ?? "ROG Harpe Ace Aim Lab";
        return new HardwareItem("Çevre Birimleri", "IconMouse", "Fare", name, "Fare");
    }

    private HardwareItem GetHeadsetItem()
    {
        var name = _config.Peripherals.Headset ?? "Razer Kraken Kitty V2 BT";
        return new HardwareItem("Çevre Birimleri", "IconHeadset", "Kulaklık", name, "Kulaklık");
    }

    private HardwareItem GetMicrophoneItem()
    {
        var name = _config.Peripherals.Microphone ?? "Razer Seiren V3 Chroma";
        return new HardwareItem("Çevre Birimleri", "IconMic", "Mikrofon", name, "Mikrofon");
    }
}
