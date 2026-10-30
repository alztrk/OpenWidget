using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OpenWidget.Models;

namespace OpenWidget.Services;

public class WeatherService
{
    private readonly string _cachePath;
    private string _currentCityConfig = "auto";
    private WeatherDetailsModel _currentDetails = new();
    private string _cachedWeather = string.Empty;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    static WeatherService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenWidget/2.0");
    }

    public event Action<WeatherDetailsModel>? WeatherUpdated;

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

    public WeatherDetailsModel GetDetails() => _currentDetails;

    public string GetCurrentWeather()
    {
        if (!string.IsNullOrEmpty(_cachedWeather)) return _cachedWeather;
        return LocalizationService.Get("WeatherUpdating");
    }

    public async Task<string> FetchLatestAsync()
    {
        try
        {
            double latitude = 41.0082;
            double longitude = 28.9784;
            string displayCity = "İstanbul";

            if (string.Equals(_currentCityConfig, "auto", StringComparison.OrdinalIgnoreCase))
            {
                // Auto-detect via IP
                try
                {
                    var geoJson = await HttpClient.GetStringAsync("http://ip-api.com/json/?fields=city,lat,lon");
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
                    var geoJson = await HttpClient.GetStringAsync(geoUrl);
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

            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=3";
            var weatherJson = await HttpClient.GetStringAsync(weatherUrl);

            using var doc = JsonDocument.Parse(weatherJson);
            var rootWeather = doc.RootElement;
            if (rootWeather.TryGetProperty("current", out var current))
            {
                double temp = current.GetProperty("temperature_2m").GetDouble();
                double apparentTemp = current.TryGetProperty("apparent_temperature", out var apProp) ? apProp.GetDouble() : temp;
                int humidity = current.TryGetProperty("relative_humidity_2m", out var humProp) ? humProp.GetInt32() : 70;
                double precip = current.TryGetProperty("precipitation", out var prProp) ? prProp.GetDouble() : 0.0;
                double windSpeed = current.TryGetProperty("wind_speed_10m", out var wsProp) ? wsProp.GetDouble() : 10.0;
                int code = current.GetProperty("weather_code").GetInt32();

                string condition = GetConditionText(code);
                string iconKey = GetIconKeyForCode(code);

                var forecastList = new List<WeatherForecastDay>();
                if (rootWeather.TryGetProperty("daily", out var daily))
                {
                    if (daily.TryGetProperty("time", out var times) &&
                        daily.TryGetProperty("weather_code", out var codes) &&
                        daily.TryGetProperty("temperature_2m_max", out var maxTemps) &&
                        daily.TryGetProperty("temperature_2m_min", out var minTemps))
                    {
                        int count = Math.Min(3, times.GetArrayLength());
                        for (int i = 0; i < count; i++)
                        {
                            string dayName;
                            if (i == 0)
                            {
                                dayName = LocalizationService.Get("Today");
                            }
                            else if (i == 1)
                            {
                                dayName = LocalizationService.Get("Tomorrow");
                            }
                            else
                            {
                                var dateStr = times[i].GetString();
                                if (DateTime.TryParse(dateStr, out var dt))
                                {
                                    var culture = LocalizationService.GetCulture();
                                    dayName = dt.ToString("dddd", culture);
                                    if (dayName.Length > 0)
                                    {
                                        dayName = char.ToUpper(dayName[0], culture) + dayName.Substring(1);
                                    }
                                }
                                else
                                {
                                    dayName = $"Gün {i + 1}";
                                }
                            }

                            int dCode = codes[i].GetInt32();
                            double dMax = maxTemps[i].GetDouble();
                            double dMin = minTemps[i].GetDouble();

                            forecastList.Add(new WeatherForecastDay
                            {
                                DayName = dayName,
                                ConditionText = GetConditionText(dCode),
                                IconKey = GetIconKeyForCode(dCode),
                                MinTemp = dMin,
                                MaxTemp = dMax
                            });
                        }
                    }
                }

                _currentDetails = new WeatherDetailsModel
                {
                    City = displayCity,
                    Temperature = temp,
                    FeelsLike = apparentTemp,
                    RelativeHumidity = humidity,
                    Precipitation = precip,
                    WindSpeed = windSpeed,
                    WeatherCode = code,
                    ConditionText = condition,
                    IconKey = iconKey,
                    ForecastDays = forecastList
                };

                _cachedWeather = $"{displayCity} {temp:F0}°C • {condition}";

                SaveCache();
                WeatherUpdated?.Invoke(_currentDetails);
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
                    if (cached.StartsWith("{"))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var model = JsonSerializer.Deserialize<WeatherDetailsModel>(cached, options);
                        if (model != null)
                        {
                            _currentDetails = model;
                            _cachedWeather = $"{model.City} {model.Temperature:F0}°C • {model.ConditionText}";
                            return;
                        }
                    }

                    // Legacy string cache fallback
                    _cachedWeather = cached;
                }
            }
        }
        catch { }
    }

    private void SaveCache()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_currentDetails, options);
            File.WriteAllText(_cachePath, json, System.Text.Encoding.UTF8);
        }
        catch { }
    }

    public static string GetIconKeyForCode(int code) => code switch
    {
        0 => "GeoWeather",
        1 or 2 or 3 => "GeoWeather",
        45 or 48 => "GeoWeather",
        51 or 53 or 55 or 61 or 63 or 65 or 80 or 81 or 82 => "GeoDrop",
        71 or 73 or 75 or 77 => "GeoWeather",
        95 or 96 or 99 => "GeoBolt",
        _ => "GeoWeather"
    };

    public static string GetConditionText(int code) => code switch
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
