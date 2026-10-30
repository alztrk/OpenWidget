using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenWidget.Models;

namespace OpenWidget.Services;

public sealed record PriceUpdateResult(
    bool Succeeded,
    bool WasSkipped,
    string Message,
    DateTime? UpdatedAtUtc);

public class PriceService
{
    private readonly string _configPath;
    private readonly HardwareConfig? _config;
    private readonly HardwarePrices _prices;
    private readonly IHardwarePriceProvider? _provider;
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public PriceService(string priceFilePath)
    {
        _configPath = priceFilePath;
        _prices = HardwarePrices.Load(priceFilePath);
    }

    public PriceService(HardwareConfig config, string configPath)
    {
        _config = config;
        _configPath = configPath;
        _prices = config.Prices;

        if (config.PriceProvider.Enabled && Uri.TryCreate(config.PriceProvider.Endpoint, UriKind.Absolute, out _))
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(Math.Clamp(config.PriceProvider.TimeoutSeconds, 5, 60))
            };
            _provider = new BackendPriceProvider(client, config.PriceProvider.Endpoint);
        }
    }

    public void CheckAndApplyDailyUpdate()
    {
        foreach (var entry in _prices.Entries.Values)
        {
            if (entry.Status == PriceStatuses.Live &&
                entry.LastSuccessfulUpdateUtc is { } updated &&
                DateTime.UtcNow - updated > GetMaxAge())
            {
                entry.Status = PriceStatuses.Stale;
            }
        }

        SavePrices();
    }

    public bool ShouldUpdate()
    {
        if (_provider == null)
        {
            return false;
        }

        return _prices.Entries.Values.Any(entry =>
            entry.Status is not PriceStatuses.Live ||
            entry.LastSuccessfulUpdateUtc is not { } updated ||
            DateTime.UtcNow - updated > GetMaxAge());
    }

    public async Task<PriceUpdateResult> UpdatePricesAsync(CancellationToken cancellationToken = default)
    {
        if (_provider == null)
        {
            MarkUnavailable();
            return new PriceUpdateResult(
                false,
                true,
                "Fiyat sağlayıcısı yapılandırılmamış.",
                null);
        }

        try
        {
            var quotes = await _provider.GetPricesAsync(_prices.Entries.Keys.ToArray(), cancellationToken);
            if (quotes.Count == 0)
            {
                MarkUnavailable();
                return new PriceUpdateResult(false, false, "Fiyat sağlayıcısı ürün döndürmedi.", null);
            }

            var updatedAt = DateTime.UtcNow;
            foreach (var quote in quotes)
            {
                if (quote.Value < 0 || string.IsNullOrWhiteSpace(quote.Key))
                {
                    continue;
                }

                if (!_prices.Entries.TryGetValue(quote.Key, out var entry))
                {
                    entry = new PriceEntry();
                    _prices.Entries[quote.Key] = entry;
                }

                entry.Value = quote.Value;
                entry.Currency = quote.Currency;
                entry.ProductId = quote.ProductId;
                entry.ProductUrl = quote.ProductUrl;
                entry.Source = _provider.Name;
                entry.Status = PriceStatuses.Live;
                entry.LastCheckedUtc = updatedAt;
                entry.LastSuccessfulUpdateUtc = updatedAt;
                _prices.Items[quote.Key] = quote.Value;
            }

            _prices.Source = _provider.Name;
            _prices.LastUpdated = updatedAt.ToString("O", CultureInfo.InvariantCulture);
            SavePrices();
            return new PriceUpdateResult(true, false, "Fiyatlar güncellendi.", updatedAt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            MarkUnavailable();
            return new PriceUpdateResult(false, false, $"Fiyat servisine ulaşılamadı: {ex.Message}", null);
        }
        catch (JsonException ex)
        {
            MarkUnavailable();
            return new PriceUpdateResult(false, false, $"Fiyat servisi geçersiz veri döndürdü: {ex.Message}", null);
        }
    }

    public string GetSource() => _prices.Source;

    public string GetPriceStatus(string itemKey)
    {
        return _prices.Entries.TryGetValue(itemKey, out var entry)
            ? entry.Status
            : PriceStatuses.Unavailable;
    }

    public string GetPriceFormatted(string itemKey)
    {
        return _prices.Entries.TryGetValue(itemKey, out var entry) && entry.Value > 0
            ? $"{entry.Value.ToString("N0", TrCulture)} {_prices.Currency}"
            : $"- {_prices.Currency}";
    }

    public decimal GetPriceValue(string itemKey)
    {
        return _prices.Entries.TryGetValue(itemKey, out var entry) ? entry.Value : 0;
    }

    public string GetTotalPriceFormatted() =>
        $"{GetTotalPriceValue().ToString("N0", TrCulture)} {_prices.Currency}";

    public decimal GetTotalPriceValue() =>
        _prices.Entries.Values.Where(entry => entry.Value > 0).Sum(entry => entry.Value);

    public string GetLastUpdatedFormatted()
    {
        if (DateTime.TryParse(
            _prices.LastUpdated,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var date))
        {
            return date.ToLocalTime().Date == DateTime.Today
                ? "Bugün"
                : date.ToLocalTime().ToString("dd.MM.yyyy");
        }

        return "Güncellenmedi";
    }

    private TimeSpan GetMaxAge() =>
        TimeSpan.FromHours(Math.Clamp(_config?.PriceProvider.MaxAgeHours ?? 24, 1, 168));

    private void MarkUnavailable()
    {
        foreach (var entry in _prices.Entries.Values)
        {
            if (entry.Status is not PriceStatuses.Manual)
            {
                entry.Status = entry.Value > 0 ? PriceStatuses.Stale : PriceStatuses.Unavailable;
            }
        }

        _prices.Source = "Estimated";
        SavePrices();
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
}
