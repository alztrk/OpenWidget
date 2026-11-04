using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using OpenWidget.Models;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class QuickActionsWidget : UserControl
{
    private const int WmActivateApp = 0x001C;
    private readonly DispatcherTimer _saveNotesTimer;
    private bool _isLoadingNotes = false;
    private int _notesLoadGeneration;
    private Window? _hostWindow;
    private HwndSource? _hwndSource;
    private MasterVolumeModel? _masterVolumeModel;
    private List<AudioSessionModel> _audioSessionModels = new();

    public QuickActionsWidget()
    {
        InitializeComponent();
        Loaded += (s, e) => LocalizationService.LanguageChanged += OnLanguageChanged;

        _saveNotesTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _saveNotesTimer.Tick += OnSaveNotesTimerTick;

        Unloaded += (s, e) =>
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            _notesLoadGeneration++;
            bool hasPendingSave = _saveNotesTimer.IsEnabled;
            _saveNotesTimer.Stop();
            DetachOutsideClickHandlers();
            PopupVolumeMixer.IsOpen = false;
            PopupScratchpad.IsOpen = false;
            if (hasPendingSave)
            {
                _ = ScratchpadService.SaveAsync(TxtScratchpad.Text);
            }
            DisposeAudioMixerState();
        };
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTexts();
        AttachOutsideClickHandlers();
        _ = LoadNotesAsync();
    }

    private async Task LoadNotesAsync()
    {
        int generation = ++_notesLoadGeneration;
        var slot = Tag as WidgetSlot;
        try
        {
            _isLoadingNotes = true;
            var cachedText = slot?.ScratchpadText;
            var diskText = cachedText ?? await ScratchpadService.LoadAsync();
            bool editedWhileLoading = cachedText == null && slot?.ScratchpadText != null;
            var text = slot?.ScratchpadText ?? diskText;
            if (slot?.ScratchpadText is { } changedText)
            {
                text = changedText;
            }
            if (!IsLoaded || generation != _notesLoadGeneration)
            {
                return;
            }

            if (slot != null)
            {
                slot.ScratchpadText = text;
            }
            TxtScratchpad.Text = text;
            TxtNotesPlaceholder.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
            TxtCharCount.Text = $"{text.Length} karakter";
            if (editedWhileLoading)
            {
                _saveNotesTimer.Stop();
                _saveNotesTimer.Start();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Scratchpad load error: {ex.Message}");
        }
        finally
        {
            if (generation == _notesLoadGeneration)
            {
                _isLoadingNotes = false;
            }
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

    private void BtnVolume_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (PopupVolumeMixer?.IsOpen == true)
        {
            PopupVolumeMixer.IsOpen = false;
            e.Handled = true;
        }
    }

    private void BtnVolume_Click(object sender, RoutedEventArgs e)
    {
        if (PopupVolumeMixer == null || PopupVolumeMixer.IsOpen)
        {
            return;
        }

        ClosePopups(keep: PopupVolumeMixer);
        RefreshAudioMixer();
        PopupVolumeMixer.IsOpen = true;
    }

    private void RefreshAudioMixer()
    {
        DisposeAudioMixerState();

        try
        {
            var (master, sessions) = AudioMixerService.GetAudioState();
            _masterVolumeModel = master;
            _audioSessionModels = sessions;
            MasterDeviceCard.DataContext = master;
            ListAppSessions.ItemsSource = sessions;
            TxtNoApps.Visibility = sessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch { }
    }

    private void PopupVolumeMixer_Closed(object? sender, EventArgs e)
    {
        DisposeAudioMixerState();
    }

    private void DisposeAudioMixerState()
    {
        MasterDeviceCard.DataContext = null;
        ListAppSessions.ItemsSource = null;

        _masterVolumeModel?.Dispose();
        _masterVolumeModel = null;

        foreach (var session in _audioSessionModels)
        {
            session.Dispose();
        }

        _audioSessionModels.Clear();
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

    #region Scratchpad Notes

    private void BtnNotes_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (PopupScratchpad?.IsOpen == true)
        {
            PopupScratchpad.IsOpen = false;
            e.Handled = true;
        }
    }

    private void BtnNotes_Click(object sender, RoutedEventArgs e)
    {
        if (PopupScratchpad == null || PopupScratchpad.IsOpen)
        {
            return;
        }

        ClosePopups(keep: PopupScratchpad);
        PopupScratchpad.IsOpen = true;
        Dispatcher.BeginInvoke(() => TxtScratchpad.Focus(), DispatcherPriority.Input);
    }

    private void PopupScratchpad_Closed(object sender, EventArgs e)
    {
        _ = ScratchpadService.SaveAsync(TxtScratchpad.Text);
    }

    private void TxtScratchpad_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Tag is WidgetSlot slot)
        {
            slot.ScratchpadText = TxtScratchpad.Text;
        }

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

    private void AttachOutsideClickHandlers()
    {
        DetachOutsideClickHandlers();
        _hostWindow = Window.GetWindow(this);
        if (_hostWindow == null)
        {
            return;
        }

        _hostWindow.PreviewMouseDown += HostWindow_PreviewMouseDown;
        _hwndSource = PresentationSource.FromVisual(_hostWindow) as HwndSource;
        _hwndSource?.AddHook(HostWindowHook);
    }

    private void DetachOutsideClickHandlers()
    {
        if (_hostWindow != null)
        {
            _hostWindow.PreviewMouseDown -= HostWindow_PreviewMouseDown;
        }

        _hwndSource?.RemoveHook(HostWindowHook);
        _hwndSource = null;
        _hostWindow = null;
    }

    private void HostWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (PopupVolumeMixer?.IsOpen == true && !IsClickOnPopupOrButton(PopupVolumeMixer, BtnVolume, e))
        {
            PopupVolumeMixer.IsOpen = false;
        }

        if (PopupScratchpad?.IsOpen == true && !IsClickOnPopupOrButton(PopupScratchpad, BtnNotes, e))
        {
            PopupScratchpad.IsOpen = false;
        }
    }

    private IntPtr HostWindowHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmActivateApp && wParam == IntPtr.Zero)
        {
            Dispatcher.BeginInvoke(() => ClosePopups());
        }

        return IntPtr.Zero;
    }

    private void ClosePopups(Popup? keep = null)
    {
        if (keep != PopupVolumeMixer && PopupVolumeMixer?.IsOpen == true)
        {
            PopupVolumeMixer.IsOpen = false;
        }

        if (keep != PopupScratchpad && PopupScratchpad?.IsOpen == true)
        {
            PopupScratchpad.IsOpen = false;
        }
    }

    private static bool IsClickOnPopupOrButton(Popup popup, UIElement button, MouseButtonEventArgs e)
    {
        if (button.IsMouseOver)
        {
            return true;
        }

        if (popup.Child is UIElement child && child.IsMouseOver)
        {
            return true;
        }

        if (e.OriginalSource is DependencyObject source &&
            (IsDescendant(button, source) || (popup.Child is DependencyObject popupRoot && IsDescendant(popupRoot, source))))
        {
            return true;
        }

        return false;
    }

    private static bool IsDescendant(DependencyObject root, DependencyObject? node)
    {
        while (node != null)
        {
            if (ReferenceEquals(node, root))
            {
                return true;
            }

            node = VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node);
        }

        return false;
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
}
