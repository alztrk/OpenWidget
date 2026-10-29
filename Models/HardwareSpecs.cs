using System.Collections.Generic;

namespace OpenWidget.Models;

public record HardwareItem(
    string Category,
    string IconKey,
    string Label,
    string Value,
    string? SubValue = null,
    string? PriceFormatted = null,
    string? Url = null,
    string? LinkDisplay = null
);

public class HardwareSpecs
{
    public List<HardwareItem> CoreSystem { get; set; } = new();
    public List<HardwareItem> Storage { get; set; } = new();
    public List<HardwareItem> Peripherals { get; set; } = new();
    public List<HardwareItem> CoolingAndPower { get; set; } = new();
    public List<HardwareItem> Accessories { get; set; } = new();

    public string TotalPriceFormatted { get; set; } = "0 ₺";
    public string LastUpdatedText { get; set; } = "Bugün";
    public string PriceSource { get; set; } = "Amazon.com.tr";
}
