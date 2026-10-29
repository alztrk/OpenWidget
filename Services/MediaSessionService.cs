using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace OpenWidget.Services;

public record MediaTrackInfo(
    bool HasMedia,
    string Title,
    string Artist,
    bool IsPlaying,
    string SourceApp
);

public class MediaSessionService : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private MediaTrackInfo _currentTrack = new(false, "", "", false, "");

    public event Action<MediaTrackInfo>? MediaChanged;

    public MediaTrackInfo CurrentTrack => _currentTrack;

    public async Task InitializeAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_manager != null)
            {
                _manager.CurrentSessionChanged += OnCurrentSessionChanged;
                UpdateCurrentSession();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GSMTC Init error: {ex.Message}");
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UpdateCurrentSession();
    }

    private void UpdateCurrentSession()
    {
        try
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }

            _currentSession = _manager?.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                _ = RefreshMediaInfoAsync();
            }
            else
            {
                _currentTrack = new MediaTrackInfo(false, "", "", false, "");
                MediaChanged?.Invoke(_currentTrack);
            }
        }
        catch { }
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _ = RefreshMediaInfoAsync();
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        _ = RefreshMediaInfoAsync();
    }

    public async Task RefreshMediaInfoAsync()
    {
        try
        {
            if (_currentSession == null)
            {
                _currentSession = _manager?.GetCurrentSession();
                if (_currentSession == null)
                {
                    _currentTrack = new MediaTrackInfo(false, "", "", false, "");
                    MediaChanged?.Invoke(_currentTrack);
                    return;
                }
            }

            var props = await _currentSession.TryGetMediaPropertiesAsync();
            var playback = _currentSession.GetPlaybackInfo();

            if (props != null && !string.IsNullOrWhiteSpace(props.Title))
            {
                bool isPlaying = playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                string source = _currentSession.SourceAppUserModelId ?? "Medya";
                if (source.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) source = "Spotify";
                else if (source.Contains("Chrome", StringComparison.OrdinalIgnoreCase)) source = "Chrome";
                else if (source.Contains("Edge", StringComparison.OrdinalIgnoreCase)) source = "Edge";

                _currentTrack = new MediaTrackInfo(
                    HasMedia: true,
                    Title: props.Title,
                    Artist: string.IsNullOrWhiteSpace(props.Artist) ? source : props.Artist,
                    IsPlaying: isPlaying,
                    SourceApp: source
                );
            }
            else
            {
                _currentTrack = new MediaTrackInfo(false, "", "", false, "");
            }

            MediaChanged?.Invoke(_currentTrack);
        }
        catch
        {
            _currentTrack = new MediaTrackInfo(false, "", "", false, "");
            MediaChanged?.Invoke(_currentTrack);
        }
    }

    public async Task TogglePlayPauseAsync()
    {
        try
        {
            if (_currentSession != null)
            {
                await _currentSession.TryTogglePlayPauseAsync();
                await Task.Delay(100);
                await RefreshMediaInfoAsync();
            }
        }
        catch { }
    }

    public async Task SkipNextAsync()
    {
        try
        {
            if (_currentSession != null)
            {
                await _currentSession.TrySkipNextAsync();
                await Task.Delay(150);
                await RefreshMediaInfoAsync();
            }
        }
        catch { }
    }

    public async Task SkipPreviousAsync()
    {
        try
        {
            if (_currentSession != null)
            {
                await _currentSession.TrySkipPreviousAsync();
                await Task.Delay(150);
                await RefreshMediaInfoAsync();
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_manager != null)
        {
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
        }
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }
    }
}