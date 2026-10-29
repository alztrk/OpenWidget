using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using RigBadge.Models;
using RigBadge.Services;

namespace RigBadge;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const int GWL_EXSTYLE = -20;
    private const int GWL_HWNDPARENT = -8;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic backdrop

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private HardwareConfig _config = new();
    private HardwareSpecs _specs = new();
    private string _configPath = string.Empty;
    private string _pricesPath = string.Empty;
    private PriceService? _priceService;

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

        // Apply Windows 11 Acrylic Glassmorphism effect via DWM
        try
        {
            int backdrop = DWMSBT_TRANSIENTWINDOW;
            DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
        }
        catch { }

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

        await RefreshHardwareSpecsAsync();
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

            var scanner = new HardwareScanner(_config, _priceService);
            var result = await Task.Run(() => scanner.Scan());
            Specs = result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hardware scan error: {ex.Message}");
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch { }
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

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
