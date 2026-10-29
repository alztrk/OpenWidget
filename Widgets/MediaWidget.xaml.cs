using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class MediaWidget : UserControl
{
    private MediaSessionService? _service;
    private TimeSpan _currentDuration = TimeSpan.Zero;
    private bool _isUserSeeking = false;

    public MediaWidget()
    {
        InitializeComponent();
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
            if (dur > TimeSpan.Zero)
            {
                SeekPanel.Visibility = Visibility.Visible;
                double pct = Math.Clamp((pos.TotalSeconds / dur.TotalSeconds) * 100.0, 0.0, 100.0);
                SliderSeek.Value = pct;
                TxtTime.Text = $"{FormatTime(pos)} / {FormatTime(dur)}";
            }
            else
            {
                SeekPanel.Visibility = Visibility.Collapsed;
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
        if (info.Duration > TimeSpan.Zero)
        {
            SeekPanel.Visibility = Visibility.Visible;
            double pct = Math.Clamp((info.Position.TotalSeconds / info.Duration.TotalSeconds) * 100.0, 0.0, 100.0);
            SliderSeek.Value = pct;
            TxtTime.Text = $"{FormatTime(info.Position)} / {FormatTime(info.Duration)}";
        }
        else
        {
            SeekPanel.Visibility = Visibility.Collapsed;
        }
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
            TxtTime.Text = $"{FormatTime(previewPos)} / {FormatTime(_currentDuration)}";
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
