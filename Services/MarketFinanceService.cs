using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RigBadge.Models;

namespace RigBadge.Services;

public class MarketFinanceService
{
    private readonly string _cacheFilePath;
    private static readonly CultureInfo TrCulture = new("tr-TR");
    private FinanceSpecs _cachedSpecs = new();

    public MarketFinanceService(string cacheFilePath)
    {
        _cacheFilePath = cacheFilePath;
        LoadCache();
    }

    public FinanceSpecs GetCurrentSpecs() => _cachedSpecs;

    public async Task<FinanceSpecs> FetchLatestAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            var json = await client.GetStringAsync("https://finans.truncgil.com/v3/today.json");
            var specs = ParseTruncgilJson(json);
            if (specs.Currencies.Count > 0 || specs.Gold.Count > 0)
            {
                _cachedSpecs = specs;
                File.WriteAllText(_cacheFilePath, json);
                return _cachedSpecs;
            }
        }
        catch
        {
            // Fall back to cache on network issue
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
                _cachedSpecs = ParseTruncgilJson(json);
            }
        }
        catch { }

        if (_cachedSpecs.Currencies.Count == 0 && _cachedSpecs.Gold.Count == 0)
        {
            _cachedSpecs = CreateDefaultSpecs();
        }
    }

    private static FinanceSpecs ParseTruncgilJson(string json)
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
                specs.Currencies.Add(CreateItem("usd", "IconDollar", "Dolar (USD)", usd));
            }

            // 2. EUR
            if (root.TryGetProperty("EUR", out var eur))
            {
                specs.Currencies.Add(CreateItem("eur", "IconEuro", "Euro (EUR)", eur));
            }

            // 3. Gram Altın
            if (root.TryGetProperty("gram-altin", out var gram))
            {
                specs.Gold.Add(CreateItem("gram_altin", "IconGold", "Gram Altın", gram));
            }

            // 4. Çeyrek Altın
            if (root.TryGetProperty("ceyrek-altin", out var ceyrek))
            {
                specs.Gold.Add(CreateItem("ceyrek_altin", "IconGold", "Çeyrek Altın", ceyrek));
            }

            // 5. Tam Altın
            if (root.TryGetProperty("tam-altin", out var tam))
            {
                specs.Gold.Add(CreateItem("tam_altin", "IconGold", "Tam Altın", tam));
            }
        }
        catch { }

        return specs;
    }

    private static FinanceItem CreateItem(string key, string iconKey, string label, JsonElement element)
    {
        var selling = element.TryGetProperty("Selling", out var s) ? s.GetString() ?? "0" : "0";
        var change = element.TryGetProperty("Change", out var c) ? c.GetString() ?? "%0,00" : "%0,00";

        // Determine direction
        string direction = "Neutral";
        if (change.Contains("-")) direction = "Down";
        else if (!change.Contains("0,00") && !change.Contains("0.00")) direction = "Up";

        // Format clean price
        string valFormatted = $"{selling} ₺";
        if (decimal.TryParse(selling.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
        {
            valFormatted = num >= 1000 ? $"{num:N0} ₺" : $"{num:N2} ₺";
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
        var specs = new FinanceSpecs { LastUpdatedText = "Piyasa Kurları" };
        specs.Currencies.Add(new FinanceItem("usd", "IconDollar", "Dolar (USD)", "49,84 ₺", "%0,00", "Neutral"));
        specs.Currencies.Add(new FinanceItem("eur", "IconEuro", "Euro (EUR)", "55,65 ₺", "%+0,14", "Up"));
        specs.Gold.Add(new FinanceItem("gram_altin", "IconGold", "Gram Altın", "6.450 ₺", "%-1,03", "Down"));
        specs.Gold.Add(new FinanceItem("ceyrek_altin", "IconGold", "Çeyrek Altın", "10.632 ₺", "%0,00", "Neutral"));
        specs.Gold.Add(new FinanceItem("tam_altin", "IconGold", "Tam Altın", "42.400 ₺", "%0,00", "Neutral"));
        return specs;
    }
}
