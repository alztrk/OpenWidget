using System.Windows;
using System.Windows.Controls;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class HeaderWidget : UserControl
{
    public HeaderWidget()
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
        TxtGameMode.Text = LocalizationService.Get("GameMode");
        TxtUptimePrefix.Text = $"{LocalizationService.Get("Uptime")}: ";
    }
}