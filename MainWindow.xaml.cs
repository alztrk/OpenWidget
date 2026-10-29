using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using OpenWidget.Models;
using OpenWidget.Services;

namespace OpenWidget;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const int GWL_EXSTYLE = -20;
    private const int GWL_HWNDPARENT = -8;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    private HardwareConfig _config = new();
    private HardwareSpecs _specs = new();
    private TelemetrySpecs _telemetry = new();
    private FinanceSpecs _finance = new();

    private string _configPath = string.Empty;
    private string _pricesPath = string.Empty;
    private string _financeCachePath = string.Empty;
    private string _weatherCachePath = string.Empty;

    private PriceService? _priceService;
    private SystemTelemetryService? _telemetryService;
    private MarketFinanceService? _financeService;
    private WeatherService? _weatherService;

    private DispatcherTimer? _telemetryTimer;
    private DispatcherTimer? _financeTimer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public HardwareSpecs Specs
    {
        get => _specs;
        set
        {
            _specs = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Specs)));
        }
    }

    public TelemetrySpecs Telemetry
    {
        get => _telemetry;
        set
        {
            _telemetry = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Telemetry)));
        }
    }

    public FinanceSpecs Finance
    {
        get => _finance;
        set
        {
            _finance = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Finance)));
        }
    }

    public bool IsHeaderVisible => _config.Widgets.ShowHeader;
    public bool IsTelemetryVisible => _config.Widgets.ShowTelemetry;
    public bool IsFinanceVisible => _config.Widgets.ShowFinance;
    public bool IsHardwareVisible => _config.Widgets.ShowHardware;
    public bool IsQuickActionsVisible => _config.Widgets.ShowQuickActions;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        ResolvePaths();
        LoadConfig();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;

        // Apply ToolWindow (hide from Alt+Tab) and NoActivate (no focus stealing)
        var exStyle = (long)GetWindowLongPtr(handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLongPtr(handle, GWL_EXSTYLE, (IntPtr)exStyle);

        // Bind window ownership to Windows Desktop Shell (Progman)
        var progman = FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            SetWindowLongPtr(handle, GWL_HWNDPARENT, progman);
        }
    }

    private void ResolvePaths()
    {
        var baseDir = AppContext.BaseDirectory;

        _configPath = Path.Combine(baseDir, "hardware-config.json");
        if (!File.Exists(_configPath))
        {
            var cwdPath = Path.Combine(Environment.CurrentDirectory, "hardware-config.json");
            if (File.Exists(cwdPath)) _configPath = cwdPath;
        }

        _pricesPath = Path.Combine(baseDir, "hardware-prices.json");
        if (!File.Exists(_pricesPath))
        {
            var cwdPrices = Path.Combine(Environment.CurrentDirectory, "hardware-prices.json");
            if (File.Exists(cwdPrices)) _pricesPath = cwdPrices;
        }

        _financeCachePath = Path.Combine(baseDir, "finance-cache.json");
        _weatherCachePath = Path.Combine(baseDir, "weather-cache.json");

        _priceService = new PriceService(_pricesPath);
        _telemetryService = new SystemTelemetryService();
        _financeService = new MarketFinanceService(_financeCachePath);
        _weatherService = new WeatherService(_weatherCachePath);
    }

    private void LoadConfig()
    {
        _config = HardwareConfig.Load(_configPath);

        Width = 360;

        if (_config.Widget.Opacity > 0.1 && _config.Widget.Opacity <= 1.0) Opacity = _config.Widget.Opacity;
        Topmost = _config.Widget.AlwaysOnTop;

        SyncMenuCheckboxes();
    }

    private void SyncMenuCheckboxes()
    {
        if (MenuHeader != null) MenuHeader.IsChecked = _config.Widgets.ShowHeader;
        if (MenuTelemetry != null) MenuTelemetry.IsChecked = _config.Widgets.ShowTelemetry;
        if (MenuFinance != null) MenuFinance.IsChecked = _config.Widgets.ShowFinance;
        if (MenuHardware != null) MenuHardware.IsChecked = _config.Widgets.ShowHardware;
        if (MenuQuickActions != null) MenuQuickActions.IsChecked = _config.Widgets.ShowQuickActions;
        if (MenuAlwaysOnTop != null) MenuAlwaysOnTop.IsChecked = Topmost;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWidget();

        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        // Ensure auto-start on boot is registered
        if (!AutoStartService.IsAutoStartEnabled())
        {
            AutoStartService.SetAutoStart(true);
        }
        if (MenuAutoStart != null)
        {
            MenuAutoStart.IsChecked = AutoStartService.IsAutoStartEnabled();
        }

        // 1. Initial Telemetry & Timer (3s)
        RefreshTelemetry();
        _telemetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _telemetryTimer.Tick += (s, args) => RefreshTelemetry();
        _telemetryTimer.Start();

        // 2. Initial Finance & Timer (60s)
        RefreshFinanceFromCache();
        _financeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _financeTimer.Tick += async (s, args) => await RefreshFinanceLiveAsync();
        _financeTimer.Start();
        _ = RefreshFinanceLiveAsync();

        // 3. Hardware Specs
        await RefreshHardwareSpecsAsync();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(PositionWidget);
    }

    public void PositionWidget()
    {
        var workArea = SystemParameters.WorkArea;
        Width = 360;

        if (_config.Widget.CustomLeft.HasValue && _config.Widget.CustomTop.HasValue)
        {
            double maxLeft = Math.Max(0, workArea.Right - Width);
            double maxTop = Math.Max(0, workArea.Bottom - 100);

            Left = Math.Clamp(_config.Widget.CustomLeft.Value, workArea.Left, maxLeft);
            Top = Math.Clamp(_config.Widget.CustomTop.Value, workArea.Top, maxTop);
        }
        else
        {
            // Auto-calculate 2.5% margins based on current resolution
            double rightMargin = workArea.Width * 0.025;
            double topMargin = workArea.Height * 0.025;

            Left = Math.Round(workArea.Right - Width - rightMargin);
            Top = Math.Round(workArea.Top + topMargin);
        }
    }

    private void MasterBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle != IntPtr.Zero)
            {
                ReleaseCapture();
                SendMessage(handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                SaveCurrentPosition();
            }
        }
    }

    private void SaveCurrentPosition()
    {
        _config.Widget.CustomLeft = Math.Round(Left);
        _config.Widget.CustomTop = Math.Round(Top);
        _config.Save(_configPath);
    }

    private void MenuResetPosition_Click(object sender, RoutedEventArgs e)
    {
        _config.Widget.CustomLeft = null;
        _config.Widget.CustomTop = null;
        _config.Save(_configPath);
        PositionWidget();
    }

    private void RefreshTelemetry()
    {
        if (_telemetryService != null)
        {
            Telemetry = _telemetryService.GetTelemetrySpecs();
        }
    }

    private void RefreshFinanceFromCache()
    {
        if (_financeService != null)
        {
            var specs = _financeService.GetCurrentSpecs();
            if (_weatherService != null)
            {
                specs.WeatherText = _weatherService.GetCurrentWeather();
            }
            Finance = specs;
        }
    }

    private async Task RefreshFinanceLiveAsync()
    {
        if (_financeService != null)
        {
            var specs = await _financeService.FetchLatestAsync();
            if (_weatherService != null)
            {
                specs.WeatherText = await _weatherService.FetchLatestAsync();
            }
            Finance = specs;
        }
    }

    private async Task RefreshHardwareSpecsAsync()
    {
        try
        {
            _priceService ??= new PriceService(_pricesPath);
            _priceService.CheckAndApplyDailyUpdate();

            await _priceService.UpdateIzolyPricesAsync();

            var scanner = new HardwareScanner(_config, _priceService);
            var result = await Task.Run(() => scanner.Scan());
            Specs = result;

            TrimMemory();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hardware scan error: {ex.Message}");
        }
    }

    private static void TrimMemory()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }
        catch { }
    }

    private void QuickActionsWidget_RamCleaned(object? sender, EventArgs e)
    {
        RefreshTelemetry();
    }

    // Interactive Widget Visibility Toggles
    private void MenuHeader_Click(object sender, RoutedEventArgs e)
    {
        _config.Widgets.ShowHeader = !_config.Widgets.ShowHeader;
        SaveAndNotifyLayout(nameof(IsHeaderVisible), MenuHeader, _config.Widgets.ShowHeader);
    }

    private void MenuTelemetry_Click(object sender, RoutedEventArgs e)
    {
        _config.Widgets.ShowTelemetry = !_config.Widgets.ShowTelemetry;
        SaveAndNotifyLayout(nameof(IsTelemetryVisible), MenuTelemetry, _config.Widgets.ShowTelemetry);
    }

    private void MenuFinance_Click(object sender, RoutedEventArgs e)
    {
        _config.Widgets.ShowFinance = !_config.Widgets.ShowFinance;
        SaveAndNotifyLayout(nameof(IsFinanceVisible), MenuFinance, _config.Widgets.ShowFinance);
    }

    private void MenuHardware_Click(object sender, RoutedEventArgs e)
    {
        _config.Widgets.ShowHardware = !_config.Widgets.ShowHardware;
        SaveAndNotifyLayout(nameof(IsHardwareVisible), MenuHardware, _config.Widgets.ShowHardware);
    }

    private void MenuQuickActions_Click(object sender, RoutedEventArgs e)
    {
        _config.Widgets.ShowQuickActions = !_config.Widgets.ShowQuickActions;
        SaveAndNotifyLayout(nameof(IsQuickActionsVisible), MenuQuickActions, _config.Widgets.ShowQuickActions);
    }

    private void SaveAndNotifyLayout(string propertyName, MenuItem? menuItem, bool isChecked)
    {
        _config.Save(_configPath);
        if (menuItem != null) menuItem.IsChecked = isChecked;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void MenuAutoStart_Click(object sender, RoutedEventArgs e)
    {
        var current = AutoStartService.IsAutoStartEnabled();
        AutoStartService.SetAutoStart(!current);
        if (MenuAutoStart != null)
        {
            MenuAutoStart.IsChecked = !current;
        }
    }

    private void MenuAlwaysOnTop_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        if (MenuAlwaysOnTop != null)
        {
            MenuAlwaysOnTop.IsChecked = Topmost;
        }
    }

    private async void MenuRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadConfig();
        RefreshTelemetry();
        _ = RefreshFinanceLiveAsync();
        _priceService = new PriceService(_pricesPath);
        await RefreshHardwareSpecsAsync();
    }

    private void MenuEditPrices_Click(object sender, RoutedEventArgs e)
    {
        OpenFileWithDefaultEditor(_pricesPath);
    }

    private void MenuEditConfig_Click(object sender, RoutedEventArgs e)
    {
        OpenFileWithDefaultEditor(_configPath);
    }

    private static void OpenFileWithDefaultEditor(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show($"Dosya bulunamadı:\n{path}", "OpenWidget", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dosya açılamadı: {ex.Message}", "OpenWidget", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _telemetryTimer?.Stop();
        _financeTimer?.Stop();
        _telemetryService?.Dispose();
        base.OnClosed(e);
    }
}
