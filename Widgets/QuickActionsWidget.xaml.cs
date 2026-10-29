using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class QuickActionsWidget : UserControl
{
    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    public event EventHandler? RamCleaned;

    public QuickActionsWidget()
    {
        InitializeComponent();
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTexts();
    }

    private void OnLanguageChanged()
    {
        Dispatcher.Invoke(UpdateTexts);
    }

    private void UpdateTexts()
    {
        TxtTaskMgr.Text = LocalizationService.Get("TaskMgr");
        BtnTaskMgr.ToolTip = LocalizationService.Get("TaskMgrTooltip");

        TxtVolume.Text = LocalizationService.Get("Volume");
        BtnVolume.ToolTip = LocalizationService.Get("VolumeTooltip");

        TxtTerminal.Text = LocalizationService.Get("Terminal");
        BtnTerminal.ToolTip = LocalizationService.Get("TerminalTooltip");

        TxtCleanRam.Text = LocalizationService.Get("CleanRam");
        BtnCleanRam.ToolTip = LocalizationService.Get("CleanRamTooltip");
    }

    private void BtnTaskMgr_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "taskmgr.exe",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void BtnVolume_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "sndvol.exe",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void BtnTerminal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    UseShellExecute = true
                });
            }
            catch
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }

    private async void BtnCleanRam_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);

            RamCleaned?.Invoke(this, EventArgs.Empty);

            if (TxtCleanRam != null)
            {
                TxtCleanRam.Text = LocalizationService.Get("Cleaned");
                TxtCleanRam.Foreground = Application.Current.Resources["AccentGreenBrush"] as Brush ?? Brushes.LightGreen;

                await Task.Delay(1500);

                TxtCleanRam.Text = LocalizationService.Get("CleanRam");
                TxtCleanRam.ClearValue(TextBlock.ForegroundProperty);
            }
        }
        catch { }
    }
}