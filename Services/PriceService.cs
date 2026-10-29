using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RigBadge.Models;

namespace RigBadge.Services;

public class PriceService
{
    private readonly string _priceFilePath;
    private HardwarePrices _prices;
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public PriceService(string priceFilePath)
    {
        _priceFilePath = priceFilePath;
        _prices = HardwarePrices.Load(_priceFilePath);
        CheckAndApplyDailyUpdate();
    }

    public void CheckAndApplyDailyUpdate()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        if (_prices.LastUpdated != today)
        {
            _prices.LastUpdated = today;
            _prices.Save(_priceFilePath);
        }
    }

    public async Task UpdateIzolyPricesAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            // 1. Izoly RAM (16GB 3200MHz)
            var ramHtml = await client.GetStringAsync("https://izoly.com/izoly-16gb-ddr4-3200mhz-cl22-1-2v-gaming-sogutuculu-desktop-ram/");
            var ramPrice = ExtractPrice(ramHtml);

            // 2. Izoly SSD (512GB S280 SATA3)
            var ssdHtml = await client.GetStringAsync("https://izoly.com/izoly-512gb-gaming-sata3-s280-550-520mb-s-ssd/");
            var ssdPrice = ExtractPrice(ssdHtml);

            bool changed = false;
            if (ssdPrice > 0)
            {
                _prices.Items["ssd_sata"] = ssdPrice;
                changed = true;
            }

            if (ramPrice > 0)
            {
                // 2x 16GB Kingston (Amazon.com.tr ~3900 TL) + 1x 16GB Izoly
                decimal kingston2x = 3900m;
                _prices.Items["ram"] = kingston2x + ramPrice;
                changed = true;
            }

            if (changed)
            {
                _prices.LastUpdated = DateTime.Today.ToString("yyyy-MM-dd");
                _prices.Save(_priceFilePath);
            }
        }
        catch
        {
            // Retain saved prices safely on network failure
        }
    }

    private static decimal ExtractPrice(string html)
    {
        var match = Regex.Match(html, @"<p class=""price"">.*?<ins[^>]*>.*?<bdi>([0-9\.,]+)<", RegexOptions.Singleline);
        if (!match.Success)
        {
            match = Regex.Match(html, @"<span class=""screen-reader-text"">Şu andaki fiyat:\s*([0-9\.,]+)", RegexOptions.Singleline);
        }

        if (match.Success)
        {
            var raw = match.Groups[1].Value.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                return price;
            }
        }
        return 0;
    }

    public string GetSource() => _prices.Source;

    public string GetPriceFormatted(string itemKey)
    {
        if (_prices.Items.TryGetValue(itemKey, out var val))
        {
            return $"{val.ToString("N0", TrCulture)} {_prices.Currency}";
        }
        return $"- {_prices.Currency}";
    }

    public decimal GetPriceValue(string itemKey)
    {
        return _prices.Items.TryGetValue(itemKey, out var val) ? val : 0;
    }

    public string GetTotalPriceFormatted()
    {
        decimal total = 0;
        foreach (var p in _prices.Items.Values)
        {
            total += p;
        }
        return $"{total.ToString("N0", TrCulture)} {_prices.Currency}";
    }

    public string GetLastUpdatedFormatted()
    {
        if (DateTime.TryParse(_prices.LastUpdated, out var dt))
        {
            if (dt.Date == DateTime.Today) return "Bugün";
            return dt.ToString("dd.MM.yyyy");
        }
        return "Bugün";
    }
}
