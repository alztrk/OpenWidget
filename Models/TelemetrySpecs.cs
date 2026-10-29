using System.Collections.Generic;

namespace RigBadge.Models;

public record TelemetryItem(
    string Key,
    string IconKey,
    string Label,
    double Percentage,
    string MainText,
    string SubText,
    string BarColorKey = "AccentBrush"
);

public class TelemetrySpecs
{
    public List<TelemetryItem> Performance { get; set; } = new();
    public List<TelemetryItem> Storage { get; set; } = new();
}
