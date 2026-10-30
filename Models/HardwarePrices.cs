using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OpenWidget.Models;

public static class PriceStatuses
{
    public const string Estimated = "Estimated";
    public const string Manual = "Manual";
    public const string Live = "Live";
    public const string Stale = "Stale";
    public const string Unavailable = "Unavailable";
}

public sealed class PriceEntry
{
    public decimal Value { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = PriceStatuses.Estimated;
    public string? ProductId { get; set; }
    public string? ProductUrl { get; set; }
    public DateTime? LastCheckedUtc { get; set; }
    public DateTime? LastSuccessfulUpdateUtc { get; set; }
}

public class HardwarePrices
{
    public string Source { get; set; } = "Estimated";
    public string LastUpdated { get; set; } = string.Empty;
    public string Currency { get; set; } = "₺";
    public Dictionary<string, decimal> Items { get; set; } = new();
    public Dictionary<string, PriceEntry> Entries { get; set; } = new();

    public static HardwarePrices Load(string path)
    {
        if (!File.Exists(path))
        {
            return GetDefaults();
        }

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var prices = JsonSerializer.Deserialize<HardwarePrices>(json, options) ?? GetDefaults();
        prices.MigrateLegacyItems();
        return prices;
    }

    public void Save(string path)
    {
        MigrateLegacyItems();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, json);
    }

    public static HardwarePrices GetDefaults()
    {
        var prices = new HardwarePrices
        {
            Items = new Dictionary<string, decimal>
            {
                ["cpu"] = 8790,
                ["gpu"] = 13450,
                ["motherboard"] = 5290,
                ["ram"] = 5850,
                ["ssd_nvme"] = 2750,
                ["ssd_sata"] = 1290,
                ["monitor"] = 14490,
                ["keyboard"] = 2490,
                ["mouse"] = 3650,
                ["headset"] = 4390,
                ["microphone"] = 5690,
                ["cooler"] = 5350,
                ["psu"] = 7590
            }
        };
        prices.MigrateLegacyItems();
        return prices;
    }

    private void MigrateLegacyItems()
    {
        foreach (var item in Items)
        {
            if (!Entries.ContainsKey(item.Key))
            {
                Entries[item.Key] = new PriceEntry
                {
                    Value = item.Value,
                    Currency = Currency,
                    Source = string.IsNullOrWhiteSpace(Source) ? PriceStatuses.Estimated : Source,
                    Status = PriceStatuses.Estimated
                };
            }
        }

        foreach (var entry in Entries)
        {
            Items[entry.Key] = entry.Value.Value;
        }

        if (string.Equals(Source, "Amazon.com.tr", StringComparison.OrdinalIgnoreCase))
        {
            Source = PriceStatuses.Estimated;
        }
    }
}
