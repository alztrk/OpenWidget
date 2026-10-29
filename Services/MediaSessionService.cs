using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Windows.Media.Control;

namespace OpenWidget.Services;

public record MediaTrackInfo(
    bool HasMedia,
    string Title,
    string Artist,
    bool IsPlaying,
    string SourceApp,
    byte[]? ThumbnailBytes = null,
    TimeSpan Position = default,
    TimeSpan Duration = default
);

public class MediaSessionService : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private MediaTrackInfo _currentTrack = new(false, "", "", false, "");
    private readonly System.Timers.Timer _progressTimer;

    public event Action<MediaTrackInfo>? MediaChanged;
    public event Action<TimeSpan, TimeSpan>? PositionChanged;

    public MediaTrackInfo CurrentTrack => _currentTrack;

    public MediaSessionService()
    {
        _progressTimer = new System.Timers.Timer(1000);
        _progressTimer.Elapsed += OnProgressTimerElapsed;
    }

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
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }

            _currentSession = _manager?.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
                _ = RefreshMediaInfoAsync();
            }
            else
            {
                _progressTimer.Stop();
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

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        UpdateTimelineFromSession();
    }

    private void UpdateTimelineFromSession()
    {
        if (_currentSession == null || !_currentTrack.HasMedia) return;
        try
        {
            var timeline = _currentSession.GetTimelineProperties();
            if (timeline != null)
            {
                var pos = timeline.Position;
                var dur = timeline.EndTime;
                _currentTrack = _currentTrack with { Position = pos, Duration = dur };
                PositionChanged?.Invoke(pos, dur);
            }
        }
        catch { }
    }

    private void OnProgressTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_currentTrack.HasMedia || !_currentTrack.IsPlaying || _currentTrack.Duration <= TimeSpan.Zero) return;

        var nextPos = _currentTrack.Position.Add(TimeSpan.FromSeconds(1));
        if (nextPos > _currentTrack.Duration) nextPos = _currentTrack.Duration;

        _currentTrack = _currentTrack with { Position = nextPos };
        PositionChanged?.Invoke(nextPos, _currentTrack.Duration);
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
                    _progressTimer.Stop();
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

                byte[]? thumbBytes = null;
                if (props.Thumbnail != null)
                {
                    try
                    {
                        using var streamRef = await props.Thumbnail.OpenReadAsync();
                        using var stream = streamRef.AsStreamForRead();
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);
                        thumbBytes = ms.ToArray();
                    }
                    catch { }
                }

                TimeSpan pos = TimeSpan.Zero;
                TimeSpan dur = TimeSpan.Zero;
                try
                {
                    var timeline = _currentSession.GetTimelineProperties();
                    if (timeline != null)
                    {
                        pos = timeline.Position;
                        dur = timeline.EndTime;
                    }
                }
                catch { }

                _currentTrack = new MediaTrackInfo(
                    HasMedia: true,
                    Title: props.Title,
                    Artist: string.IsNullOrWhiteSpace(props.Artist) ? source : props.Artist,
                    IsPlaying: isPlaying,
                    SourceApp: source,
                    ThumbnailBytes: thumbBytes,
                    Position: pos,
                    Duration: dur
                );

                if (isPlaying && dur > TimeSpan.Zero)
                {
                    _progressTimer.Start();
                }
                else
                {
                    _progressTimer.Stop();
                }
            }
            else
            {
                _progressTimer.Stop();
                _currentTrack = new MediaTrackInfo(false, "", "", false, "");
            }

            MediaChanged?.Invoke(_currentTrack);
        }
        catch
        {
            _progressTimer.Stop();
            _currentTrack = new MediaTrackInfo(false, "", "", false, "");
            MediaChanged?.Invoke(_currentTrack);
        }
    }

    public async Task SeekToAsync(TimeSpan position)
    {
        try
        {
            if (_currentSession != null)
            {
                await _currentSession.TryChangePlaybackPositionAsync(position.Ticks);
                _currentTrack = _currentTrack with { Position = position };
                PositionChanged?.Invoke(position, _currentTrack.Duration);
            }
        }
        catch { }
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
        _progressTimer.Stop();
        _progressTimer.Dispose();
        if (_manager != null)
        {
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
        }
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        }
    }
}