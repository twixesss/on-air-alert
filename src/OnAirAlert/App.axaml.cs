using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using OnAirAlert.Models;
using OnAirAlert.Services;
using OnAirAlert.UI;

namespace OnAirAlert;

public class App : Application
{
    private ConfigService _configService = null!;
    private CalendarService _calendarService = null!;
    private AudioService _audioService = null!;
    private MeetingWatcher _meetingWatcher = null!;
    private CountdownWindow _countdownWindow = null!;
    private NativeMenuItem _nextMeetingItem = null!;
    private TrayIcon _trayIcon = null!;
    private WindowIcon _defaultIcon = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _configService = new ConfigService();
            _calendarService = new CalendarService();
            _audioService = new AudioService();
            _meetingWatcher = new MeetingWatcher(_calendarService, () => _configService.Config);
            _countdownWindow = new CountdownWindow();

            SetupTrayIcon();

            _meetingWatcher.CountdownStarted += OnCountdownStarted;
            _meetingWatcher.CountdownTick += OnCountdownTick;
            _meetingWatcher.MeetingStarted += OnMeetingStarted;
            _meetingWatcher.CountdownFinished += OnCountdownFinished;
            _meetingWatcher.NextMeetingChanged += OnNextMeetingChanged;

            _meetingWatcher.Start();

            if (string.IsNullOrWhiteSpace(_configService.Config.IcalUrl))
                Dispatcher.UIThread.Post(() => ShowSettings());

            desktop.Exit += (_, _) =>
            {
                _meetingWatcher.Dispose();
                _audioService.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon()
    {
        _nextMeetingItem = new NativeMenuItem("次の予定: なし") { IsEnabled = false };

        var settingsItem = new NativeMenuItem("設定...");
        settingsItem.Click += (_, _) => Dispatcher.UIThread.Post(() => ShowSettings());

        var testItem = new NativeMenuItem("テスト再生");
        testItem.Click += (_, _) => Dispatcher.UIThread.Post(() => _meetingWatcher.TriggerTest());

        var exitItem = new NativeMenuItem("終了");
        exitItem.Click += (_, _) =>
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        };

        var menu = new NativeMenu();
        menu.Items.Add(_nextMeetingItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(testItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);

        _defaultIcon = IconGenerator.CreateDefaultIcon();

        _trayIcon = new TrayIcon
        {
            ToolTipText = "OnAirAlert",
            Menu = menu,
            Icon = _defaultIcon,
            IsVisible = true
        };

        var icons = new TrayIcons { _trayIcon };
        SetValue(TrayIcon.IconsProperty, icons);
    }

    private void ShowSettings()
    {
        var settingsWindow = new SettingsWindow(_configService.Config);
        settingsWindow.Closed += (_, _) =>
        {
            if (settingsWindow.Saved)
            {
                if (OperatingSystem.IsWindows())
                    AutoStartService.SetEnabled(_configService.Config.AutoStart);

                _configService.Save();
                _meetingWatcher.Restart();
            }
        };
        settingsWindow.Show();
    }

    private void OnCountdownStarted(MeetingInfo meeting)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _countdownWindow.ShowCountdown(meeting);
            var bgmPath = _configService.GetAbsolutePath(_configService.Config.BgmFilePath);
            _audioService.Play(bgmPath);

            var remaining = (int)Math.Ceiling((meeting.StartTime - DateTime.Now).TotalSeconds);
            _trayIcon.Icon = IconGenerator.CreateNumberIcon(remaining);
        });
    }

    private void OnCountdownTick(int remainingSeconds)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _countdownWindow.UpdateRemainingSeconds(remainingSeconds);
            _trayIcon.Icon = IconGenerator.CreateNumberIcon(remainingSeconds);
            _trayIcon.ToolTipText = $"OnAirAlert - あと {remainingSeconds}秒";
        });
    }

    private void OnMeetingStarted(MeetingInfo meeting)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _audioService.Stop();
            _countdownWindow.SwitchToLive();
            _trayIcon.Icon = IconGenerator.CreateLiveIcon();
            _trayIcon.ToolTipText = $"OnAirAlert - {meeting.Title} IS LIVE!";
        });
    }

    private void OnCountdownFinished()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _countdownWindow.CloseCountdown();
            _trayIcon.Icon = _defaultIcon;
            _trayIcon.ToolTipText = "OnAirAlert";
        });
    }

    private void OnNextMeetingChanged(MeetingInfo? meeting)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _nextMeetingItem.Header = meeting == null
                ? "次の予定: なし"
                : $"次の予定: {meeting.StartTime:HH:mm} {meeting.Title}";
        });
    }
}
