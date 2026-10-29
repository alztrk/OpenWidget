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
    #region Win32 Desktop Embedding APIs
    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const int WM_WINDOWPOSCHANGING = 0x0046;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    #endregion

    private HardwareConfig _config = new();
    private HardwareSpecs _specs = new();
    private string _configPath = string.Empty;

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
        ResolveConfigPath();
        LoadConfig();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;

        // Apply toolwindow and noactivate styles
        var exStyle = (long)GetWindowLongPtr(handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLongPtr(handle, GWL_EXSTYLE, (IntPtr)exStyle);

        // Pin to bottom (desktop wallpaper level)
        SetToBottom(handle);

        // Hook window message loop to keep it at HWND_BOTTOM
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
    }

    private void SetToBottom(IntPtr handle)
    {
        SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_WINDOWPOSCHANGING)
        {
            var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
            pos.hwndInsertAfter = HWND_BOTTOM;
            pos.flags |= SWP_NOACTIVATE;
            Marshal.StructureToPtr(pos, lParam, true);
        }
        return IntPtr.Zero;
    }

    private void ResolveConfigPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var directPath = Path.Combine(baseDir, "hardware-config.json");
        if (File.Exists(directPath))
        {
            _configPath = directPath;
            return;
        }

        var cwdPath = Path.Combine(Environment.CurrentDirectory, "hardware-config.json");
        if (File.Exists(cwdPath))
        {
            _configPath = cwdPath;
            return;
        }

        _configPath = directPath;
    }

    private void LoadConfig()
    {
        _config = HardwareConfig.Load(_configPath);

        if (_config.Widget.Width >= 200) Width = _config.Widget.Width;
        if (_config.Widget.Opacity > 0.1 && _config.Widget.Opacity <= 1.0) Opacity = _config.Widget.Opacity;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWidget();
        await RefreshHardwareSpecsAsync();
    }

    private void PositionWidget()
    {
        var workArea = SystemParameters.WorkArea;
        var targetLeft = workArea.Right - Width - _config.Widget.OffsetX;
        var targetTop = workArea.Top + _config.Widget.OffsetY;

        Left = Math.Max(workArea.Left, targetLeft);
        Top = Math.Max(workArea.Top, targetTop);

        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            SetToBottom(handle);
        }
    }

    private async Task RefreshHardwareSpecsAsync()
    {
        try
        {
            var scanner = new HardwareScanner(_config);
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
                var handle = new WindowInteropHelper(this).Handle;
                if (handle != IntPtr.Zero)
                {
                    SetToBottom(handle);
                }
            }
            catch { }
        }
    }

    private async void MenuRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadConfig();
        await RefreshHardwareSpecsAsync();
    }

    private void MenuEditConfig_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(_configPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _configPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show($"Yapılandırma dosyası bulunamadı:\n{_configPath}", "RigBadge", MessageBoxButton.OK, MessageBoxImage.Information);
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
