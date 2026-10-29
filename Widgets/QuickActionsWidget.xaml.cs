using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class QuickActionsWidget : UserControl
{
    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    public event EventHandler? RamCleaned;
    private DateTime _lastPopupClosedTime = DateTime.MinValue;
    private DateTime _lastNotesPopupClosedTime = DateTime.MinValue;
    private readonly DispatcherTimer _saveNotesTimer;
    private bool _isLoadingNotes = false;

    public QuickActionsWidget()
    {
        InitializeComponent();
        LocalizationService.LanguageChanged += OnLanguageChanged;

        _saveNotesTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _saveNotesTimer.Tick += OnSaveNotesTimerTick;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTexts();
        _ = LoadNotesAsync();
    }

    private async Task LoadNotesAsync()
    {
        try
        {
            _isLoadingNotes = true;
            var text = await ScratchpadService.LoadAsync();
            TxtScratchpad.Text = text;
            TxtNotesPlaceholder.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
            TxtCharCount.Text = $"{text.Length} karakter";
        }
        catch { }
        finally
        {
            _isLoadingNotes = false;
        }
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

        TxtNotes.Text = LocalizationService.Get("Notes");
        BtnNotes.ToolTip = LocalizationService.Get("NotesTooltip");

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
        if (PopupVolumeMixer == null) return;

        if ((DateTime.UtcNow - _lastPopupClosedTime).TotalMilliseconds < 250)
        {
            return;
        }

        if (PopupVolumeMixer.IsOpen)
        {
            PopupVolumeMixer.IsOpen = false;
        }
        else
        {
            if (PopupScratchpad != null && PopupScratchpad.IsOpen) PopupScratchpad.IsOpen = false;
            RefreshAudioMixer();
            PopupVolumeMixer.IsOpen = true;
        }
    }

    private void PopupVolumeMixer_Closed(object sender, EventArgs e)
    {
        _lastPopupClosedTime = DateTime.UtcNow;
    }

    private void RefreshAudioMixer()
    {
        try
        {
            var (master, sessions) = AudioMixerService.GetAudioState();
            MasterDeviceCard.DataContext = master;
            ListAppSessions.ItemsSource = sessions;
            TxtNoApps.Visibility = sessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch { }
    }

    private void BtnRefreshMixer_Click(object sender, RoutedEventArgs e)
    {
        RefreshAudioMixer();
    }

    private void BtnMasterMute_Click(object sender, RoutedEventArgs e)
    {
        if (MasterDeviceCard.DataContext is MasterVolumeModel master)
        {
            master.ToggleMute();
        }
    }

    private void BtnAppMute_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is AudioSessionModel session)
        {
            session.ToggleMute();
        }
    }

    private void BtnClassicMixer_Click(object sender, RoutedEventArgs e)
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

    #region Scratchpad Notes

    private void BtnNotes_Click(object sender, RoutedEventArgs e)
    {
        if (PopupScratchpad == null) return;

        if ((DateTime.UtcNow - _lastNotesPopupClosedTime).TotalMilliseconds < 250)
        {
            return;
        }

        if (PopupScratchpad.IsOpen)
        {
            PopupScratchpad.IsOpen = false;
        }
        else
        {
            if (PopupVolumeMixer != null && PopupVolumeMixer.IsOpen) PopupVolumeMixer.IsOpen = false;
            PopupScratchpad.IsOpen = true;
            Dispatcher.BeginInvoke(() => TxtScratchpad.Focus(), DispatcherPriority.Input);
        }
    }

    private void PopupScratchpad_Closed(object sender, EventArgs e)
    {
        _lastNotesPopupClosedTime = DateTime.UtcNow;
        _ = ScratchpadService.SaveAsync(TxtScratchpad.Text);
    }

    private void TxtScratchpad_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TxtNotesPlaceholder != null)
        {
            TxtNotesPlaceholder.Visibility = string.IsNullOrEmpty(TxtScratchpad.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        if (TxtCharCount != null)
        {
            TxtCharCount.Text = $"{TxtScratchpad.Text.Length} karakter";
        }

        if (!_isLoadingNotes)
        {
            _saveNotesTimer.Stop();
            _saveNotesTimer.Start();
            if (TxtSaveStatus != null)
            {
                TxtSaveStatus.Text = "Kaydediliyor...";
            }
        }
    }

    private async void OnSaveNotesTimerTick(object? sender, EventArgs e)
    {
        _saveNotesTimer.Stop();
        await ScratchpadService.SaveAsync(TxtScratchpad.Text);
        if (TxtSaveStatus != null)
        {
            TxtSaveStatus.Text = "Otomatik kaydedildi";
        }
    }

    private void BtnClearNotes_Click(object sender, RoutedEventArgs e)
    {
        TxtScratchpad.Text = string.Empty;
        _ = ScratchpadService.SaveAsync(string.Empty);
    }

    #endregion

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
