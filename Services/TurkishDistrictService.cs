using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenWidget.Services;

public static class TurkishDistrictService
{
    private const string ProvincesUrl = "https://api.turkiyeapi.dev/v2/provinces?fields=id,name&limit=100";
    private const string DistrictsUrl = "https://api.turkiyeapi.dev/v2/districts?fields=name,provinceId&limit=1000&sort=name";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    private static IReadOnlyList<string>? _cache;
    private static Task<IReadOnlyList<string>>? _loadTask;

    static TurkishDistrictService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenWidget/2.0");
    }

    public static Task<IReadOnlyList<string>> GetDistrictsAsync()
    {
        if (_cache != null)
        {
            return Task.FromResult(_cache);
        }

        _loadTask ??= LoadAsync();
        return _loadTask;
    }

    public static List<string> ParseDistrictLocations(string provincesJson, string districtsJson)
    {
        var provinces = new Dictionary<int, string>();
        using (var provinceDoc = JsonDocument.Parse(provincesJson))
        {
            foreach (var item in EnumerateData(provinceDoc.RootElement))
            {
                if (item.TryGetProperty("id", out var idElement) &&
                    item.TryGetProperty("name", out var nameElement))
                {
                    var name = nameElement.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        provinces[idElement.GetInt32()] = name.Trim();
                    }
                }
            }
        }

        var locations = new List<string>();
        using var districtDoc = JsonDocument.Parse(districtsJson);
        foreach (var item in EnumerateData(districtDoc.RootElement))
        {
            if (!item.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            var district = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(district))
            {
                continue;
            }

            var provinceName = item.TryGetProperty("provinceId", out var provinceIdElement) &&
                               provinces.TryGetValue(provinceIdElement.GetInt32(), out var matched)
                ? matched
                : null;

            locations.Add(string.IsNullOrWhiteSpace(provinceName)
                ? district.Trim()
                : $"{district.Trim()}, {provinceName}");
        }

        return locations;
    }

    private static async Task<IReadOnlyList<string>> LoadAsync()
    {
        try
        {
            var provincesJson = await HttpClient.GetStringAsync(ProvincesUrl);
            var districtsJson = await HttpClient.GetStringAsync(DistrictsUrl);
            _cache = ParseDistrictLocations(provincesJson, districtsJson);
            return _cache;
        }
        catch
        {
            _loadTask = null;
            return [];
        }
    }

    private static IEnumerable<JsonElement> EnumerateData(JsonElement root)
    {
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                yield return item;
            }
        }
    }
}
