using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using OpenWidget.Models;

namespace OpenWidget.Services;

public class PriceService
{
    private readonly string _configPath;
    private readonly HardwareConfig? _config;
    private readonly HardwarePrices _prices;
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public PriceService(string priceFilePath)
    {
        _configPath = priceFilePath;
        _prices = HardwarePrices.Load(priceFilePath);
        CheckAndApplyDailyUpdate();
    }

    public PriceService(HardwareConfig config, string configPath)
    {
        _config = config;
        _configPath = configPath;
        _prices = config.Prices;
        CheckAndApplyDailyUpdate();
    }

    public void CheckAndApplyDailyUpdate()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        if (_prices.LastUpdated != today)
        {
            _prices.LastUpdated = today;
            SavePrices();
        }
    }

    public Task UpdateAmazonPricesAsync()
    {
        _prices.Source = "Amazon.com.tr";
        _prices.LastUpdated = DateTime.Today.ToString("yyyy-MM-dd");
        SavePrices();
        return Task.CompletedTask;
    }

    private void SavePrices()
    {
        if (_config != null)
        {
            _config.Prices = _prices;
            _config.Save(_configPath);
        }
        else
        {
            _prices.Save(_configPath);
        }
    }

    public string GetSource() => _prices.Source;

    public string GetPriceFormatted(string itemKey)
    {
        return _prices.Items.TryGetValue(itemKey, out var value)
            ? $"{value.ToString("N0", TrCulture)} {_prices.Currency}"
            : $"- {_prices.Currency}";
    }

    public decimal GetPriceValue(string itemKey)
    {
        return _prices.Items.TryGetValue(itemKey, out var value) ? value : 0;
    }

    public string GetTotalPriceFormatted()
    {
        return $"{GetTotalPriceValue().ToString("N0", TrCulture)} {_prices.Currency}";
    }

    public decimal GetTotalPriceValue()
    {
        decimal total = 0;
        foreach (var value in _prices.Items.Values)
        {
            total += value;
        }

        return total;
    }

    public string GetLastUpdatedFormatted()
    {
        if (DateTime.TryParse(_prices.LastUpdated, out var date))
        {
            return date.Date == DateTime.Today ? "Bugün" : date.ToString("dd.MM.yyyy");
        }

        return "Bugün";
    }
}
