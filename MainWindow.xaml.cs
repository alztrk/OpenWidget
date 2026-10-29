using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using RigBadge.Models;
using RigBadge.Services;

namespace RigBadge;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const int GWL_EXSTYLE = -20;
    private const int GWL_HWNDPARENT = -8;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    private HardwareConfig _config = new();
    private HardwareSpecs _specs = new();
    private TelemetrySpecs _telemetry = new();
    private FinanceSpecs _finance = new();

    private string _configPath = string.Empty;
    private string _pricesPath = string.Empty;
    private string _financeCachePath = string.Empty;

    private PriceService? _priceService;
    private SystemTelemetryService? _telemetryService;
    private MarketFinanceService? _financeService;

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

    public bool IsTelemetryVisible => _config.TelemetryWidget.Enabled;
    public bool IsFinanceVisible => _config.FinanceWidget.Enabled;

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

        _priceService = new PriceService(_pricesPath);
        _telemetryService = new SystemTelemetryService();
        _financeService = new MarketFinanceService(_financeCachePath);
    }

    private void LoadConfig()
    {
        _config = HardwareConfig.Load(_configPath);

        Width = 380;

        if (_config.Widget.Opacity > 0.1 && _config.Widget.Opacity <= 1.0) Opacity = _config.Widget.Opacity;
        Topmost = _config.Widget.AlwaysOnTop;

        if (MenuAlwaysOnTop != null)
        {
            MenuAlwaysOnTop.IsChecked = Topmost;
        }
        if (MenuTelemetry != null)
        {
            MenuTelemetry.IsChecked = _config.TelemetryWidget.Enabled;
        }
        if (MenuFinance != null)
        {
            MenuFinance.IsChecked = _config.FinanceWidget.Enabled;
        }
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

        // 1. Initial Telemetry & Timer (2s)
        RefreshTelemetry();
        _telemetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
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

        // Auto-calculate 2.5% margins based on current resolution
        double rightMargin = workArea.Width * 0.025;
        double topMargin = workArea.Height * 0.025;

        Width = 380;

        Left = Math.Round(workArea.Right - Width - rightMargin);
        Top = Math.Round(workArea.Top + topMargin);
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
            Finance = _financeService.GetCurrentSpecs();
        }
    }

    private async Task RefreshFinanceLiveAsync()
    {
        if (_financeService != null)
        {
            Finance = await _financeService.FetchLatestAsync();
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

    private void MenuTelemetry_Click(object sender, RoutedEventArgs e)
    {
        _config.TelemetryWidget.Enabled = !_config.TelemetryWidget.Enabled;
        _config.Save(_configPath);
        if (MenuTelemetry != null) MenuTelemetry.IsChecked = _config.TelemetryWidget.Enabled;

        NotifyLayoutChanged();
    }

    private void MenuFinance_Click(object sender, RoutedEventArgs e)
    {
        _config.FinanceWidget.Enabled = !_config.FinanceWidget.Enabled;
        _config.Save(_configPath);
        if (MenuFinance != null) MenuFinance.IsChecked = _config.FinanceWidget.Enabled;

        NotifyLayoutChanged();
    }

    private void NotifyLayoutChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTelemetryVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFinanceVisible)));
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

    private void OpenFileWithDefaultEditor(string path)
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
                MessageBox.Show($"Dosya bulunamadı:\n{path}", "RigBadge", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dosya açılamadı: {ex.Message}", "RigBadge", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Link_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void Link_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement elem && elem.Tag is string url && !string.IsNullOrEmpty(url))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
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
