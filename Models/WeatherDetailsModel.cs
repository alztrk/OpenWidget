using System;
using System.Collections.Generic;

namespace OpenWidget.Models;

public class WeatherForecastDay
{
    public string DayName { get; set; } = "Bugün";
    public string ConditionText { get; set; } = "Açık";
    public string IconKey { get; set; } = "GeoWeather";
    public double MinTemp { get; set; } = 15;
    public double MaxTemp { get; set; } = 20;

    public string TempRangeFormatted => $"{Math.Round(MinTemp)}° / {Math.Round(MaxTemp)}°";
}

public class WeatherDetailsModel
{
    public string City { get; set; } = "İstanbul";
    public double Temperature { get; set; } = 17.0;
    public double FeelsLike { get; set; } = 16.0;
    public int RelativeHumidity { get; set; } = 75;
    public double WindSpeed { get; set; } = 14.0;
    public double Precipitation { get; set; } = 0.0;
    public int WeatherCode { get; set; } = 0;
    public string ConditionText { get; set; } = "Açık";
    public string IconKey { get; set; } = "GeoWeather";
    public List<WeatherForecastDay> ForecastDays { get; set; } = new();

    public string TempFormatted => $"{Math.Round(Temperature)}°";
    public string FeelsLikeFormatted => $"{Math.Round(FeelsLike)}°C";
    public string HumidityFormatted => $"%{RelativeHumidity}";
    public string WindSpeedFormatted => $"{Math.Round(WindSpeed)} km/s";
    public string PrecipitationFormatted => $"{Precipitation:0.#} mm";
}
