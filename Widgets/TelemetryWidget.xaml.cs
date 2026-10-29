using System.Windows;
using System.Windows.Controls;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class TelemetryWidget : UserControl
{
    public TelemetryWidget()
    {
        InitializeComponent();
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTexts();
    }

    private void OnLanguageChanged()
    {
        Dispatcher.Invoke(UpdateTexts);
    }

    private void UpdateTexts()
    {
        TxtTelemetryTitle.Text = LocalizationService.Get("Telemetry");
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ContentPanel.Visibility == Visibility.Visible)
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            ArrowIcon.Data = (System.Windows.Media.Geometry)FindResource("GeoExpand");
        }
        else
        {
            ContentPanel.Visibility = Visibility.Visible;
            ArrowIcon.Data = (System.Windows.Media.Geometry)FindResource("GeoCollapse");
        }
    }
}