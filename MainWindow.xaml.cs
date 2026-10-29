using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using RigBadge.Models;
using RigBadge.Services;
using RigBadge.Windows;

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

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    private HardwareConfig _config = new();
    private HardwareSpecs _specs = new();
    private string _configPath = string.Empty;
    private string _pricesPath = string.Empty;
    private PriceService? _priceService;
    private DispatcherTimer? _desktopDetectTimer;
    private TelemetryWindow? _telemetryWindow;
    private FinanceWindow? _financeWindow;

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

        _priceService = new PriceService(_pricesPath);
    }

    private void LoadConfig()
    {
        _config = HardwareConfig.Load(_configPath);

        if (_config.Widget.Width >= 200) Width = _config.Widget.Width;
        if (_config.Widget.Opacity > 0.1 && _config.Widget.Opacity <= 1.0) Opacity = _config.Widget.Opacity;
        Topmost = _config.Widget.AlwaysOnTop;

        if (MenuAlwaysOnTop != null)
        {
            MenuAlwaysOnTop.IsChecked = Topmost;
        }

        if (MenuOnlyDesktop != null)
        {
            MenuOnlyDesktop.IsChecked = _config.Widget.OnlyShowOnDesktop;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWidget();

        // Ensure auto-start on boot is registered
        if (!AutoStartService.IsAutoStartEnabled())
        {
            AutoStartService.SetAutoStart(true);
        }
        if (MenuAutoStart != null)
        {
            MenuAutoStart.IsChecked = AutoStartService.IsAutoStartEnabled();
        }

        if (MenuOnlyDesktop != null)
        {
            MenuOnlyDesktop.IsChecked = _config.Widget.OnlyShowOnDesktop;
        }

        InitSubWidgets();
        StartDesktopMonitor();

        await RefreshHardwareSpecsAsync();
    }

    private void InitSubWidgets()
    {
        if (_config.TelemetryWidget.Enabled)
        {
            _telemetryWindow = new TelemetryWindow(_config, _configPath);
            _telemetryWindow.Show();
        }
        if (MenuTelemetry != null)
        {
            MenuTelemetry.IsChecked = _config.TelemetryWidget.Enabled;
        }

        if (_config.FinanceWidget.Enabled)
        {
            _financeWindow = new FinanceWindow(_config, _configPath);
            _financeWindow.Show();
        }
        if (MenuFinance != null)
        {
            MenuFinance.IsChecked = _config.FinanceWidget.Enabled;
        }
    }

    private void StartDesktopMonitor()
    {
        _desktopDetectTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _desktopDetectTimer.Tick += (s, e) => CheckDesktopVisibility();
        _desktopDetectTimer.Start();
    }

    private void CheckDesktopVisibility()
    {
        if (!_config.Widget.OnlyShowOnDesktop)
        {
            if (Visibility != Visibility.Visible) Visibility = Visibility.Visible;
            if (_telemetryWindow != null && _config.TelemetryWidget.Enabled && _telemetryWindow.Visibility != Visibility.Visible)
                _telemetryWindow.Visibility = Visibility.Visible;
            if (_financeWindow != null && _config.FinanceWidget.Enabled && _financeWindow.Visibility != Visibility.Visible)
                _financeWindow.Visibility = Visibility.Visible;
            return;
        }

        var fg = GetForegroundWindow();
        var myHandle = new WindowInteropHelper(this).Handle;
        var telHandle = _telemetryWindow != null ? new WindowInteropHelper(_telemetryWindow).Handle : IntPtr.Zero;
        var finHandle = _financeWindow != null ? new WindowInteropHelper(_financeWindow).Handle : IntPtr.Zero;

        bool isDesktop = false;

        if (fg == IntPtr.Zero || fg == myHandle || fg == telHandle || fg == finHandle ||
            IsMouseOver ||
            (_telemetryWindow != null && _telemetryWindow.IsMouseOver) ||
            (_financeWindow != null && _financeWindow.IsMouseOver) ||
            (ContextMenu != null && ContextMenu.IsOpen))
        {
            isDesktop = true;
        }
        else
        {
            var sb = new StringBuilder(256);
            GetClassName(fg, sb, 256);
            var cls = sb.ToString();

            // Detect Windows desktop shell surfaces (wallpaper, taskbar, desktop view)
            if (cls == "Progman" ||
                cls == "WorkerW" ||
                cls == "Shell_TrayWnd" ||
                cls == "Shell_SecondaryTrayWnd" ||
                cls == "SysListView32" ||
                cls == "DesktopWindowXamlSource")
            {
                isDesktop = true;
            }
            else if (cls == "#32768" ||
                     cls == "Xaml_WindowedPopupClass" ||
                     cls == "TopLevelWindowForOverflowXamlIsland" ||
                     cls == "Popup" ||
                     cls.StartsWith("HwndWrapper[RigBadge"))
            {
                // When a context menu or popup is active, maintain visibility if we were already on desktop
                if (Visibility == Visibility.Visible)
                {
                    isDesktop = true;
                }
            }
        }

        if (isDesktop)
        {
            if (Visibility != Visibility.Visible)
            {
                Visibility = Visibility.Visible;
            }
            if (_telemetryWindow != null && _config.TelemetryWidget.Enabled && _telemetryWindow.Visibility != Visibility.Visible)
            {
                _telemetryWindow.Visibility = Visibility.Visible;
            }
            if (_financeWindow != null && _config.FinanceWidget.Enabled && _financeWindow.Visibility != Visibility.Visible)
            {
                _financeWindow.Visibility = Visibility.Visible;
            }
        }
        else
        {
            if (Visibility != Visibility.Collapsed)
            {
                Visibility = Visibility.Collapsed;
            }
            if (_telemetryWindow != null && _telemetryWindow.Visibility != Visibility.Collapsed)
            {
                _telemetryWindow.Visibility = Visibility.Collapsed;
            }
            if (_financeWindow != null && _financeWindow.Visibility != Visibility.Collapsed)
            {
                _financeWindow.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void PositionWidget()
    {
        var workArea = SystemParameters.WorkArea;
        var targetLeft = workArea.Right - Width - _config.Widget.OffsetX;
        var targetTop = workArea.Top + _config.Widget.OffsetY;

        Left = Math.Max(workArea.Left, targetLeft);
        Top = Math.Max(workArea.Top, targetTop);
    }

    private async Task RefreshHardwareSpecsAsync()
    {
        try
        {
            _priceService ??= new PriceService(_pricesPath);
            _priceService.CheckAndApplyDailyUpdate();

            // Fetch live Izoly prices in background
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

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();

                var workArea = SystemParameters.WorkArea;
                _config.Widget.OffsetX = Math.Max(0, workArea.Right - Left - Width);
                _config.Widget.OffsetY = Math.Max(0, Top - workArea.Top);
                _config.Save(_configPath);
            }
            catch { }
        }
    }

    private void MenuTelemetry_Click(object sender, RoutedEventArgs e)
    {
        _config.TelemetryWidget.Enabled = !_config.TelemetryWidget.Enabled;
        _config.Save(_configPath);
        if (MenuTelemetry != null) MenuTelemetry.IsChecked = _config.TelemetryWidget.Enabled;

        if (_config.TelemetryWidget.Enabled)
        {
            _telemetryWindow ??= new TelemetryWindow(_config, _configPath);
            _telemetryWindow.Show();
        }
        else
        {
            _telemetryWindow?.Close();
            _telemetryWindow = null;
        }
    }

    private void MenuFinance_Click(object sender, RoutedEventArgs e)
    {
        _config.FinanceWidget.Enabled = !_config.FinanceWidget.Enabled;
        _config.Save(_configPath);
        if (MenuFinance != null) MenuFinance.IsChecked = _config.FinanceWidget.Enabled;

        if (_config.FinanceWidget.Enabled)
        {
            _financeWindow ??= new FinanceWindow(_config, _configPath);
            _financeWindow.Show();
        }
        else
        {
            _financeWindow?.Close();
            _financeWindow = null;
        }
    }

    private void MenuOnlyDesktop_Click(object sender, RoutedEventArgs e)
    {
        _config.Widget.OnlyShowOnDesktop = !_config.Widget.OnlyShowOnDesktop;
        if (MenuOnlyDesktop != null)
        {
            MenuOnlyDesktop.IsChecked = _config.Widget.OnlyShowOnDesktop;
        }
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
        _telemetryWindow?.Close();
        _financeWindow?.Close();
        Close();
    }
}
