using System;
using System.Collections.Generic;
using System.Threading;
using OnAirAlert.Models;

namespace OnAirAlert.Services;

public enum WatcherState
{
    Idle,
    Countdown,
    Live
}

public class MeetingWatcher : IDisposable
{
    private readonly CalendarService _calendarService;
    private readonly Func<AppConfig> _getConfig;
    private readonly SynchronizationContext _syncContext;

    private Timer? _fetchTimer;
    private Timer? _checkTimer;
    private List<MeetingInfo> _meetings = new();
    private MeetingInfo? _currentMeeting;
    private WatcherState _state = WatcherState.Idle;
    private DateTime _liveStartTime;

    public event Action<MeetingInfo>? CountdownStarted;
    public event Action<int>? CountdownTick;
    public event Action<MeetingInfo>? MeetingStarted;
    public event Action? CountdownFinished;
    public event Action<MeetingInfo?>? NextMeetingChanged;

    public MeetingInfo? NextMeeting { get; private set; }

    public MeetingWatcher(CalendarService calendarService, Func<AppConfig> getConfig)
    {
        _calendarService = calendarService;
        _getConfig = getConfig;
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public void Start()
    {
        // Fetch immediately, then every 5 minutes
        _fetchTimer = new Timer(OnFetchTick, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        // Check every 5 seconds
        _checkTimer = new Timer(OnCheckTick, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public void Stop()
    {
        _fetchTimer?.Dispose();
        _fetchTimer = null;
        _checkTimer?.Dispose();
        _checkTimer = null;
    }

    public void Restart()
    {
        Stop();
        _state = WatcherState.Idle;
        _currentMeeting = null;
        Start();
    }

    public void TriggerTest()
    {
        var testMeeting = new MeetingInfo
        {
            Title = "テスト再生",
            StartTime = DateTime.Now.AddSeconds(5)
        };

        _state = WatcherState.Countdown;
        _currentMeeting = testMeeting;
        PostToUI(() => CountdownStarted?.Invoke(testMeeting));
    }

    private async void OnFetchTick(object? state)
    {
        var config = _getConfig();
        _meetings = await _calendarService.FetchMeetingsAsync(config.IcalUrl, config.MeetingKeywords);
        var next = _calendarService.GetNextMeeting(_meetings);

        if (next?.Title != NextMeeting?.Title || next?.StartTime != NextMeeting?.StartTime)
        {
            NextMeeting = next;
            PostToUI(() => NextMeetingChanged?.Invoke(next));
        }
    }

    private void OnCheckTick(object? state)
    {
        var config = _getConfig();
        var now = DateTime.Now;

        switch (_state)
        {
            case WatcherState.Idle:
                var next = _calendarService.GetNextMeeting(_meetings);
                if (next == null) return;

                var secondsUntil = (next.StartTime - now).TotalSeconds;
                if (secondsUntil <= config.AlertSecondsBefore && secondsUntil > 0)
                {
                    _state = WatcherState.Countdown;
                    _currentMeeting = next;
                    PostToUI(() => CountdownStarted?.Invoke(next));
                }
                break;

            case WatcherState.Countdown:
                if (_currentMeeting == null) { _state = WatcherState.Idle; return; }

                var remaining = (int)Math.Ceiling((_currentMeeting.StartTime - now).TotalSeconds);
                if (remaining <= 0)
                {
                    _state = WatcherState.Live;
                    _liveStartTime = now;
                    PostToUI(() => MeetingStarted?.Invoke(_currentMeeting));
                }
                else
                {
                    PostToUI(() => CountdownTick?.Invoke(remaining));
                }
                break;

            case WatcherState.Live:
                var liveElapsed = (now - _liveStartTime).TotalSeconds;
                if (liveElapsed >= 60)
                {
                    _state = WatcherState.Idle;
                    _currentMeeting = null;
                    PostToUI(() => CountdownFinished?.Invoke());
                }
                break;
        }
    }

    private void PostToUI(Action action)
    {
        _syncContext.Post(_ => action(), null);
    }

    public void Dispose()
    {
        Stop();
    }
}
