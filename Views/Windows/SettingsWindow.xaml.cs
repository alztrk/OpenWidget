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
    private readonly HardwareSpecs _hardwareSpecs;
    private string _selectedCity = "auto";
    private bool _isClosing;

    public SettingsWindow(HardwareConfig config, HardwareSpecs hardwareSpecs, Action<HardwareConfig> onSave)
    {
        InitializeComponent();
        _config = config;
        _hardwareSpecs = hardwareSpecs;
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
        ChkModularCards.IsChecked = _config.Widget.UseModularCards;
        ChkClipboardHeight.IsChecked = _config.Widget.LimitClipboardHeight;
        ChkClipboardExpanded.IsChecked = _config.Widget.IsClipboardHistoryExpanded;
        SelectCurrency(_config.Widget.Currency);
        ChkHardwareCore.IsChecked = _config.HardwareVisibility.CoreSystem;
        ChkHardwareStorage.IsChecked = _config.HardwareVisibility.Storage;
        ChkHardwarePeripherals.IsChecked = _config.HardwareVisibility.Peripherals;
        ChkHardwareCooling.IsChecked = _config.HardwareVisibility.CoolingAndPower;
        ChkHardwareAccessories.IsChecked = _config.HardwareVisibility.Accessories;
        RefreshHardwareLists();
    }

    private void RefreshHardwareLists()
    {
        ListHardware.ItemsSource = BuildHardwareItems();
    }

    private List<SettingsHardwareItem> BuildHardwareItems()
    {
        var manualUrls = _config.ManualHardware
            .Select(item => item.AmazonUrl)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hiddenHardware = _config.HiddenHardware.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = _hardwareSpecs.CoreSystem
            .Concat(_hardwareSpecs.Storage)
            .Concat(_hardwareSpecs.Peripherals)
            .Concat(_hardwareSpecs.CoolingAndPower)
            .Concat(_hardwareSpecs.Accessories)
            .Where(item => !hiddenHardware.Contains(HardwareScanner.GetHardwareKey(item)))
            .Select(item => new SettingsHardwareItem
            {
                Name = item.Value,
                Category = GetCategoryLabel(item.Category),
                Price = item.PriceFormatted ?? "-",
                Url = item.Url,
                IsManual = item.Url != null && manualUrls.Contains(item.Url),
                Source = item.Url != null && manualUrls.Contains(item.Url)
                    ? LocalizationService.Get("HardwareSourceConfig")
                    : LocalizationService.Get("HardwareSourceDetected"),
                PriceStatus = GetPriceStatusLabel(HardwareScanner.GetHardwareKey(item)),
                Key = HardwareScanner.GetHardwareKey(item),
                Link = item.Url ?? string.Empty
            })
            .ToList();

        foreach (var item in _config.ManualHardware)
        {
            var manualKey = $"{HardwareCategory.Normalize(item.Category)}:{item.Name}";
            var existing = items.FirstOrDefault(existingItem =>
                string.Equals(existingItem.Key, manualKey, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Link = item.AmazonUrl;
                continue;
            }

            items.Add(new SettingsHardwareItem
            {
                Name = item.Name,
                Category = GetCategoryLabel(item.Category),
                Price = item.Price > 0 ? $"{item.Price:N0} ₺" : "-",
                Url = item.AmazonUrl,
                IsManual = true,
                Source = LocalizationService.Get("HardwareSourceConfig"),
                PriceStatus = LocalizationService.Get("PriceStatusManual"),
                Key = manualKey,
                Link = item.AmazonUrl
            });
        }

        return items;
    }

    private static string GetCategoryLabel(string category)
    {
        return HardwareCategory.Normalize(category) switch
        {
            HardwareCategory.CoreSystem => LocalizationService.Get("HardwareCategoryCoreSystem"),
            HardwareCategory.Storage => LocalizationService.Get("HardwareCategoryStorage"),
            HardwareCategory.Peripherals => LocalizationService.Get("HardwareCategoryPeripherals"),
            HardwareCategory.CoolingAndPower => LocalizationService.Get("HardwareCategoryCoolingAndPower"),
            _ => LocalizationService.Get("HardwareCategoryAccessories")
        };
    }

    private string GetPriceStatusLabel(string key)
    {
        var status = _config.Prices.Entries.TryGetValue(key, out var entry)
            ? entry.Status
            : PriceStatuses.Estimated;

        return status switch
        {
            PriceStatuses.Live => LocalizationService.Get("PriceStatusLive"),
            PriceStatuses.Stale => LocalizationService.Get("PriceStatusStale"),
            PriceStatuses.Estimated => LocalizationService.Get("PriceStatusEstimated"),
            PriceStatuses.Manual => LocalizationService.Get("PriceStatusManual"),
            _ => LocalizationService.Get("PriceStatusUnavailable")
        };
    }

    private void UpdateCityDisplay()
    {
        if (TxtSelectedCity == null) return;

        if (string.Equals(_selectedCity, "auto", StringComparison.OrdinalIgnoreCase))
        {
            TxtSelectedCity.Text = LocalizationService.Get("AutoDetectIp");
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
        HardwareCategoryAccessories.Content = LocalizationService.Get("HardwareCategoryAccessories");
        HardwareCategoryCoreSystem.Content = LocalizationService.Get("HardwareCategoryCoreSystem");
        HardwareCategoryStorage.Content = LocalizationService.Get("HardwareCategoryStorage");
        HardwareCategoryPeripherals.Content = LocalizationService.Get("HardwareCategoryPeripherals");
        HardwareCategoryCoolingAndPower.Content = LocalizationService.Get("HardwareCategoryCoolingAndPower");
        LblHardwareCategories.Text = LocalizationService.Get("HardwareCategoriesLabel");
        LblCurrentHardware.Text = LocalizationService.Get("CurrentHardwareLabel");
        LblCurrentHardwareHelp.Text = LocalizationService.Get("CurrentHardwareHelp");
        LblAddHardware.Text = LocalizationService.Get("AddHardwareLabel");
        LblAddHardwareHelp.Text = LocalizationService.Get("AddHardwareHelp");
        LblHardwareName.Text = LocalizationService.Get("HardwareNameLabel");
        LblHardwareCategory.Text = LocalizationService.Get("HardwareCategoryLabel");
        TxtCurrentVersion.Text = string.Format(
            LocalizationService.GetCulture(),
            LocalizationService.Get("UpdateVersionLabel"),
            UpdateCheckService.CurrentVersion);

        LblLanguage.Text = LocalizationService.Get("LanguageLabel");
        LblCity.Text = LocalizationService.Get("CityLabel");
        LblCityHelp.Text = LocalizationService.Get("CityHelp");
        LblOpacity.Text = LocalizationService.Get("OpacityLabel");
        LblAccent.Text = LocalizationService.Get("AccentThemeLabel");

        BtnReset.Content = LocalizationService.Get("ResetDefaultsBtn");
        BtnCancel.Content = LocalizationService.Get("Cancel");
        BtnSave.Content = LocalizationService.Get("SaveAndApplyBtn");

        UpdateCityDisplay();
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (PanelGeneral == null || PanelWidgets == null || PanelHardware == null || PanelSystem == null) return;

        PanelGeneral.Visibility = TabBtnGeneral.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelWidgets.Visibility = TabBtnWidgets.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelHardware.Visibility = TabBtnHardware.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
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

    private void ComponentComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ComboBox comboBox && !comboBox.IsDropDownOpen)
        {
            comboBox.IsDropDownOpen = true;
            e.Handled = true;
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
        ChkModularCards.IsChecked = false;
        ChkClipboardHeight.IsChecked = true;
        ChkClipboardExpanded.IsChecked = true;
        ChkHardwareCore.IsChecked = true;
        ChkHardwareStorage.IsChecked = true;
        ChkHardwarePeripherals.IsChecked = true;
        ChkHardwareCooling.IsChecked = true;
        ChkHardwareAccessories.IsChecked = true;
        _config.ManualHardware.Clear();
        _config.HiddenHardware.Clear();
        RefreshHardwareLists();
    }

    private void BtnAddHardware_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtHardwareName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(
                LocalizationService.CurrentLanguage == "en"
                    ? "Enter a product name."
                    : "Ürün adı girin.",
                "OpenWidget",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var category = HardwareCategory.Normalize((CmbHardwareCategory.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        _config.ManualHardware.Add(new ManualHardwareItem
        {
            Name = name,
            Category = category
        });
        RefreshHardwareLists();
        TxtHardwareName.Clear();
    }

    private void BtnRemoveHardwareCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is SettingsHardwareItem item)
        {
            var manual = _config.ManualHardware.FirstOrDefault(manualItem =>
                string.Equals(manualItem.AmazonUrl, item.Url, StringComparison.OrdinalIgnoreCase));
            if (manual != null)
            {
                _config.ManualHardware.Remove(manual);
            }
            else if (!string.IsNullOrWhiteSpace(item.Key) &&
                     !_config.HiddenHardware.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
            {
                _config.HiddenHardware.Add(item.Key);
            }

            RefreshHardwareLists();
        }
    }

    private void BtnUpdateHardwareLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not SettingsHardwareItem item)
        {
            return;
        }

        return;
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
        _config.Widget.UseModularCards = ChkModularCards.IsChecked == true;
        _config.Widget.LimitClipboardHeight = ChkClipboardHeight.IsChecked == true;
        _config.Widget.IsClipboardHistoryExpanded = ChkClipboardExpanded.IsChecked == true;
        _config.Widget.Currency = (CmbCurrency.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "TRY";
        _config.HardwareVisibility.CoreSystem = ChkHardwareCore.IsChecked == true;
        _config.HardwareVisibility.Storage = ChkHardwareStorage.IsChecked == true;
        _config.HardwareVisibility.Peripherals = ChkHardwarePeripherals.IsChecked == true;
        _config.HardwareVisibility.CoolingAndPower = ChkHardwareCooling.IsChecked == true;
        _config.HardwareVisibility.Accessories = ChkHardwareAccessories.IsChecked == true;

        var autoStart = ChkAutoStart.IsChecked == true;
        if (AutoStartService.IsAutoStartEnabled() != autoStart)
        {
            AutoStartService.SetAutoStart(autoStart);
        }

        // Persist settings immediately and synchronously
        _onSave?.Invoke(_config);

        AnimationHelper.AnimateWindowExit(this, MasterDialogBorder);
    }

    private void SelectCurrency(string currency)
    {
        foreach (ComboBoxItem item in CmbCurrency.Items)
        {
            if (string.Equals(item.Tag?.ToString(), currency, StringComparison.OrdinalIgnoreCase))
            {
                CmbCurrency.SelectedItem = item;
                return;
            }
        }

        CmbCurrency.SelectedIndex = 0;
    }

    private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnCheckUpdate.IsEnabled = false;
        TxtUpdateStatus.Text = LocalizationService.Get("CheckingUpdates");

        var result = await UpdateCheckService.CheckForUpdatesAsync();

        BtnCheckUpdate.IsEnabled = true;

        if (result.IsUpdateAvailable)
        {
            TxtUpdateStatus.Text = string.Format(
                LocalizationService.Get("LatestVersionAvailable"),
                result.LatestVersion);

            BtnCheckUpdate.Content = LocalizationService.Get("DownloadUpdate");
            BtnCheckUpdate.Click -= BtnCheckUpdate_Click;
            BtnCheckUpdate.Click += (s, args) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = result.ReleaseUrl,
                        UseShellExecute = true
                    });
                }
                catch { }
            };
        }
        else if (result.IsSuccess)
        {
            TxtUpdateStatus.Text = LocalizationService.Get("LatestVersionInstalled");
        }
        else
        {
            TxtUpdateStatus.Text = string.Format(
                LocalizationService.Get("UpdateCheckFailed"),
                result.ErrorMessage ?? LocalizationService.Get("Offline"));
        }
    }
}
