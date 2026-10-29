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
    private string _cachedWeather = "İstanbul 17°C";

    public WeatherService(string cachePath)
    {
        _cachePath = cachePath;
        LoadCache();
    }

    public string GetCurrentWeather() => _cachedWeather;

    public async Task<string> FetchLatestAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(6);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RigBadge/1.0");

            var url = "https://api.open-meteo.com/v1/forecast?latitude=41.0082&longitude=28.9784&current=temperature_2m,weather_code&timezone=auto";
            var json = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("current", out var current))
            {
                double temp = current.GetProperty("temperature_2m").GetDouble();
                int code = current.GetProperty("weather_code").GetInt32();

                string condition = GetConditionText(code);
                _cachedWeather = $"İstanbul {temp:F0}°C • {condition}";

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
        0 => "Açık",
        1 or 2 => "Az Bulutlu",
        3 => "Parçalı Bulutlu",
        45 or 48 => "Sisli",
        51 or 53 or 55 => "Çisenti",
        61 or 63 or 65 => "Yağmurlu",
        71 or 73 or 75 or 77 => "Karlı",
        80 or 81 or 82 => "Sağanak",
        95 or 96 or 99 => "Gök Gürültülü",
        _ => "Bulutlu"
    };
}
