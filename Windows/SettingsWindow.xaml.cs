using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenWidget.Models;
using OpenWidget.Services;

namespace OpenWidget.Windows;

public partial class SettingsWindow : Window
{
    private static readonly string[] PredefinedCities =
    [
        "İstanbul", "Ankara", "İzmir", "Bursa", "Antalya", "Adana", "Konya", "Gaziantep",
        "Şanlıurfa", "Kocaeli", "Mersin", "Diyarbakır", "Hatay", "Manisa", "Kayseri",
        "Samsun", "Balıkesir", "Kahramanmaraş", "Van", "Aydın", "Tekirdağ", "Denizli",
        "Sakarya", "Muğla", "Eskişehir", "Trabzon", "Malatya", "Ordu", "Erzurum",
        "Afyonkarahisar", "Sivas", "Çanakkale", "Edirne", "Rize", "Kütahya",
        "London", "New York", "Berlin", "Paris", "Tokyo", "Rome", "Amsterdam", "Dubai"
    ];

    private readonly HardwareConfig _config;
    private readonly Action<HardwareConfig> _onSave;

    public SettingsWindow(HardwareConfig config, Action<HardwareConfig> onSave)
    {
        InitializeComponent();
        _config = config;
        _onSave = onSave;

        LoadValues();
        ApplyLocalization();
    }

    private void LoadValues()
    {
        // Language
        var lang = _config.Widget.Language?.ToLowerInvariant() ?? "tr";
        foreach (ComboBoxItem item in CmbLanguage.Items)
        {
            if (string.Equals(item.Tag?.ToString(), lang, StringComparison.OrdinalIgnoreCase))
            {
                CmbLanguage.SelectedItem = item;
                break;
            }
        }

        // City
        TxtCity.Text = string.Equals(_config.Widget.City, "auto", StringComparison.OrdinalIgnoreCase)
            ? ""
            : (_config.Widget.City ?? "");

        // Opacity
        var op = _config.Widget.Opacity > 0.1 ? _config.Widget.Opacity * 100 : 100;
        SliderOpacity.Value = Math.Clamp(op, 30, 100);
        TxtOpacityValue.Text = $"{(int)SliderOpacity.Value}%";
        Opacity = Math.Clamp(op / 100.0, 0.3, 1.0);

        // Accent Theme
        var accent = _config.Widget.AccentTheme ?? "Sky";
        foreach (ComboBoxItem item in CmbAccent.Items)
        {
            if (string.Equals(item.Tag?.ToString(), accent, StringComparison.OrdinalIgnoreCase))
            {
                CmbAccent.SelectedItem = item;
                break;
            }
        }

        // HUD Layout Radio
        RadioModeFull.IsChecked = !_config.Widget.IsCompact;
        RadioModeCompact.IsChecked = _config.Widget.IsCompact;

        // Widgets
        ChkHeader.IsChecked = _config.Widgets.ShowHeader;
        ChkTelemetry.IsChecked = _config.Widgets.ShowTelemetry;
        ChkFinance.IsChecked = _config.Widgets.ShowFinance;
        ChkMedia.IsChecked = _config.Widgets.ShowMedia;
        ChkHardware.IsChecked = _config.Widgets.ShowHardware;
        ChkQuickActions.IsChecked = _config.Widgets.ShowQuickActions;

        // Performance & Options
        ChkLockPosition.IsChecked = _config.Widget.IsLocked;
        ChkPinToDesktop.IsChecked = _config.Widget.PinToDesktop;
        ChkAutoStart.IsChecked = AutoStartService.IsAutoStartEnabled();
        ChkAlwaysOnTop.IsChecked = _config.Widget.AlwaysOnTop;
        ChkGameMode.IsChecked = _config.Widget.AutoGameMode;
    }

