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
    private const int WM_MOVING = 0x0216;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

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
    private MediaSessionService? _mediaService;
    private readonly ClipboardService _clipboardService = new();

    private DispatcherTimer? _telemetryTimer;
    private DispatcherTimer? _financeTimer;

    private bool _isGamingThrottled = false;
    private int _gamingThrottleCounter = 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ClipboardService Clipboard => _clipboardService;
    public System.Collections.ObjectModel.ObservableCollection<ClipboardItem> ClipboardItems => _clipboardService.Items;
    public WeatherDetailsModel Weather => _weatherService?.GetDetails() ?? new WeatherDetailsModel();
    public string CompactWeatherText => _weatherService != null ? $"{_weatherService.GetDetails().City} {_weatherService.GetDetails().TempFormatted}" : "İstanbul 17°";

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompactCpuText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompactGpuText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompactRamText)));
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
    public bool IsWeatherVisible => _config.Widgets.ShowWeather;
    public bool IsClipboardVisible => _config.Widgets.ShowClipboard;
    public bool IsTelemetryVisible => _config.Widgets.ShowTelemetry;
    public bool IsFinanceVisible => _config.Widgets.ShowFinance;
    public bool IsMediaVisible => _config.Widgets.ShowMedia;
    public bool IsHardwareVisible => _config.Widgets.ShowHardware;
    public bool IsQuickActionsVisible => _config.Widgets.ShowQuickActions;

    public bool IsCompactViewVisible => _config.Widget.IsCompact;
    public bool IsFullViewVisible => !_config.Widget.IsCompact;

    public string CompactCpuText => GetCompactTelemetryText("cpu", "CPU");
    public string CompactGpuText => GetCompactTelemetryText("gpu", "GPU");
    public string CompactRamText => GetCompactTelemetryText("ram", "RAM");

    private string GetCompactTelemetryText(string key, string fallbackLabel)
    {
        if (_telemetry?.Performance != null)
        {
            foreach (var item in _telemetry.Performance)
            {
                if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{fallbackLabel} {item.MainText}";
                }
            }
        }
        return $"{fallbackLabel} --";
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LocalizationService.LanguageChanged += OnLanguageChanged;
        ResolvePaths();
        LoadConfig();
    }

    private void OnLanguageChanged()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateContextMenuTexts();
            RefreshTelemetry();
            _ = RefreshFinanceLiveAsync();
        });
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

        // Attach Win32 Hook to prevent window from crossing screen boundaries
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == ClipboardService.WM_CLIPBOARDUPDATE)
        {
            _clipboardService.HandleClipboardUpdate();
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_MOVING)
        {
            var rect = Marshal.PtrToStructure<RECT>(lParam);
            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (GetMonitorInfo(monitor, ref mi))
                {
                    var work = mi.rcWork;
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    const int snapDistance = 12;

                    // Clamp & Snap Horizontal (Left / Right)
                    if (rect.Left < work.Left || Math.Abs(rect.Left - work.Left) <= snapDistance)
                    {
                        rect.Left = work.Left;
                        rect.Right = work.Left + width;
                    }
                    else if (rect.Right > work.Right || Math.Abs(rect.Right - work.Right) <= snapDistance)
                    {
                        rect.Right = work.Right;
                        rect.Left = work.Right - width;
                    }

                    // Clamp & Snap Vertical (Top / Bottom)
                    if (rect.Top < work.Top || Math.Abs(rect.Top - work.Top) <= snapDistance)
                    {
                        rect.Top = work.Top;
                        rect.Bottom = work.Top + height;
                    }
                    else if (rect.Bottom > work.Bottom || Math.Abs(rect.Bottom - work.Bottom) <= snapDistance)
                    {
                        rect.Bottom = work.Bottom;
                        rect.Top = work.Bottom - height;
                    }

                    Marshal.StructureToPtr(rect, lParam, true);
                    handled = true;
                }
            }
        }

        return IntPtr.Zero;
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
        _weatherService = new WeatherService(_weatherCachePath, _config.Widget.City);
        _weatherService.WeatherUpdated += (details) =>
        {
            Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Weather)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompactWeatherText)));
            });
        };
    }

    private void LoadConfig()
    {
        _config = HardwareConfig.Load(_configPath);

        Width = 360;

        if (_config.Widget.Opacity > 0.1 && _config.Widget.Opacity <= 1.0) Opacity = _config.Widget.Opacity;
        Topmost = _config.Widget.AlwaysOnTop;

        ThemeService.ApplyAccent(_config.Widget.AccentTheme);
        LocalizationService.CurrentLanguage = _config.Widget.Language;
        _weatherService?.UpdateCity(_config.Widget.City);

        ApplyLockState();
        ApplyDesktopPinning();
        NotifyCompactState();

        UpdateContextMenuTexts();
        SyncMenuCheckboxes();
    }

    private void ApplyLockState()
    {
        if (MasterBorder != null)
        {
            MasterBorder.Cursor = _config.Widget.IsLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
        }
    }

    private void ApplyDesktopPinning()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero && _config.Widget.PinToDesktop)
        {
            SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
    }

    private void NotifyCompactState()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompactViewVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFullViewVisible)));
    }

    private void UpdateContextMenuTexts()
    {
        if (MenuHeader != null) MenuHeader.Header = LocalizationService.Get("MenuHeader");
        if (MenuWeather != null) MenuWeather.Header = LocalizationService.Get("MenuWeather");
        if (MenuClipboard != null) MenuClipboard.Header = LocalizationService.Get("MenuClipboard");
        if (MenuTelemetry != null) MenuTelemetry.Header = LocalizationService.Get("MenuTelemetry");
        if (MenuFinance != null) MenuFinance.Header = LocalizationService.Get("MenuFinance");
        if (MenuMedia != null) MenuMedia.Header = LocalizationService.Get("MenuMedia");
        if (MenuHardware != null) MenuHardware.Header = LocalizationService.Get("MenuHardware");
        if (MenuQuickActions != null) MenuQuickActions.Header = LocalizationService.Get("MenuQuickActions");
        if (MenuCompactMode != null) MenuCompactMode.Header = LocalizationService.Get("MenuCompactMode");
        if (MenuLockPosition != null) MenuLockPosition.Header = LocalizationService.Get("MenuLockPosition");
        if (MenuPinToDesktop != null) MenuPinToDesktop.Header = LocalizationService.Get("MenuPinToDesktop");
        if (MenuAutoStart != null) MenuAutoStart.Header = LocalizationService.Get("MenuAutoStart");
        if (MenuAlwaysOnTop != null) MenuAlwaysOnTop.Header = LocalizationService.Get("MenuAlwaysOnTop");
        if (MenuGameMode != null) MenuGameMode.Header = LocalizationService.Get("MenuGameMode");
        if (MenuSettings != null) MenuSettings.Header = LocalizationService.Get("MenuSettings");
        if (MenuRefresh != null) MenuRefresh.Header = LocalizationService.Get("MenuRefresh");
        if (MenuResetPos != null) MenuResetPos.Header = LocalizationService.Get("MenuResetPos");
        if (MenuPrices != null) MenuPrices.Header = LocalizationService.Get("MenuPrices");
        if (MenuExit != null) MenuExit.Header = LocalizationService.Get("MenuExit");
    }

    private void SyncMenuCheckboxes()
    {
        if (MenuHeader != null) MenuHeader.IsChecked = _config.Widgets.ShowHeader;
        if (MenuWeather != null) MenuWeather.IsChecked = _config.Widgets.ShowWeather;
        if (MenuClipboard != null) MenuClipboard.IsChecked = _config.Widgets.ShowClipboard;
        if (MenuTelemetry != null) MenuTelemetry.IsChecked = _config.Widgets.ShowTelemetry;
        if (MenuFinance != null) MenuFinance.IsChecked = _config.Widgets.ShowFinance;
        if (MenuMedia != null) MenuMedia.IsChecked = _config.Widgets.ShowMedia;
        if (MenuHardware != null) MenuHardware.IsChecked = _config.Widgets.ShowHardware;
        if (MenuQuickActions != null) MenuQuickActions.IsChecked = _config.Widgets.ShowQuickActions;
        if (MenuCompactMode != null) MenuCompactMode.IsChecked = _config.Widget.IsCompact;
        if (MenuLockPosition != null) MenuLockPosition.IsChecked = _config.Widget.IsLocked;
        if (MenuPinToDesktop != null) MenuPinToDesktop.IsChecked = _config.Widget.PinToDesktop;
        if (MenuAlwaysOnTop != null) MenuAlwaysOnTop.IsChecked = Topmost;
        if (MenuGameMode != null) MenuGameMode.IsChecked = _config.Widget.AutoGameMode;
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
        _telemetryTimer.Tick += (s, args) => OnTelemetryTimerTick();
        _telemetryTimer.Start();

        // 2. Initial Finance & Timer (60s)
        RefreshFinanceFromCache();
        _financeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _financeTimer.Tick += async (s, args) => await RefreshFinanceLiveAsync();
        _financeTimer.Start();
        _ = RefreshFinanceLiveAsync();

        // 3. Hardware Specs
        await RefreshHardwareSpecsAsync();

        // 4. Media Controller Service
        try
        {
            _mediaService = new MediaSessionService();
            await _mediaService.InitializeAsync();
            MediaControlWidget.AttachService(_mediaService);
        }
        catch { }

        // 5. Windows Clipboard Service
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            _clipboardService.Initialize(handle);
        }
        catch { }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(PositionWidget);
    }

    public void PositionWidget()
    {
        var workArea = SystemParameters.WorkArea;
        Width = 360;
        double h = ActualHeight > 0 ? ActualHeight : 450;

        if (_config.Widget.CustomLeft.HasValue && _config.Widget.CustomTop.HasValue)
        {
            double maxLeft = Math.Max(workArea.Left, workArea.Right - Width);
            double maxTop = Math.Max(workArea.Top, workArea.Bottom - h);

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
        if (_config.Widget.IsLocked) return;

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

    private void BtnToggleCompact_Click(object sender, RoutedEventArgs e)
    {
        ToggleCompactMode();
    }

    private void ToggleCompactMode()
    {
        var targetOpacity = _config.Widget.Opacity > 0.1 ? _config.Widget.Opacity : 1.0;
        var animOut = new System.Windows.Media.Animation.DoubleAnimation(targetOpacity, 0, TimeSpan.FromMilliseconds(120))
        {
            EasingFunction = Utils.AnimationHelper.EaseInOut
        };
        animOut.Completed += (_, _) =>
        {
            _config.Widget.IsCompact = !_config.Widget.IsCompact;
            _config.Save(_configPath);
            SyncMenuCheckboxes();
            NotifyCompactState();

            var animIn = new System.Windows.Media.Animation.DoubleAnimation(0, targetOpacity, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = Utils.AnimationHelper.EaseInOut
            };
            MasterBorder.BeginAnimation(UIElement.OpacityProperty, animIn);
        };
        MasterBorder.BeginAnimation(UIElement.OpacityProperty, animOut);
    }

    private void SaveCurrentPosition()
    {
        var workArea = SystemParameters.WorkArea;
        double w = ActualWidth > 0 ? ActualWidth : Width;
        double h = ActualHeight > 0 ? ActualHeight : 450;

        double maxLeft = Math.Max(workArea.Left, workArea.Right - w);
        double maxTop = Math.Max(workArea.Top, workArea.Bottom - h);

        Left = Math.Clamp(Left, workArea.Left, maxLeft);
        Top = Math.Clamp(Top, workArea.Top, maxTop);

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

    private void OnTelemetryTimerTick()
    {
        if (_config.Widget.AutoGameMode)
        {
            var handle = new WindowInteropHelper(this).Handle;
            bool isGameFullscreen = GameModeService.IsFullscreenAppRunning(handle);

            if (isGameFullscreen)
            {
                if (!_isGamingThrottled)
                {
                    _isGamingThrottled = true;
                    _gamingThrottleCounter = 0;
                    TrimMemory();
                }

                _gamingThrottleCounter++;
                // Throttle polling: query only once every 10 ticks (3s * 10 = 30s) while gaming
                if (_gamingThrottleCounter < 10)
                {
                    if (Telemetry != null && !Telemetry.IsGamingMode)
                    {
                        Telemetry.IsGamingMode = true;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Telemetry)));
                    }
                    return;
                }
                _gamingThrottleCounter = 0;
            }
            else
            {
                if (_isGamingThrottled)
                {
                    _isGamingThrottled = false;
                    _gamingThrottleCounter = 0;
                }
            }
        }
        else
        {
            _isGamingThrottled = false;
        }

        RefreshTelemetry();
    }

    private void RefreshTelemetry()
    {
        if (_telemetryService != null)
        {
            var specs = _telemetryService.GetTelemetrySpecs();
            specs.IsGamingMode = _isGamingThrottled;
            Telemetry = specs;
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

    private void MenuWeather_Click(object sender, RoutedEventArgs e)
    {
        _config.Widgets.ShowWeather = !_config.Widgets.ShowWeather;
        SaveAndNotifyLayout(nameof(IsWeatherVisible), MenuWeather, _config.Widgets.ShowWeather);
    }

    private void MenuClipboard_Click(object sender, RoutedEventArgs e)
    {
        _config.Widgets.ShowClipboard = !_config.Widgets.ShowClipboard;
        SaveAndNotifyLayout(nameof(IsClipboardVisible), MenuClipboard, _config.Widgets.ShowClipboard);
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

    private void MenuMedia_Click(object sender, RoutedEventArgs e)
    {
        _config.Widgets.ShowMedia = !_config.Widgets.ShowMedia;
        SaveAndNotifyLayout(nameof(IsMediaVisible), MenuMedia, _config.Widgets.ShowMedia);
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

    private void MenuCompactMode_Click(object sender, RoutedEventArgs e)
    {
        ToggleCompactMode();
    }

    private void MenuLockPosition_Click(object sender, RoutedEventArgs e)
    {
        _config.Widget.IsLocked = !_config.Widget.IsLocked;
        _config.Save(_configPath);
        ApplyLockState();
        SyncMenuCheckboxes();
    }

    private void MenuPinToDesktop_Click(object sender, RoutedEventArgs e)
    {
        _config.Widget.PinToDesktop = !_config.Widget.PinToDesktop;
        if (_config.Widget.PinToDesktop)
        {
            _config.Widget.AlwaysOnTop = false;
            Topmost = false;
        }
        _config.Save(_configPath);
        ApplyDesktopPinning();
        SyncMenuCheckboxes();
    }

    private void SaveAndNotifyLayout(string propertyName, MenuItem? menuItem, bool isChecked)
    {
        _config.Save(_configPath);
        if (menuItem != null) menuItem.IsChecked = isChecked;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void MenuGameMode_Click(object sender, RoutedEventArgs e)
    {
        _config.Widget.AutoGameMode = !_config.Widget.AutoGameMode;
        _config.Save(_configPath);
        if (MenuGameMode != null) MenuGameMode.IsChecked = _config.Widget.AutoGameMode;
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

    private void MenuSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWin = new Windows.SettingsWindow(_config, OnSettingsSaved);
        settingsWin.Owner = this;
        settingsWin.ShowDialog();
    }

    private void OnSettingsSaved(HardwareConfig updated)
    {
        _config = updated;
        _config.Save(_configPath);

        Opacity = _config.Widget.Opacity;
        Topmost = _config.Widget.AlwaysOnTop;
        ThemeService.ApplyAccent(_config.Widget.AccentTheme);
        LocalizationService.CurrentLanguage = _config.Widget.Language;
        _weatherService?.UpdateCity(_config.Widget.City);

        ApplyLockState();
        ApplyDesktopPinning();
        NotifyCompactState();

        UpdateContextMenuTexts();
        SyncMenuCheckboxes();

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHeaderVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWeatherVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsClipboardVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTelemetryVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFinanceVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMediaVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHardwareVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsQuickActionsVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Weather)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompactWeatherText)));

        RefreshTelemetry();
        _ = RefreshFinanceLiveAsync();
        _ = RefreshHardwareSpecsAsync();
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
        _mediaService?.Dispose();
        try { _clipboardService.Cleanup(new WindowInteropHelper(this).Handle); } catch { }
        base.OnClosed(e);
    }
}
