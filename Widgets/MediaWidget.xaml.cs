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
    private readonly DispatcherTimer _volumeSyncTimer;

    public MediaWidget()
    {
        InitializeComponent();

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
        UpdateTrackInfo(_service.CurrentTrack);
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
                double pct = Math.Clamp((pos.TotalSeconds / dur.TotalSeconds) * 100.0, 0.0, 100.0);
                SliderSeek.Value = pct;
                TxtTotalTime.Text = FormatTime(dur);
            }
            else
            {
                // Web stream / YouTube / video without fixed timeline metadata
                SliderSeek.IsEnabled = true;
                double modSec = pos.TotalSeconds % 60.0;
                SliderSeek.Value = (modSec / 60.0) * 100.0;
                TxtTotalTime.Text = "Akış";
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
            double pct = Math.Clamp((info.Position.TotalSeconds / info.Duration.TotalSeconds) * 100.0, 0.0, 100.0);
            SliderSeek.Value = pct;
            TxtTotalTime.Text = FormatTime(info.Duration);
        }
        else
        {
            SliderSeek.IsEnabled = true;
            double modSec = info.Position.TotalSeconds % 60.0;
            SliderSeek.Value = (modSec / 60.0) * 100.0;
            TxtTotalTime.Text = "Akış";
        }

        // Initialize Real Master Volume Control
        UpdateMediaVolumeUI();
    }

    private void UpdateMediaVolumeUI()
    {
        if (_isUserAdjustingVolume) return;

        try
        {
            var (vol, isMuted) = AudioMixerService.GetMasterVolume();

            _isUpdatingVolumeInternal = true;
            SliderMediaVolume.Value = vol;
            if (TxtMediaVolumePercent != null)
            {
                TxtMediaVolumePercent.Text = $"{(int)Math.Round(vol)}%";
            }
            _isUpdatingVolumeInternal = false;

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
        return t.TotalHours >= 1 ? t.ToString(@"hh\:mm\:ss") : t.ToString(@"mm\:ss");
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
            var previewPos = TimeSpan.FromSeconds((SliderSeek.Value / 100.0) * _currentDuration.TotalSeconds);
            TxtCurrentTime.Text = FormatTime(previewPos);
        }
    }

    private void CommitSeek()
    {
        if (_service != null && _currentDuration > TimeSpan.Zero)
        {
            var target = TimeSpan.FromSeconds((SliderSeek.Value / 100.0) * _currentDuration.TotalSeconds);
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

        float newVol = (float)SliderMediaVolume.Value;
        if (TxtMediaVolumePercent != null)
        {
            TxtMediaVolumePercent.Text = $"{(int)Math.Round(newVol)}%";
        }

        AudioMixerService.SetMasterVolume(newVol);
    }

    private void BtnMediaMute_Click(object sender, RoutedEventArgs e)
    {
        bool isMuted = AudioMixerService.ToggleMasterMute();
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
