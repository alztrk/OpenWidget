using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
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
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    private static readonly ConcurrentDictionary<string, TimeSpan> _durationCache = new();
    private CancellationTokenSource? _durationCts;

    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private IReadOnlyList<GlobalSystemMediaTransportControlsSession> _allSessions = Array.Empty<GlobalSystemMediaTransportControlsSession>();
    private int _sessionIndex = 0;

    private MediaTrackInfo _currentTrack = new(false, "", "", false, "");
    private readonly System.Timers.Timer _progressTimer;

    // High-precision stopwatch tracking
    private string _activeTrackKey = "";
    private readonly Stopwatch _trackStopwatch = new();
    private TimeSpan _trackBasePosition = TimeSpan.Zero;
    private TimeSpan _trackDuration = TimeSpan.Zero;

    public event Action<MediaTrackInfo>? MediaChanged;
    public event Action<TimeSpan, TimeSpan>? PositionChanged;
    public event Action? SessionsUpdated;

    public MediaTrackInfo CurrentTrack => _currentTrack;
    public int SessionCount => _allSessions?.Count ?? 0;
    public int CurrentSessionIndex => _sessionIndex + 1;

    public void SwitchToNextSession()
    {
        if (_manager == null) return;
        try
        {
            _allSessions = _manager.GetSessions();
            if (_allSessions.Count <= 1) return;

            _sessionIndex = (_sessionIndex + 1) % _allSessions.Count;
            AttachSession(_allSessions[_sessionIndex]);
        }
        catch { }
    }

    public void SwitchToPreviousSession()
    {
        if (_manager == null) return;
        try
        {
            _allSessions = _manager.GetSessions();
            if (_allSessions.Count <= 1) return;

            _sessionIndex = (_sessionIndex - 1 + _allSessions.Count) % _allSessions.Count;
            AttachSession(_allSessions[_sessionIndex]);
        }
        catch { }
    }

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
            _allSessions = _manager?.GetSessions() ?? Array.Empty<GlobalSystemMediaTransportControlsSession>();
            var current = _manager?.GetCurrentSession();

            if (current != null)
            {
                for (int i = 0; i < _allSessions.Count; i++)
                {
                    if (_allSessions[i].SourceAppUserModelId == current.SourceAppUserModelId)
                    {
                        _sessionIndex = i;
                        break;
                    }
                }
                AttachSession(current);
            }
            else if (_allSessions.Count > 0)
            {
                _sessionIndex = 0;
                AttachSession(_allSessions[0]);
            }
            else
            {
                AttachSession(null);
            }
        }
        catch { }
    }

    private void AttachSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        }

        _currentSession = session;

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

        SessionsUpdated?.Invoke();
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
                else if (source.Contains("Firefox", StringComparison.OrdinalIgnoreCase) || source.Contains("LibreWolf", StringComparison.OrdinalIgnoreCase) || source.StartsWith("83C1C0F3FA8524B1", StringComparison.OrdinalIgnoreCase)) source = "LibreWolf";

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
                    _trackBasePosition = TimeSpan.Zero;
                    _trackStopwatch.Restart();

                    if (_durationCache.TryGetValue(trackKey, out var cachedDur) && cachedDur > TimeSpan.Zero)
                    {
                        _trackDuration = cachedDur;
                    }
                    else
                    {
                        _trackDuration = TimeSpan.Zero;
                        ResolveWebMediaDurationAsync(props.Title, props.Artist, trackKey);
                    }
                }
                else if (_trackDuration <= TimeSpan.Zero)
                {
                    if (_durationCache.TryGetValue(trackKey, out var cachedDur) && cachedDur > TimeSpan.Zero)
                    {
                        _trackDuration = cachedDur;
                    }
                    else
                    {
                        ResolveWebMediaDurationAsync(props.Title, props.Artist, trackKey);
                    }
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

    private void ResolveWebMediaDurationAsync(string title, string artist, string trackKey)
    {
        try
        {
            _durationCts?.Cancel();
            _durationCts?.Dispose();
            _durationCts = new CancellationTokenSource();
            var token = _durationCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    var dur = await FetchDurationFromWebAsync(title, artist, token);
                    if (dur > TimeSpan.Zero)
                    {
                        _durationCache[trackKey] = dur;
                        if (_activeTrackKey == trackKey)
                        {
                            _trackDuration = dur;
                            var pos = CurrentPlaybackPosition;
                            _currentTrack = _currentTrack with { Position = pos, Duration = dur };
                            PositionChanged?.Invoke(pos, dur);
                            MediaChanged?.Invoke(_currentTrack);
                        }
                    }
                }
                catch { }
            }, token);
        }
        catch { }
    }

    private static async Task<TimeSpan> FetchDurationFromWebAsync(string title, string artist, CancellationToken ct)
    {
        try
        {
            string query = Uri.EscapeDataString(string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}");
            var url = $"https://www.youtube.com/results?search_query={query}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return TimeSpan.Zero;

            var html = await response.Content.ReadAsStringAsync(ct);
            var m = Regex.Match(html, "\"simpleText\":\"(\\d+:\\d+(?::\\d+)?)\"");
            if (m.Success)
            {
                return ParseDuration(m.Groups[1].Value);
            }
        }
        catch { }
        return TimeSpan.Zero;
    }

    private static TimeSpan ParseDuration(string raw)
    {
        try
        {
            var parts = raw.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int m) && int.TryParse(parts[1], out int s))
            {
                return TimeSpan.FromMinutes(m) + TimeSpan.FromSeconds(s);
            }
            if (parts.Length == 3 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m2) && int.TryParse(parts[2], out int s2))
            {
                return TimeSpan.FromHours(h) + TimeSpan.FromMinutes(m2) + TimeSpan.FromSeconds(s2);
            }
        }
        catch { }
        return TimeSpan.Zero;
    }

    public void Dispose()
    {
        _durationCts?.Cancel();
        _durationCts?.Dispose();
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
