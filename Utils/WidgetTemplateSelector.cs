using System.Windows;
using System.Windows.Controls;
using OpenWidget.Models;

namespace OpenWidget.Utils;

public sealed class WidgetTemplateSelector : DataTemplateSelector
{
    public DataTemplate? WeatherTemplate { get; set; }
    public DataTemplate? TelemetryTemplate { get; set; }
    public DataTemplate? FinanceTemplate { get; set; }
    public DataTemplate? MediaTemplate { get; set; }
    public DataTemplate? ClipboardTemplate { get; set; }
    public DataTemplate? HardwareTemplate { get; set; }
    public DataTemplate? QuickActionsTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not WidgetSlot slot)
        {
            return base.SelectTemplate(item, container);
        }

        return slot.Kind switch
        {
            WidgetKind.Weather => WeatherTemplate,
            WidgetKind.Telemetry => TelemetryTemplate,
            WidgetKind.Finance => FinanceTemplate,
            WidgetKind.Media => MediaTemplate,
            WidgetKind.Clipboard => ClipboardTemplate,
            WidgetKind.Hardware => HardwareTemplate,
            WidgetKind.QuickActions => QuickActionsTemplate,
            _ => base.SelectTemplate(item, container)
        };
    }
}
