using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
