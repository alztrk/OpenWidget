using System;
using System.Collections.Generic;

namespace OpenWidget.Models;

public record TelemetryItem(
    string Key,
    string IconKey,
    string Label,
    double Percentage,
    string MainText,
    string SubText,
    string BarColorKey = "AccentBrush",
    bool IsAlert = false
)
{
    public double RemainingPercentage => Math.Max(0.01, 100.0 - Math.Max(0.01, Math.Min(100.0, Percentage)));
}

public class TelemetrySpecs
{
    public string UptimeText { get; set; } = "0s 0dk";
    public bool IsGamingMode { get; set; } = false;
    public List<TelemetryItem> Performance { get; set; } = new();
    public List<TelemetryItem> Storage { get; set; } = new();
}
