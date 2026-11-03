using System;
using System.Collections.Generic;

namespace OpenWidget.Services;

public static class WeatherLocationFormatter
{
    public static string Format(string? locality, string? district, string? region, string fallback = "İstanbul")
    {
        var specific = FirstNonEmpty(district, locality);
        var parent = FirstNonEmpty(region, locality);

        var parts = new List<string>();
        AddUnique(parts, specific);
        AddUnique(parts, parent);

        return parts.Count > 0 ? string.Join(", ", parts) : fallback;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static void AddUnique(List<string> parts, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        foreach (var part in parts)
        {
            if (string.Equals(part, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        parts.Add(trimmed);
    }
}
