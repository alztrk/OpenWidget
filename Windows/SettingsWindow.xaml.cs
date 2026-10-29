using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenWidget.Models;
using OpenWidget.Services;

namespace OpenWidget.Windows;

public partial class SettingsWindow : Window
{
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

        // Widgets
        ChkHeader.IsChecked = _config.Widgets.ShowHeader;
        ChkTelemetry.IsChecked = _config.Widgets.ShowTelemetry;
        ChkFinance.IsChecked = _config.Widgets.ShowFinance;
        ChkHardware.IsChecked = _config.Widgets.ShowHardware;
        ChkQuickActions.IsChecked = _config.Widgets.ShowQuickActions;

        // Performance & Options
        ChkAutoStart.IsChecked = AutoStartService.IsAutoStartEnabled();
        ChkAlwaysOnTop.IsChecked = _config.Widget.AlwaysOnTop;
        ChkGameMode.IsChecked = _config.Widget.AutoGameMode;
    }

    private void ApplyLocalization()
    {
        TxtTitle.Text = LocalizationService.Get("SettingsTitle");
        LblGeneralHeader.Text = LocalizationService.Get("GeneralTab");
        LblLanguage.Text = LocalizationService.Get("LanguageLabel");
        LblCity.Text = LocalizationService.Get("CityLabel");
        LblCityHelp.Text = LocalizationService.Get("CityHelp");
        LblOpacity.Text = LocalizationService.Get("OpacityLabel");
        LblAccent.Text = LocalizationService.Get("AccentThemeLabel");

        LblWidgetsHeader.Text = LocalizationService.Get("WidgetsGroupLabel");
        ChkHeader.Content = LocalizationService.Get("ShowHeaderLabel");
        ChkTelemetry.Content = LocalizationService.Get("ShowTelemetryLabel");
        ChkFinance.Content = LocalizationService.Get("ShowFinanceLabel");
        ChkHardware.Content = LocalizationService.Get("ShowHardwareLabel");
        ChkQuickActions.Content = LocalizationService.Get("ShowQuickActionsLabel");

        LblPerformanceHeader.Text = LocalizationService.Get("PerformanceGroupLabel");
        ChkAutoStart.Content = LocalizationService.Get("AutoStartSettingLabel");
        ChkAlwaysOnTop.Content = LocalizationService.Get("AlwaysOnTopSettingLabel");
        ChkGameMode.Content = LocalizationService.Get("GameModeSettingLabel");

        BtnReset.Content = LocalizationService.Get("ResetDefaultsBtn");
        BtnCancel.Content = LocalizationService.Get("CloseBtn");
        BtnSave.Content = LocalizationService.Get("SaveAndApplyBtn");
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
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        CmbLanguage.SelectedIndex = 0; // TR
        TxtCity.Text = "";
        SliderOpacity.Value = 100;
        CmbAccent.SelectedIndex = 0; // Sky
        ChkHeader.IsChecked = true;
        ChkTelemetry.IsChecked = true;
        ChkFinance.IsChecked = true;
        ChkHardware.IsChecked = true;
        ChkQuickActions.IsChecked = true;
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

        // 5. Widgets
        _config.Widgets.ShowHeader = ChkHeader.IsChecked == true;
        _config.Widgets.ShowTelemetry = ChkTelemetry.IsChecked == true;
        _config.Widgets.ShowFinance = ChkFinance.IsChecked == true;
        _config.Widgets.ShowHardware = ChkHardware.IsChecked == true;
        _config.Widgets.ShowQuickActions = ChkQuickActions.IsChecked == true;

        // 6. Options
        _config.Widget.AlwaysOnTop = ChkAlwaysOnTop.IsChecked == true;
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