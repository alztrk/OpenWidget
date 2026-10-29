using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenWidget.Services;

public class WeatherService
{
    private readonly string _cachePath;
    private string _currentCityConfig = "auto";
    private string _cachedWeather = "İstanbul 17°C";

    public WeatherService(string cachePath, string cityConfig = "auto")
    {
        _cachePath = cachePath;
        _currentCityConfig = string.IsNullOrWhiteSpace(cityConfig) ? "auto" : cityConfig;
        LoadCache();
    }

    public void UpdateCity(string cityConfig)
    {
        _currentCityConfig = string.IsNullOrWhiteSpace(cityConfig) ? "auto" : cityConfig;
    }

    public string GetCurrentWeather() => _cachedWeather;

    public async Task<string> FetchLatestAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(6);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenWidget/2.0");

            double latitude = 41.0082;
            double longitude = 28.9784;
            string displayCity = "İstanbul";

            if (string.Equals(_currentCityConfig, "auto", StringComparison.OrdinalIgnoreCase))
            {
                // Auto-detect via IP
                try
                {
                    var geoJson = await client.GetStringAsync("http://ip-api.com/json/?fields=city,lat,lon");
                    using var geoDoc = JsonDocument.Parse(geoJson);
                    var root = geoDoc.RootElement;
                    if (root.TryGetProperty("lat", out var latProp) && root.TryGetProperty("lon", out var lonProp))
                    {
                        latitude = latProp.GetDouble();
                        longitude = lonProp.GetDouble();
                        displayCity = root.TryGetProperty("city", out var cityProp) ? (cityProp.GetString() ?? "İstanbul") : "İstanbul";
                    }
                }
                catch
                {
                    // Fall back to default coordinates
                }
            }
            else
            {
                // Geocode city name via Open-Meteo Geocoding API
                try
                {
                    var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(_currentCityConfig)}&count=1&format=json";
                    var geoJson = await client.GetStringAsync(geoUrl);
                    using var geoDoc = JsonDocument.Parse(geoJson);
                    if (geoDoc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                    {
                        var first = results[0];
                        latitude = first.GetProperty("latitude").GetDouble();
                        longitude = first.GetProperty("longitude").GetDouble();
                        displayCity = first.GetProperty("name").GetString() ?? _currentCityConfig;
                    }
                    else
                    {
                        displayCity = _currentCityConfig;
                    }
                }
                catch
                {
                    displayCity = _currentCityConfig;
                }
            }

            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,weather_code&timezone=auto";
            var weatherJson = await client.GetStringAsync(weatherUrl);

            using var doc = JsonDocument.Parse(weatherJson);
            if (doc.RootElement.TryGetProperty("current", out var current))
            {
                double temp = current.GetProperty("temperature_2m").GetDouble();
                int code = current.GetProperty("weather_code").GetInt32();

                string condition = GetConditionText(code);
                _cachedWeather = $"{displayCity} {temp:F0}°C • {condition}";

                File.WriteAllText(_cachePath, _cachedWeather, System.Text.Encoding.UTF8);
                return _cachedWeather;
            }
        }
        catch
        {
            // Keep using cached value on network error
        }

        return _cachedWeather;
    }

    private void LoadCache()
    {
        try
        {
            if (File.Exists(_cachePath))
            {
                var cached = File.ReadAllText(_cachePath, System.Text.Encoding.UTF8).Trim();
                if (!string.IsNullOrEmpty(cached))
                {
                    _cachedWeather = cached;
                }
            }
        }
        catch { }
    }

    private static string GetConditionText(int code) => code switch
    {
        0 => LocalizationService.Get("WeatherClear"),
        1 or 2 => LocalizationService.Get("WeatherMainlyClear"),
        3 => LocalizationService.Get("WeatherPartlyCloudy"),
        45 or 48 => LocalizationService.Get("WeatherFoggy"),
        51 or 53 or 55 => LocalizationService.Get("WeatherDrizzle"),
        61 or 63 or 65 => LocalizationService.Get("WeatherRain"),
        71 or 73 or 75 or 77 => LocalizationService.Get("WeatherSnow"),
        80 or 81 or 82 => LocalizationService.Get("WeatherShowers"),
        95 or 96 or 99 => LocalizationService.Get("WeatherThunderstorm"),
        _ => LocalizationService.Get("WeatherOvercast")
    };
}