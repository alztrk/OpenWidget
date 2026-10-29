using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using RigBadge.Models;
using RigBadge.Services;

namespace RigBadge.Windows;

public partial class FinanceWindow : Window, INotifyPropertyChanged
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

    private readonly HardwareConfig _config;
    private readonly string _configPath;
    private readonly MarketFinanceService _service;
    private FinanceSpecs _specs = new();
    private DispatcherTimer? _timer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public FinanceSpecs Specs
    {
        get => _specs;
        set
        {
            _specs = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Specs)));
        }
    }

    public FinanceWindow(HardwareConfig config, string configPath)
    {
        InitializeComponent();
        _config = config;
        _configPath = configPath;

        var baseDir = AppContext.BaseDirectory;
        var cachePath = Path.Combine(baseDir, "finance-cache.json");
        _service = new MarketFinanceService(cachePath);
        _specs = _service.GetCurrentSpecs();

        DataContext = this;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;

        var exStyle = (long)GetWindowLongPtr(handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLongPtr(handle, GWL_EXSTYLE, (IntPtr)exStyle);

        var progman = FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            SetWindowLongPtr(handle, GWL_HWNDPARENT, progman);
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWidget();

        // Fetch live data immediately
        await RefreshDataAsync();

        // 60-second periodic update
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(60)
        };
        _timer.Tick += async (s, args) => await RefreshDataAsync();
        _timer.Start();
    }

    public async Task RefreshDataAsync()
    {
        Specs = await _service.FetchLatestAsync();
    }

    private void PositionWidget()
    {
        var workArea = SystemParameters.WorkArea;
        var targetLeft = workArea.Right - Width - _config.FinanceWidget.OffsetX;
        var targetTop = workArea.Top + _config.FinanceWidget.OffsetY;

        Left = Math.Max(workArea.Left, targetLeft);
        Top = Math.Max(workArea.Top, targetTop);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();

                // Save new coordinates relative to top-right
                var workArea = SystemParameters.WorkArea;
                _config.FinanceWidget.OffsetX = Math.Max(0, workArea.Right - Left - Width);
                _config.FinanceWidget.OffsetY = Math.Max(0, Top - workArea.Top);
                _config.Save(_configPath);
            }
            catch { }
        }
    }

    private async void MenuRefresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDataAsync();
    }

    private void MenuResetPos_Click(object sender, RoutedEventArgs e)
    {
        _config.FinanceWidget.OffsetX = 415;
        _config.FinanceWidget.OffsetY = 275;
        _config.Save(_configPath);
        PositionWidget();
    }

    private void MenuClose_Click(object sender, RoutedEventArgs e)
    {
        _config.FinanceWidget.Enabled = false;
        _config.Save(_configPath);
        Close();
    }
}
