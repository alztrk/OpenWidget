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
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

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

        // Apply ToolWindow (hide from Alt+Tab) and NoActivate (no focus stealing)
        var exStyle = (long)GetWindowLongPtr(handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLongPtr(handle, GWL_EXSTYLE, (IntPtr)exStyle);
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
        Topmost = _config.Widget.AlwaysOnTop;
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
