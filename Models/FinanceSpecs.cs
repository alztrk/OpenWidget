using System.Collections.Generic;

namespace OpenWidget.Models;

public record FinanceItem(
    string Key,
    string IconKey,
    string Label,
    string ValueFormatted,
    string ChangeFormatted,
    string ChangeDirection, // "Up", "Down", "Neutral"
    string? SubText = null
);

public class FinanceSpecs
{
    public List<FinanceItem> Currencies { get; set; } = new();
    public List<FinanceItem> Gold { get; set; } = new();
    public string LastUpdatedText { get; set; } = "Canlı Piyasa";
    public string WeatherText { get; set; } = "İstanbul 17°C";
}
