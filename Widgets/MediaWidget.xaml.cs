using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenWidget.Services;

namespace OpenWidget.Widgets;

public partial class MediaWidget : UserControl
{
    private MediaSessionService? _service;

    public MediaWidget()
    {
        InitializeComponent();
    }

    public void AttachService(MediaSessionService service)
    {
        _service = service;
        _service.MediaChanged += OnMediaChanged;
        UpdateTrackInfo(_service.CurrentTrack);
    }

    private void OnMediaChanged(MediaTrackInfo info)
    {
        Dispatcher.Invoke(() => UpdateTrackInfo(info));
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

        var geoKey = info.IsPlaying ? "GeoPause" : "GeoPlay";
        if (Application.Current.Resources.Contains(geoKey))
        {
            PathPlayPause.Data = Application.Current.Resources[geoKey] as Geometry;
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