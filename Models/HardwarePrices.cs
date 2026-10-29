using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RigBadge.Models;

public class HardwarePrices
{
    public string LastUpdated { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string Currency { get; set; } = "₺";
    public Dictionary<string, decimal> Items { get; set; } = new();

    public static HardwarePrices Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var prices = JsonSerializer.Deserialize<HardwarePrices>(json, options);
                if (prices != null) return prices;
            }
        }
        catch { }

        return GetDefaults();
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

    public static HardwarePrices GetDefaults()
    {
        return new HardwarePrices
        {
            LastUpdated = DateTime.Today.ToString("yyyy-MM-dd"),
            Currency = "₺",
            Items = new Dictionary<string, decimal>
            {
                ["cpu"] = 8250,
                ["gpu"] = 12800,
                ["motherboard"] = 5200,
                ["ram"] = 5550,
                ["ssd_nvme"] = 2650,
                ["ssd_sata"] = 1250,
                ["monitor"] = 14320,
                ["keyboard"] = 2450,
                ["mouse"] = 3570,
                ["headset"] = 4250,
                ["microphone"] = 5600,
                ["cooler"] = 5200,
                ["psu"] = 7490
            }
        };
    }
}
