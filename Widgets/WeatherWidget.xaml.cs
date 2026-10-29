using System.Windows;
using System.Windows.Controls;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class WeatherWidget : UserControl
{
    public WeatherWidget()
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
        TxtTitle.Text = LocalizationService.Get("WeatherTitle");
        TxtFeelsLikeLabel.Text = $"{LocalizationService.Get("FeelsLike")} ";
        TxtHumidityLabel.Text = LocalizationService.Get("Humidity");
        TxtWindLabel.Text = LocalizationService.Get("Wind");
        TxtPrecipLabel.Text = LocalizationService.Get("Precipitation");
    }
}
