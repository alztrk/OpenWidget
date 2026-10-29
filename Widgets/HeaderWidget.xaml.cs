using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class HeaderWidget : UserControl
{
    private DispatcherTimer? _clockTimer;

    public HeaderWidget()
    {
        InitializeComponent();
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateClock();
        UpdateTexts();

        if (_clockTimer == null)
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, ev) => UpdateClock();
            _clockTimer.Start();
        }
    }

    private void OnLanguageChanged()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateClock();
            UpdateTexts();
        });
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TxtClock.Text = now.ToString("HH:mm");

        var culture = LocalizationService.CurrentLanguage == "en"
            ? CultureInfo.InvariantCulture
            : new CultureInfo("tr-TR");

        TxtDate.Text = now.ToString("d MMMM dddd", culture);
    }

    private void UpdateTexts()
    {
        TxtGameMode.Text = LocalizationService.Get("GameMode");
        TxtUptimePrefix.Text = $"{LocalizationService.Get("Uptime")}: ";
    }
}
