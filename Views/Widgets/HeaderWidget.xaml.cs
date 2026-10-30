using System;
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
        Unloaded += (s, e) =>
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            _clockTimer?.Stop();
        };
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

        TxtDate.Text = now.ToString("d MMMM dddd", LocalizationService.GetCulture());
    }

    private void UpdateTexts()
    {
        TxtGameMode.Text = LocalizationService.Get("GameMode");
        TxtUptimePrefix.Text = $"{LocalizationService.Get("Uptime")}: ";
    }
}
