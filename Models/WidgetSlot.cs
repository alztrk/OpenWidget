namespace OpenWidget.Models;

public enum WidgetKind
{
    Weather,
    Telemetry,
    Finance,
    Media,
    Clipboard,
    Hardware,
    QuickActions
}

public sealed class WidgetSlot(WidgetKind kind)
{
    public WidgetKind Kind { get; } = kind;
    public bool IsExpanded { get; set; } = true;
    public string? ScratchpadText { get; set; }
}
