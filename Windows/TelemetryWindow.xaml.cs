using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using RigBadge.Models;
using RigBadge.Services;

namespace RigBadge.Windows;

public partial class TelemetryWindow : Window, INotifyPropertyChanged
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
    private readonly SystemTelemetryService _service = new();
    private TelemetrySpecs _specs = new();
    private DispatcherTimer? _timer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TelemetrySpecs Specs
    {
        get => _specs;
        set
        {
            _specs = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Specs)));
        }
    }

    public TelemetryWindow(HardwareConfig config, string configPath)
    {
        InitializeComponent();
        _config = config;
        _configPath = configPath;
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

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWidget();

        // Initial scan
        RefreshData();

        // 2-second timer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += (s, args) => RefreshData();
        _timer.Start();
    }

    public void RefreshData()
    {
        Specs = _service.GetTelemetrySpecs();
    }

    private void PositionWidget()
    {
        var workArea = SystemParameters.WorkArea;
        var targetLeft = workArea.Right - Width - _config.TelemetryWidget.OffsetX;
        var targetTop = workArea.Top + _config.TelemetryWidget.OffsetY;

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
                _config.TelemetryWidget.OffsetX = Math.Max(0, workArea.Right - Left - Width);
                _config.TelemetryWidget.OffsetY = Math.Max(0, Top - workArea.Top);
                _config.Save(_configPath);
            }
            catch { }
        }
    }

    private void MenuRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshData();
    }

    private void MenuResetPos_Click(object sender, RoutedEventArgs e)
    {
        _config.TelemetryWidget.OffsetX = 415;
        _config.TelemetryWidget.OffsetY = 20;
        _config.Save(_configPath);
        PositionWidget();
    }

    private void MenuClose_Click(object sender, RoutedEventArgs e)
    {
        _config.TelemetryWidget.Enabled = false;
        _config.Save(_configPath);
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer?.Stop();
        _service.Dispose();
        base.OnClosed(e);
    }
}
