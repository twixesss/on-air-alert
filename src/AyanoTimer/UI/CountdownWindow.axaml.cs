using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AyanoTimer.Models;

namespace AyanoTimer.UI;

public partial class CountdownWindow : Window
{
    private readonly Border _rootBorder;
    private readonly TextBlock _titleText;
    private readonly TextBlock _countdownText;
    private readonly DispatcherTimer _updateTimer;
    private readonly DispatcherTimer _blinkTimer;

    private MeetingInfo? _meeting;
    private bool _isLive;
    private bool _blinkState;

    private static readonly IBrush BgNormal = new SolidColorBrush(Color.FromArgb(230, 32, 32, 32));
    private static readonly IBrush BgLive = new SolidColorBrush(Color.FromArgb(230, 204, 0, 0));
    private static readonly IBrush BgLiveDim = new SolidColorBrush(Color.FromArgb(230, 153, 0, 0));

    public CountdownWindow()
    {
        InitializeComponent();

        _rootBorder = this.FindControl<Border>("RootBorder")!;
        _titleText = this.FindControl<TextBlock>("TitleText")!;
        _countdownText = this.FindControl<TextBlock>("CountdownText")!;

        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _updateTimer.Tick += OnUpdateTick;

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += OnBlinkTick;

        Opened += (_, _) => PositionBottomRight();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CloseCountdown();
    }

    public void ShowCountdown(MeetingInfo meeting)
    {
        _meeting = meeting;
        _isLive = false;
        _blinkTimer.Stop();
        _rootBorder.Background = BgNormal;
        _titleText.Text = meeting.Title;
        _titleText.Foreground = new SolidColorBrush(Color.Parse("#AAAAAA"));
        _countdownText.FontSize = 40;
        _countdownText.FontFamily = new FontFamily("Consolas, monospace");
        UpdateCountdownText();
        _updateTimer.Start();

        // Position and fade in
        Opacity = 0;
        Show();
        PositionBottomRight();
        FadeIn();
    }

    public void UpdateRemainingSeconds(int seconds)
    {
        if (_isLive) return;
        _countdownText.Text = FormatSeconds(seconds);
    }

    public void SwitchToLive()
    {
        _isLive = true;
        _rootBorder.Background = BgLive;
        _titleText.Foreground = Brushes.White;
        _countdownText.Text = "\U0001f534 IS LIVE!";
        _countdownText.FontSize = 32;
        _countdownText.FontFamily = new FontFamily("Segoe UI, sans-serif");
        _blinkTimer.Start();
    }

    public void CloseCountdown()
    {
        _updateTimer.Stop();
        _blinkTimer.Stop();
        _isLive = false;
        _meeting = null;
        FadeOut();
    }

    private void OnUpdateTick(object? sender, EventArgs e)
    {
        if (_meeting == null || _isLive) return;
        UpdateCountdownText();
    }

    private void OnBlinkTick(object? sender, EventArgs e)
    {
        if (!_isLive) return;
        _blinkState = !_blinkState;
        _rootBorder.Background = _blinkState ? BgLiveDim : BgLive;
    }

    private void UpdateCountdownText()
    {
        if (_meeting == null) return;
        var remaining = (int)Math.Ceiling((_meeting.StartTime - DateTime.Now).TotalSeconds);
        if (remaining < 0) remaining = 0;
        _countdownText.Text = FormatSeconds(remaining);
    }

    private static string FormatSeconds(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        var m = totalSeconds / 60;
        var s = totalSeconds % 60;
        return $"{m:D2}:{s:D2}";
    }

    private void PositionBottomRight()
    {
        var screen = Screens.Primary;
        if (screen == null) return;
        var workArea = screen.WorkingArea;
        var scale = screen.Scaling;
        Position = new PixelPoint(
            workArea.Right - (int)(Width * scale) - (int)(20 * scale),
            workArea.Bottom - (int)(Height * scale) - (int)(20 * scale)
        );
    }

    private void FadeIn()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            Opacity += 0.1;
            if (Opacity >= 0.95)
            {
                Opacity = 0.95;
                timer.Stop();
            }
        };
        timer.Start();
    }

    private void FadeOut()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            Opacity -= 0.08;
            if (Opacity <= 0)
            {
                Opacity = 0;
                timer.Stop();
                Hide();
            }
        };
        timer.Start();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        CloseCountdown();
    }
}
