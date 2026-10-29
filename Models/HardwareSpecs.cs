using System.Collections.Generic;

namespace RigBadge.Models;

public record HardwareItem(
    string Category,
    string IconKey,
    string Label,
    string Value,
    string? SubValue = null
);

public class HardwareSpecs
{
    public List<HardwareItem> CoreSystem { get; set; } = new();
    public List<HardwareItem> Storage { get; set; } = new();
    public List<HardwareItem> Peripherals { get; set; } = new();
    public List<HardwareItem> CoolingAndPower { get; set; } = new();
}