    private void ApplyLocalization()
    {
        TxtTitle.Text = LocalizationService.Get("SettingsTitle");
        TabBtnGeneral.Content = LocalizationService.Get("GeneralTab");
        TabBtnWidgets.Content = LocalizationService.Get("WidgetsTab");
        TabBtnSystem.Content = LocalizationService.Get("PerformanceTab");

        LblLanguage.Text = LocalizationService.Get("LanguageLabel");
        LblCity.Text = LocalizationService.Get("CityLabel");
        LblCityHelp.Text = LocalizationService.Get("CityHelp");
        LblOpacity.Text = LocalizationService.Get("OpacityLabel");
        LblAccent.Text = LocalizationService.Get("AccentThemeLabel");

        BtnReset.Content = LocalizationService.Get("ResetDefaultsBtn");
        BtnCancel.Content = LocalizationService.CurrentLanguage == "en" ? "Cancel" : "Vazgeç";
        BtnSave.Content = LocalizationService.Get("SaveAndApplyBtn");
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (PanelGeneral == null || PanelWidgets == null || PanelSystem == null) return;

        PanelGeneral.Visibility = TabBtnGeneral.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelWidgets.Visibility = TabBtnWidgets.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelSystem.Visibility = TabBtnSystem.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtOpacityValue != null)
        {
            TxtOpacityValue.Text = $"{(int)SliderOpacity.Value}%";
            Opacity = Math.Clamp(SliderOpacity.Value / 100.0, 0.3, 1.0);
        }
    }

    private void TxtCity_GotFocus(object sender, RoutedEventArgs e)
    {
        UpdateCitySuggestions();
    }

    private void TxtCity_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCitySuggestions();
    }

    private void TxtCity_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            PopupCitySuggestions.IsOpen = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            PopupCitySuggestions.IsOpen = false;
        }
    }

    private void UpdateCitySuggestions()
    {
        if (TxtCity == null || PopupCitySuggestions == null || ListCitySuggestions == null) return;

        var query = TxtCity.Text?.Trim() ?? "";
        var results = new List<string>();

        var autoLabel = LocalizationService.CurrentLanguage == "en" ? "📍 Auto Detect (IP Location)" : "📍 Otomatik Algıla (IP)";

        if (string.IsNullOrEmpty(query))
        {
            results.Add(autoLabel);
            results.AddRange(PredefinedCities.Take(7));
        }
        else
        {
            if (autoLabel.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                "auto".IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                "otomatik".IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add(autoLabel);
            }

            var culture = new CultureInfo("tr-TR");
            var matched = PredefinedCities.Where(c => culture.CompareInfo.IndexOf(c, query, CompareOptions.IgnoreCase) >= 0);
            results.AddRange(matched.Take(8));
        }

        if (results.Count > 0 && TxtCity.IsFocused)
        {
            ListCitySuggestions.ItemsSource = results;
            PopupCitySuggestions.IsOpen = true;
        }
        else
        {
            PopupCitySuggestions.IsOpen = false;
        }
    }

    private void CityItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is string selectedCity)
        {
            if (selectedCity.StartsWith("📍"))
            {
                TxtCity.Text = "";
            }
            else
            {
                TxtCity.Text = selectedCity;
            }
            PopupCitySuggestions.IsOpen = false;
            TxtCity.Focus();
            TxtCity.CaretIndex = TxtCity.Text.Length;
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        CmbLanguage.SelectedIndex = 0; // TR
        TxtCity.Text = "";
        SliderOpacity.Value = 100;
        CmbAccent.SelectedIndex = 0; // Sky
        RadioModeFull.IsChecked = true;
        RadioModeCompact.IsChecked = false;
        ChkHeader.IsChecked = true;
        ChkTelemetry.IsChecked = true;
        ChkFinance.IsChecked = true;
        ChkMedia.IsChecked = true;
        ChkHardware.IsChecked = true;
        ChkQuickActions.IsChecked = true;
        ChkLockPosition.IsChecked = false;
        ChkPinToDesktop.IsChecked = false;
        ChkAutoStart.IsChecked = true;
        ChkAlwaysOnTop.IsChecked = false;
        ChkGameMode.IsChecked = true;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // 1. Language
        if (CmbLanguage.SelectedItem is ComboBoxItem langItem && langItem.Tag != null)
        {
            _config.Widget.Language = langItem.Tag.ToString() ?? "tr";
            LocalizationService.CurrentLanguage = _config.Widget.Language;
        }

        // 2. City
        var cityText = TxtCity.Text?.Trim();
        _config.Widget.City = string.IsNullOrWhiteSpace(cityText) ? "auto" : cityText;

        // 3. Opacity
        _config.Widget.Opacity = Math.Round(SliderOpacity.Value / 100.0, 2);

        // 4. Accent Theme
        if (CmbAccent.SelectedItem is ComboBoxItem accentItem && accentItem.Tag != null)
        {
            _config.Widget.AccentTheme = accentItem.Tag.ToString() ?? "Sky";
        }

        // 5. Layout Mode (Custom Radio Button)
        _config.Widget.IsCompact = RadioModeCompact.IsChecked == true;

        // 6. Widgets
        _config.Widgets.ShowHeader = ChkHeader.IsChecked == true;
        _config.Widgets.ShowTelemetry = ChkTelemetry.IsChecked == true;
        _config.Widgets.ShowFinance = ChkFinance.IsChecked == true;
        _config.Widgets.ShowMedia = ChkMedia.IsChecked == true;
        _config.Widgets.ShowHardware = ChkHardware.IsChecked == true;
        _config.Widgets.ShowQuickActions = ChkQuickActions.IsChecked == true;

        // 7. Options
        _config.Widget.IsLocked = ChkLockPosition.IsChecked == true;
        _config.Widget.PinToDesktop = ChkPinToDesktop.IsChecked == true;
        _config.Widget.AlwaysOnTop = ChkPinToDesktop.IsChecked == true ? false : (ChkAlwaysOnTop.IsChecked == true);
        _config.Widget.AutoGameMode = ChkGameMode.IsChecked == true;

        var autoStart = ChkAutoStart.IsChecked == true;
        if (AutoStartService.IsAutoStartEnabled() != autoStart)
        {
            AutoStartService.SetAutoStart(autoStart);
        }

        _onSave?.Invoke(_config);
        Close();
    }
}