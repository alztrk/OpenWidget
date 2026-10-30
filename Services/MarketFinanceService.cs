using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OpenWidget.Models;

namespace OpenWidget.Services;

public class MarketFinanceService
{
    private readonly string _cacheFilePath;
    private FinanceSpecs _cachedSpecs = new();
    private string _currency = "TRY";
    private string? _rawMarketJson;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    static MarketFinanceService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public MarketFinanceService(string cacheFilePath)
    {
        _cacheFilePath = cacheFilePath;
        LoadCache();
    }

    public FinanceSpecs GetCurrentSpecs() => _cachedSpecs;

    public void SetCurrency(string currency)
    {
        _currency = SupportedCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase)
            ? currency.ToUpperInvariant()
            : "TRY";
        if (!string.IsNullOrWhiteSpace(_rawMarketJson))
        {
            _cachedSpecs = ParseTruncgilJson(_rawMarketJson, _currency, new Dictionary<string, decimal>());
        }
    }

    private static readonly string[] SupportedCurrencies = ["TRY", "USD", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD"];

    public async Task<FinanceSpecs> FetchLatestAsync(string? currency = null)
    {
        if (!string.IsNullOrWhiteSpace(currency))
        {
            SetCurrency(currency);
        }

        var exchangeRates = await FetchExchangeRatesAsync();
        // 1. Currencies & Gold from Truncgil
        try
        {
            var json = await HttpClient.GetStringAsync("https://finans.truncgil.com/v3/today.json");
            _rawMarketJson = json;
            var specs = ParseTruncgilJson(json, _currency, exchangeRates);
            if (specs.Currencies.Count > 0 || specs.Gold.Count > 0)
            {
                _cachedSpecs.Currencies = specs.Currencies;
                _cachedSpecs.Gold = specs.Gold;
                _cachedSpecs.LastUpdatedText = specs.LastUpdatedText;
                File.WriteAllText(_cacheFilePath, json);
            }
        }
        catch { }

        // 2. Crypto from Binance (BTC & ETH)
        try
        {
            var btcJson = await HttpClient.GetStringAsync("https://api.binance.com/api/v3/ticker/24hr?symbol=BTCUSDT");
            var btcItem = ParseBinanceTicker(btcJson, "btc", "IconBitcoin", LocalizationService.Get("Bitcoin"), exchangeRates);

            var ethJson = await HttpClient.GetStringAsync("https://api.binance.com/api/v3/ticker/24hr?symbol=ETHUSDT");
            var ethItem = ParseBinanceTicker(ethJson, "eth", "IconEthereum", LocalizationService.Get("Ethereum"), exchangeRates);

            _cachedSpecs.Crypto = new List<FinanceItem> { btcItem, ethItem };
        }
        catch { }

        if (_cachedSpecs.Crypto.Count == 0)
        {
            _cachedSpecs.Crypto.Add(new FinanceItem("btc", "IconBitcoin", LocalizationService.Get("Bitcoin"), "$--", "%0,00", "Neutral"));
            _cachedSpecs.Crypto.Add(new FinanceItem("eth", "IconEthereum", LocalizationService.Get("Ethereum"), "$--", "%0,00", "Neutral"));
        }

        return _cachedSpecs;
    }

    private void LoadCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                _rawMarketJson = json;
                _cachedSpecs = ParseTruncgilJson(json, _currency, new Dictionary<string, decimal>());
            }
        }
        catch { }

        if (_cachedSpecs.Currencies.Count == 0 && _cachedSpecs.Gold.Count == 0)
        {
            _cachedSpecs = CreateDefaultSpecs();
        }
        else if (_cachedSpecs.Crypto.Count == 0)
        {
            _cachedSpecs.Crypto.Add(new FinanceItem("btc", "IconBitcoin", LocalizationService.Get("Bitcoin"), "$--", "%0,00", "Neutral"));
            _cachedSpecs.Crypto.Add(new FinanceItem("eth", "IconEthereum", LocalizationService.Get("Ethereum"), "$--", "%0,00", "Neutral"));
        }
    }

    private async Task<Dictionary<string, decimal>> FetchExchangeRatesAsync()
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["TRY"] = 1m };
        if (_currency == "TRY")
        {
            return rates;
        }

        try
        {
            using var doc = JsonDocument.Parse(await HttpClient.GetStringAsync("https://api.frankfurter.app/latest?from=TRY"));
            if (doc.RootElement.TryGetProperty("rates", out var values))
            {
                foreach (var property in values.EnumerateObject())
                {
                    if (property.Value.TryGetDecimal(out var value))
                    {
                        rates[property.Name] = value;
                    }
                }
            }
        }
        catch
        {
            // Keep TRY values when conversion data is unavailable.
        }

        return rates;
    }

    private static FinanceSpecs ParseTruncgilJson(string json, string currency, IReadOnlyDictionary<string, decimal> rates)
    {
        var specs = new FinanceSpecs();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Update_Date", out var updateDate))
            {
                specs.LastUpdatedText = updateDate.GetString() ?? "Canlı Piyasa";
            }

            // 1. USD
            if (root.TryGetProperty("USD", out var usd))
            {
                specs.Currencies.Add(CreateItem("usd", "IconDollar", LocalizationService.Get("Dollar"), usd, currency, rates));
            }

            // 2. EUR
            if (root.TryGetProperty("EUR", out var eur))
            {
                specs.Currencies.Add(CreateItem("eur", "IconEuro", LocalizationService.Get("Euro"), eur, currency, rates));
            }

            // 3. Gram Altın
            if (root.TryGetProperty("gram-altin", out var gram))
            {
                specs.Gold.Add(CreateItem("gram_altin", "IconGoldBar", LocalizationService.Get("GramGold"), gram, currency, rates));
            }

            // 4. Çeyrek Altın
            if (root.TryGetProperty("ceyrek-altin", out var ceyrek))
            {
                specs.Gold.Add(CreateItem("ceyrek_altin", "IconGoldQuarter", LocalizationService.Get("QuarterGold"), ceyrek, currency, rates));
            }
        }
        catch { }

        return specs;
    }

    private FinanceItem ParseBinanceTicker(string json, string key, string iconKey, string label, IReadOnlyDictionary<string, decimal> rates)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var lastPriceStr = root.TryGetProperty("lastPrice", out var lp) ? lp.GetString() ?? "0" : "0";
            var changePercentStr = root.TryGetProperty("priceChangePercent", out var cp) ? cp.GetString() ?? "0" : "0";

            string direction = "Neutral";
            double changeNum = 0;
            if (double.TryParse(changePercentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out changeNum))
            {
                if (changeNum > 0.001) direction = "Up";
                else if (changeNum < -0.001) direction = "Down";
            }

            string formattedPrice = "$0";
            if (decimal.TryParse(lastPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceNum))
            {
                var conversion = _currency == "TRY" ? 1m : GetConversionFromUsd(_currency, rates);
                var convertedPrice = priceNum * conversion;
                formattedPrice = FormatCurrency(convertedPrice, _currency);
            }

            string sign = changeNum > 0 ? "+" : "";
            string formattedChange = $"%{sign}{changeNum:F2}".Replace(".", ",");

            return new FinanceItem(
                Key: key,
                IconKey: iconKey,
                Label: label,
                ValueFormatted: formattedPrice,
                ChangeFormatted: formattedChange,
                ChangeDirection: direction
            );
        }
        catch
        {
            return new FinanceItem(key, iconKey, label, "$0", "%0,00", "Neutral");
        }
    }

    private static FinanceItem CreateItem(string key, string iconKey, string label, JsonElement element, string currency, IReadOnlyDictionary<string, decimal> rates)
    {
        var selling = element.TryGetProperty("Selling", out var s) ? s.GetString() ?? "0" : "0";
        var change = element.TryGetProperty("Change", out var c) ? c.GetString() ?? "%0,00" : "%0,00";

        // Determine direction
        string direction = "Neutral";
        if (change.Contains("-")) direction = "Down";
        else if (!change.Contains("0,00") && !change.Contains("0.00")) direction = "Up";

        // Format clean price
        string valFormatted = $"{selling} {GetCurrencySymbol(currency)}";
        if (decimal.TryParse(selling.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
        {
            num *= rates.TryGetValue(currency, out var conversion) ? conversion : 1m;
            valFormatted = FormatCurrency(num, currency);
        }

        return new FinanceItem(
            Key: key,
            IconKey: iconKey,
            Label: label,
            ValueFormatted: valFormatted,
            ChangeFormatted: change,
            ChangeDirection: direction
        );
    }

    private static FinanceSpecs CreateDefaultSpecs()
    {
        var specs = new FinanceSpecs { LastUpdatedText = LocalizationService.Get("Updating") };
        var symbol = GetCurrencySymbol("TRY");
        specs.Currencies.Add(new FinanceItem("usd", "IconDollar", LocalizationService.Get("Dollar"), $"-- {symbol}", "%0,00", "Neutral"));
        specs.Currencies.Add(new FinanceItem("eur", "IconEuro", LocalizationService.Get("Euro"), $"-- {symbol}", "%0,00", "Neutral"));
        specs.Gold.Add(new FinanceItem("gram_altin", "IconGoldBar", LocalizationService.Get("GramGold"), $"-- {symbol}", "%0,00", "Neutral"));
        specs.Gold.Add(new FinanceItem("ceyrek_altin", "IconGoldQuarter", LocalizationService.Get("QuarterGold"), $"-- {symbol}", "%0,00", "Neutral"));
        specs.Crypto.Add(new FinanceItem("btc", "IconBitcoin", LocalizationService.Get("Bitcoin"), "$--", "%0,00", "Neutral"));
        specs.Crypto.Add(new FinanceItem("eth", "IconEthereum", LocalizationService.Get("Ethereum"), "$--", "%0,00", "Neutral"));
        return specs;
    }

    private static decimal GetConversionFromUsd(string currency, IReadOnlyDictionary<string, decimal> rates)
    {
        if (currency == "USD" || !rates.TryGetValue(currency, out var target) || !rates.TryGetValue("USD", out var usd))
        {
            return currency == "USD" ? 1m : 1m;
        }

        return target / usd;
    }

    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "TRY" => "₺",
        "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        "JPY" => "¥",
        "CHF" => "CHF",
        "CAD" => "CA$",
        "AUD" => "A$",
        _ => currency
    };

    private static string FormatCurrency(decimal value, string currency)
    {
        var culture = currency switch
        {
            "TRY" => new CultureInfo("tr-TR"),
            "USD" => new CultureInfo("en-US"),
            "EUR" => new CultureInfo("de-DE"),
            "GBP" => new CultureInfo("en-GB"),
            "JPY" => new CultureInfo("ja-JP"),
            _ => CultureInfo.InvariantCulture
        };
        var decimals = currency == "JPY" ? 0 : 2;
        return $"{value.ToString($"N{decimals}", culture)} {GetCurrencySymbol(currency)}";
    }
}