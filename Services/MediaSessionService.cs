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

    // High-precision stopwatch tracking
    private string _activeTrackKey = "";
    private readonly Stopwatch _trackStopwatch = new();
    private TimeSpan _trackBasePosition = TimeSpan.Zero;
    private TimeSpan _trackDuration = TimeSpan.Zero;

    public event Action<MediaTrackInfo>? MediaChanged;
    public event Action<TimeSpan, TimeSpan>? PositionChanged;

    public MediaTrackInfo CurrentTrack => _currentTrack;

    public TimeSpan CurrentPlaybackPosition
    {
        get
        {
            var elapsed = _trackBasePosition + _trackStopwatch.Elapsed;
            if (_trackDuration > TimeSpan.Zero && elapsed > _trackDuration)
            {
                return _trackDuration;
            }
            return elapsed;
        }
    }

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
                _trackStopwatch.Stop();
                _activeTrackKey = "";
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
            // Only sync timeline if player reports a real non-zero duration (e.g. Spotify)
            if (timeline != null && timeline.EndTime > TimeSpan.Zero)
            {
                _trackDuration = timeline.EndTime;
                var elapsedSinceSnap = DateTimeOffset.UtcNow - timeline.LastUpdatedTime;
                if (elapsedSinceSnap < TimeSpan.Zero) elapsedSinceSnap = TimeSpan.Zero;
                _trackBasePosition = timeline.Position + (_currentTrack.IsPlaying ? elapsedSinceSnap : TimeSpan.Zero);
                if (_trackBasePosition > _trackDuration) _trackBasePosition = _trackDuration;
                _trackStopwatch.Restart();

                var pos = CurrentPlaybackPosition;
                _currentTrack = _currentTrack with { Position = pos, Duration = _trackDuration };
                PositionChanged?.Invoke(pos, _trackDuration);
            }
        }
        catch { }
    }

    private void OnProgressTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_currentTrack.HasMedia || !_currentTrack.IsPlaying) return;

        var pos = CurrentPlaybackPosition;
        _currentTrack = _currentTrack with { Position = pos, Duration = _trackDuration };
        PositionChanged?.Invoke(pos, _trackDuration);
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
                    _trackStopwatch.Stop();
                    _activeTrackKey = "";
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

                string trackKey = $"{props.Title}|{props.Artist}";
                bool trackChanged = trackKey != _activeTrackKey;

                var timeline = _currentSession.GetTimelineProperties();
                if (timeline != null && timeline.EndTime > TimeSpan.Zero)
                {
                    _trackDuration = timeline.EndTime;
                    var elapsedSinceSnap = DateTimeOffset.UtcNow - timeline.LastUpdatedTime;
                    if (elapsedSinceSnap < TimeSpan.Zero) elapsedSinceSnap = TimeSpan.Zero;
                    _trackBasePosition = timeline.Position + (isPlaying ? elapsedSinceSnap : TimeSpan.Zero);
                    if (_trackBasePosition > _trackDuration) _trackBasePosition = _trackDuration;
                    _trackStopwatch.Restart();
                }
                else if (trackChanged)
                {
                    _trackDuration = TimeSpan.Zero;
                    _trackBasePosition = TimeSpan.Zero;
                    _trackStopwatch.Restart();
                }

                _activeTrackKey = trackKey;

                if (isPlaying)
                {
                    if (!_trackStopwatch.IsRunning) _trackStopwatch.Start();
                    _progressTimer.Start();
                }
                else
                {
                    if (_trackStopwatch.IsRunning) _trackStopwatch.Stop();
                    _progressTimer.Stop();
                }

                var currentPos = CurrentPlaybackPosition;
                _currentTrack = new MediaTrackInfo(
                    HasMedia: true,
                    Title: props.Title,
                    Artist: string.IsNullOrWhiteSpace(props.Artist) ? source : props.Artist,
                    IsPlaying: isPlaying,
                    SourceApp: source,
                    ThumbnailBytes: thumbBytes,
                    Position: currentPos,
                    Duration: _trackDuration
                );
            }
            else
            {
                _progressTimer.Stop();
                _trackStopwatch.Stop();
                _activeTrackKey = "";
                _currentTrack = new MediaTrackInfo(false, "", "", false, "");
            }

            MediaChanged?.Invoke(_currentTrack);
        }
        catch
        {
            _progressTimer.Stop();
            _trackStopwatch.Stop();
            _activeTrackKey = "";
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
                _trackBasePosition = position;
                _trackStopwatch.Restart();
                var currentPos = CurrentPlaybackPosition;
                _currentTrack = _currentTrack with { Position = currentPos };
                PositionChanged?.Invoke(currentPos, _trackDuration);
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
        _trackStopwatch.Stop();
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
