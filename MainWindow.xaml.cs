using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RigBadge.Models;
using RigBadge.Services;

namespace RigBadge;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private HardwareConfig _config = new();
    private HardwareSpecs _specs = new();
    private bool _isLocked = false;
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

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            _isLocked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLocked)));
            UpdatePinIcon();
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        ResolveConfigPath();
        LoadConfig();
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
        _isLocked = _config.Widget.IsLocked;

        if (_config.Widget.Width > 200) Width = _config.Widget.Width;
        if (_config.Widget.Height > 200) Height = _config.Widget.Height;
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

        Topmost = _config.Widget.AlwaysOnTop;
        UpdatePinIcon();
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
        if (!_isLocked && e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch
            {
            }
        }
    }

    private void UpdatePinIcon()
    {
        if (PinIconPath != null)
        {
            PinIconPath.Fill = _isLocked
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("TextSecondaryBrush");
        }
    }

    private void MenuTogglePin_Click(object sender, RoutedEventArgs e)
    {
        IsLocked = !IsLocked;
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
