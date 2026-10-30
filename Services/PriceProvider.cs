using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenWidget.Models;

namespace OpenWidget.Services;

public sealed record PriceQuote(
    string Key,
    decimal Value,
    string Currency,
    string? ProductId,
    string? ProductUrl);

public interface IHardwarePriceProvider
{
    string Name { get; }
    Task<IReadOnlyList<PriceQuote>> GetPricesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken);
}

public sealed class BackendPriceProvider : IHardwarePriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    public BackendPriceProvider(HttpClient httpClient, string endpoint)
    {
        _httpClient = httpClient;
        _endpoint = endpoint.TrimEnd('/');
    }

    public string Name => "Backend API";

    public async Task<IReadOnlyList<PriceQuote>> GetPricesAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_endpoint))
        {
            return Array.Empty<PriceQuote>();
        }

        var uri = $"{_endpoint}/prices?keys={Uri.EscapeDataString(string.Join(",", keys))}";
        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var quotes = await JsonSerializer.DeserializeAsync<List<PriceQuote>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);
        return quotes ?? new List<PriceQuote>();
    }
}
