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
    private List<string> _visibleKeys = FinanceInstrumentCatalog.DefaultVisibleKeys.ToList();

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
        RebuildFromCache();
    }

    public void SetVisibleKeys(IEnumerable<string>? keys)
    {
        _visibleKeys = FinanceInstrumentCatalog.NormalizeVisibleKeys(keys);
        ApplyVisibleKeys(_cachedSpecs);
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
                MergeMarketSpecs(_cachedSpecs, specs);
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

        EnsureCryptoPlaceholders(_cachedSpecs);
        ApplyVisibleKeys(_cachedSpecs);
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

        EnsureCryptoPlaceholders(_cachedSpecs);
        ApplyVisibleKeys(_cachedSpecs);
    }

    private void RebuildFromCache()
    {
        if (!string.IsNullOrWhiteSpace(_rawMarketJson))
        {
            var parsed = ParseTruncgilJson(_rawMarketJson, _currency, new Dictionary<string, decimal>());
            MergeMarketSpecs(_cachedSpecs, parsed);
        }

        ApplyVisibleKeys(_cachedSpecs);
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

            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("Update_Date") || property.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var type = property.Value.TryGetProperty("Type", out var typeElement)
                    ? typeElement.GetString()
                    : FinanceInstrumentCatalog.GroupCurrency;
                var key = property.Name;
                var group = FinanceInstrumentCatalog.GetGroup(key, type);
                var item = CreateItem(
                    key,
                    FinanceInstrumentCatalog.GetIconKey(key, type),
                    FinanceInstrumentCatalog.GetLabel(key),
                    property.Value,
                    currency,
                    rates);

                if (group == FinanceInstrumentCatalog.GroupGold)
                {
                    specs.Gold.Add(item);
                }
                else
                {
                    specs.Currencies.Add(item);
                }
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
        if (TryParseMarketDecimal(selling, out var num))
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

    private static bool TryParseMarketDecimal(string value, out decimal result)
    {
        var chars = value.Where(ch => char.IsDigit(ch) || ch is ',' or '.' or '-').ToArray();
        var normalized = new string(chars);
        if (normalized.Contains(',') && normalized.Contains('.'))
        {
            normalized = normalized.LastIndexOf(',') > normalized.LastIndexOf('.')
                ? normalized.Replace(".", string.Empty).Replace(',', '.')
                : normalized.Replace(",", string.Empty);
        }
        else if (normalized.Contains(','))
        {
            normalized = normalized.Replace(',', '.');
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private static FinanceSpecs CreateDefaultSpecs()
    {
        var specs = new FinanceSpecs { LastUpdatedText = LocalizationService.Get("Updating") };
        var symbol = GetCurrencySymbol("TRY");
        specs.Currencies.Add(new FinanceItem("USD", "IconDollar", FinanceInstrumentCatalog.GetLabel("USD"), $"-- {symbol}", "%0,00", "Neutral"));
        specs.Currencies.Add(new FinanceItem("EUR", "IconEuro", FinanceInstrumentCatalog.GetLabel("EUR"), $"-- {symbol}", "%0,00", "Neutral"));
        specs.Gold.Add(new FinanceItem("gram-altin", "IconGoldBar", FinanceInstrumentCatalog.GetLabel("gram-altin"), $"-- {symbol}", "%0,00", "Neutral"));
        EnsureCryptoPlaceholders(specs);
        ApplyVisibleKeys(specs, FinanceInstrumentCatalog.DefaultVisibleKeys);
        return specs;
    }

    private static void MergeMarketSpecs(FinanceSpecs target, FinanceSpecs parsed)
    {
        var crypto = target.Crypto;
        target.Currencies = parsed.Currencies;
        target.Gold = parsed.Gold;
        target.LastUpdatedText = parsed.LastUpdatedText;
        target.Crypto = crypto;
    }

    private void ApplyVisibleKeys(FinanceSpecs specs) => ApplyVisibleKeys(specs, _visibleKeys);

    private static void ApplyVisibleKeys(FinanceSpecs specs, IReadOnlyList<string> visibleKeys)
    {
        var lookup = new Dictionary<string, FinanceItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in specs.Currencies.Concat(specs.Gold).Concat(specs.Crypto))
        {
            lookup[item.Key] = item;
        }

        specs.AllItems = lookup.Values.ToList();
        specs.VisibleItems = visibleKeys
            .Select(key => lookup.TryGetValue(key, out var item) ? item : null)
            .Where(item => item != null)
            .Cast<FinanceItem>()
            .ToList();

        if (specs.VisibleItems.Count == 0)
        {
            specs.VisibleItems = specs.AllItems.Take(5).ToList();
        }
    }

    private static void EnsureCryptoPlaceholders(FinanceSpecs specs)
    {
        if (specs.Crypto.Count == 0)
        {
            specs.Crypto.Add(new FinanceItem("btc", "IconBitcoin", FinanceInstrumentCatalog.GetLabel("btc"), "$--", "%0,00", "Neutral"));
            specs.Crypto.Add(new FinanceItem("eth", "IconEthereum", FinanceInstrumentCatalog.GetLabel("eth"), "$--", "%0,00", "Neutral"));
        }
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