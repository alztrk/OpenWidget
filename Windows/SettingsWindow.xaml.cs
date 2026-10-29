using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenWidget.Models;
using OpenWidget.Services;
using OpenWidget.Utils;

namespace OpenWidget.Windows;

public partial class SettingsWindow : Window
{
    private readonly HardwareConfig _config;
    private readonly Action<HardwareConfig> _onSave;
    private string _selectedCity = "auto";
    private bool _isClosing;

    public SettingsWindow(HardwareConfig config, Action<HardwareConfig> onSave)
    {
        InitializeComponent();
        _config = config;
        _onSave = onSave;

        LoadValues();
        ApplyLocalization();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AnimationHelper.AnimateWindowEntrance(this, MasterDialogBorder, Opacity);
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

        // City (Default to current city from config)
        _selectedCity = string.IsNullOrWhiteSpace(_config.Widget.City) ? "auto" : _config.Widget.City;
        UpdateCityDisplay();

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
        ChkWeather.IsChecked = _config.Widgets.ShowWeather;
        ChkClipboard.IsChecked = _config.Widgets.ShowClipboard;
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

    private void UpdateCityDisplay()
    {
        if (TxtSelectedCity == null) return;

        if (string.Equals(_selectedCity, "auto", StringComparison.OrdinalIgnoreCase))
        {
            TxtSelectedCity.Text = LocalizationService.CurrentLanguage == "en"
                ? "Auto Detect (IP)"
                : "Otomatik Algıla (IP)";
        }
        else
        {
            TxtSelectedCity.Text = _selectedCity;
        }
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

        UpdateCityDisplay();
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

    private void BtnOpacityPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null && double.TryParse(btn.Tag.ToString(), out var presetValue))
        {
            SliderOpacity.Value = presetValue;
        }
    }

    private void BtnCityDropdown_Click(object sender, RoutedEventArgs e)
    {
        if (PopupCityDirectory == null) return;

        if (PopupCityDirectory.IsOpen)
        {
            PopupCityDirectory.IsOpen = false;
            BtnCityDropdown.IsChecked = false;
        }
        else
        {
            TxtCitySearch.Text = "";
            TxtSearchPlaceholder.Visibility = Visibility.Visible;
            ListCountryGroups.ItemsSource = WorldCityService.GetAll();
            PopupCityDirectory.IsOpen = true;
            BtnCityDropdown.IsChecked = true;

            Dispatcher.BeginInvoke(() => TxtCitySearch.Focus(), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void PopupCityDirectory_Closed(object sender, EventArgs e)
    {
        if (BtnCityDropdown != null)
        {
            BtnCityDropdown.IsChecked = false;
        }
    }

    private void TxtCitySearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TxtSearchPlaceholder != null)
        {
            TxtSearchPlaceholder.Visibility = string.IsNullOrEmpty(TxtCitySearch.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (ListCountryGroups != null)
        {
            ListCountryGroups.ItemsSource = WorldCityService.Filter(TxtCitySearch.Text);
        }
    }

    private void CityRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is string cityName)
        {
            _selectedCity = cityName;
            UpdateCityDisplay();
            PopupCityDirectory.IsOpen = false;
            BtnCityDropdown.IsChecked = false;
        }
    }

    private void BtnAutoDetectCity_Click(object sender, MouseButtonEventArgs e)
    {
        _selectedCity = "auto";
        UpdateCityDisplay();
        PopupCityDirectory.IsOpen = false;
        BtnCityDropdown.IsChecked = false;
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        CmbLanguage.SelectedIndex = 0; // TR
        _selectedCity = "auto";
        UpdateCityDisplay();
        SliderOpacity.Value = 100;
        CmbAccent.SelectedIndex = 0; // Sky
        RadioModeFull.IsChecked = true;
        RadioModeCompact.IsChecked = false;
        ChkHeader.IsChecked = true;
        ChkWeather.IsChecked = true;
        ChkClipboard.IsChecked = true;
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
        if (_isClosing) return;
        _isClosing = true;
        AnimationHelper.AnimateWindowExit(this, MasterDialogBorder);
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;

        // 1. Language
        if (CmbLanguage.SelectedItem is ComboBoxItem langItem && langItem.Tag != null)
        {
            _config.Widget.Language = langItem.Tag.ToString() ?? "tr";
            LocalizationService.CurrentLanguage = _config.Widget.Language;
        }

        // 2. City (from worldwide directory)
        _config.Widget.City = string.IsNullOrWhiteSpace(_selectedCity) ? "auto" : _selectedCity;

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
        _config.Widgets.ShowWeather = ChkWeather.IsChecked == true;
        _config.Widgets.ShowClipboard = ChkClipboard.IsChecked == true;
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

        // Persist settings immediately and synchronously
        _onSave?.Invoke(_config);

        AnimationHelper.AnimateWindowExit(this, MasterDialogBorder);
    }
}
