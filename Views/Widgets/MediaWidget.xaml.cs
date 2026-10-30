using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class MediaWidget : UserControl
{
    private MediaSessionService? _service;
    private TimeSpan _currentDuration = TimeSpan.Zero;
    private bool _isUserSeeking = false;
    private bool _isUpdatingVolumeInternal = false;
    private bool _isUserAdjustingVolume = false;
    private DateTime _lastUserVolumeAdjustTime = DateTime.MinValue;
    private readonly DispatcherTimer _volumeSyncTimer;

    public MediaWidget()
    {
        _isUpdatingVolumeInternal = true;
        InitializeComponent();
        _isUpdatingVolumeInternal = false;

        _volumeSyncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _volumeSyncTimer.Tick += (s, e) => UpdateMediaVolumeUI();
        _volumeSyncTimer.Start();
    }

    public void AttachService(MediaSessionService service)
    {
        _service = service;
        _service.MediaChanged += OnMediaChanged;
        _service.PositionChanged += OnPositionChanged;
        _service.SessionsUpdated += OnSessionsUpdated;
        UpdateTrackInfo(_service.CurrentTrack);
        OnSessionsUpdated();
    }

    private void OnSessionsUpdated()
    {
        Dispatcher.Invoke(() =>
        {
            if (_service != null && _service.SessionCount > 1)
            {
                PanelSessionSwitcher.Visibility = Visibility.Visible;
                string source = _service.CurrentTrack.SourceApp;
                if (string.IsNullOrEmpty(source)) source = "Medya";
                TxtSessionSource.Text = $"{source} ({_service.CurrentSessionIndex}/{_service.SessionCount})";
            }
            else
            {
                PanelSessionSwitcher.Visibility = Visibility.Collapsed;
            }
        });
    }

    private void BtnPrevSession_Click(object sender, RoutedEventArgs e)
    {
        _service?.SwitchToPreviousSession();
    }

    private void BtnNextSession_Click(object sender, RoutedEventArgs e)
    {
        _service?.SwitchToNextSession();
    }

    private void OnMediaChanged(MediaTrackInfo info)
    {
        Dispatcher.Invoke(() => UpdateTrackInfo(info));
    }

    private void OnPositionChanged(TimeSpan pos, TimeSpan dur)
    {
        Dispatcher.Invoke(() =>
        {
            if (_isUserSeeking) return;

            _currentDuration = dur;
            TxtCurrentTime.Text = FormatTime(pos);

            if (dur > TimeSpan.Zero)
            {
                SliderSeek.IsEnabled = true;
                SliderSeek.IsHitTestVisible = true;
                SliderSeek.Maximum = dur.TotalSeconds;
                SliderSeek.Value = Math.Clamp(pos.TotalSeconds, 0, dur.TotalSeconds);
                TxtTotalTime.Text = FormatTime(dur);
            }
            else
            {
                // Web stream / video without fixed timeline metadata or resolving
                SliderSeek.IsEnabled = true;
                SliderSeek.IsHitTestVisible = false;
                double scale = Math.Max(300, pos.TotalSeconds * 1.25);
                SliderSeek.Maximum = scale;
                SliderSeek.Value = Math.Clamp(pos.TotalSeconds, 0, scale);
                TxtTotalTime.Text = "--:--";
            }
        });
    }

    private void UpdateTrackInfo(MediaTrackInfo info)
    {
        if (!info.HasMedia)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        Visibility = Visibility.Visible;
        TxtTitle.Text = info.Title;
        TxtArtist.Text = info.Artist;

        // Play/Pause icon
        var geoKey = info.IsPlaying ? "GeoPause" : "GeoPlay";
        if (Application.Current.Resources.Contains(geoKey))
        {
            PathPlayPause.Data = Application.Current.Resources[geoKey] as Geometry;
        }

        // Thumbnail
        if (info.ThumbnailBytes != null && info.ThumbnailBytes.Length > 0)
        {
            try
            {
                var bmp = new BitmapImage();
                using var ms = new MemoryStream(info.ThumbnailBytes);
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();

                ImgThumbnail.Source = bmp;
                ImgThumbnail.Visibility = Visibility.Visible;
                PathFallbackMusic.Visibility = Visibility.Collapsed;
            }
            catch
            {
                ImgThumbnail.Source = null;
                ImgThumbnail.Visibility = Visibility.Collapsed;
                PathFallbackMusic.Visibility = Visibility.Visible;
            }
        }
        else
        {
            ImgThumbnail.Source = null;
            ImgThumbnail.Visibility = Visibility.Collapsed;
            PathFallbackMusic.Visibility = Visibility.Visible;
        }

        // Seek Bar & Timeline
        _currentDuration = info.Duration;
        TxtCurrentTime.Text = FormatTime(info.Position);
        if (info.Duration > TimeSpan.Zero)
        {
            SliderSeek.IsEnabled = true;
            SliderSeek.IsHitTestVisible = true;
            SliderSeek.Maximum = info.Duration.TotalSeconds;
            SliderSeek.Value = Math.Clamp(info.Position.TotalSeconds, 0, info.Duration.TotalSeconds);
            TxtTotalTime.Text = FormatTime(info.Duration);
        }
        else
        {
            SliderSeek.IsEnabled = true;
            SliderSeek.IsHitTestVisible = false;
            double scale = Math.Max(300, info.Position.TotalSeconds * 1.25);
            SliderSeek.Maximum = scale;
            SliderSeek.Value = Math.Clamp(info.Position.TotalSeconds, 0, scale);
            TxtTotalTime.Text = "--:--";
        }

        // Initialize Real Master Volume Control
        UpdateMediaVolumeUI();
        OnSessionsUpdated();
    }

    private void UpdateMediaVolumeUI()
    {
        if (_isUserAdjustingVolume || (DateTime.UtcNow - _lastUserVolumeAdjustTime < TimeSpan.FromMilliseconds(1500))) return;

        try
        {
            string sourceApp = _service?.CurrentTrack.SourceApp ?? "";
            var (vol, isMuted) = AudioMixerService.GetMediaVolume(sourceApp);

            _isUpdatingVolumeInternal = true;
            try
            {
                SliderMediaVolume.Value = vol;
                if (TxtMediaVolumePercent != null)
                {
                    TxtMediaVolumePercent.Text = $"{(int)Math.Round(vol)}%";
                }
            }
            finally
            {
                _isUpdatingVolumeInternal = false;
            }

            UpdateMuteIcon(isMuted);
        }
        catch { }
    }

    private void UpdateMuteIcon(bool isMuted)
    {
        var iconKey = isMuted ? "GeoSpeakerMute" : "GeoSpeaker";
        if (Application.Current.Resources.Contains(iconKey))
        {
            PathMediaVolume.Data = Application.Current.Resources[iconKey] as Geometry;
        }
        PathMediaVolume.Fill = isMuted
            ? (Application.Current.Resources["AccentRedBrush"] as Brush ?? Brushes.Red)
            : (Application.Current.Resources["TextSecondaryBrush"] as Brush ?? Brushes.Gray);
    }

    private static string FormatTime(TimeSpan t)
    {
        if (t.TotalHours >= 1)
        {
            return $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}";
        }
        return $"{t.Minutes:D2}:{t.Seconds:D2}";
    }

    private void SliderSeek_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isUserSeeking = true;
    }

    private void SliderSeek_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isUserSeeking = false;
        CommitSeek();
    }

    private void SliderSeek_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUserSeeking && _currentDuration > TimeSpan.Zero)
        {
            var previewPos = TimeSpan.FromSeconds(SliderSeek.Value);
            TxtCurrentTime.Text = FormatTime(previewPos);
        }
    }

    private void CommitSeek()
    {
        if (_service != null && _currentDuration > TimeSpan.Zero)
        {
            var target = TimeSpan.FromSeconds(SliderSeek.Value);
            _ = _service.SeekToAsync(target);
        }
    }

    private void SliderMediaVolume_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isUserAdjustingVolume = true;
    }

    private void SliderMediaVolume_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isUserAdjustingVolume = false;
    }

    private void SliderMediaVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingVolumeInternal) return;
        if (!IsLoaded) return;

        // Ensure volume commands are only dispatched when user is actively interacting with the slider
        if (!_isUserAdjustingVolume && !SliderMediaVolume.IsMouseOver && !SliderMediaVolume.IsKeyboardFocusWithin)
        {
            return;
        }

        _lastUserVolumeAdjustTime = DateTime.UtcNow;
        float newVol = (float)SliderMediaVolume.Value;
        if (TxtMediaVolumePercent != null)
        {
            TxtMediaVolumePercent.Text = $"{(int)Math.Round(newVol)}%";
        }

        string sourceApp = _service?.CurrentTrack.SourceApp ?? "";
        AudioMixerService.SetMediaVolume(sourceApp, newVol);
    }

    private void BtnMediaMute_Click(object sender, RoutedEventArgs e)
    {
        string sourceApp = _service?.CurrentTrack.SourceApp ?? "";
        bool isMuted = AudioMixerService.ToggleMediaMute(sourceApp);
        UpdateMuteIcon(isMuted);
    }

    private async void BtnPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_service != null) await _service.SkipPreviousAsync();
    }

    private async void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_service != null) await _service.TogglePlayPauseAsync();
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        if (_service != null) await _service.SkipNextAsync();
    }
}
