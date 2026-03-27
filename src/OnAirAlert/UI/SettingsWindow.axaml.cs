using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OnAirAlert.Models;

namespace OnAirAlert.UI;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    public bool Saved { get; private set; }

    public SettingsWindow() : this(new AppConfig()) { }

    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;
        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        var icalBox = this.FindControl<TextBox>("IcalUrlBox")!;
        var bgmBox = this.FindControl<TextBox>("BgmFileBox")!;
        var alertBox = this.FindControl<NumericUpDown>("AlertSecondsBox")!;
        var keywordsBox = this.FindControl<TextBox>("KeywordsBox")!;
        var positionBox = this.FindControl<ComboBox>("PositionBox")!;
        var autoStartBox = this.FindControl<CheckBox>("AutoStartBox")!;

        icalBox.Text = _config.IcalUrl;
        bgmBox.Text = _config.BgmFilePath;
        alertBox.Value = _config.AlertSecondsBefore;
        keywordsBox.Text = string.Join(", ", _config.MeetingKeywords);

        // Select matching position
        for (int i = 0; i < positionBox.Items.Count; i++)
        {
            if (positionBox.Items[i] is ComboBoxItem item &&
                item.Tag?.ToString() == _config.WindowPosition)
            {
                positionBox.SelectedIndex = i;
                break;
            }
        }

        autoStartBox.IsChecked = _config.AutoStart;
    }

    private void SaveToConfig()
    {
        var icalBox = this.FindControl<TextBox>("IcalUrlBox")!;
        var bgmBox = this.FindControl<TextBox>("BgmFileBox")!;
        var alertBox = this.FindControl<NumericUpDown>("AlertSecondsBox")!;
        var keywordsBox = this.FindControl<TextBox>("KeywordsBox")!;
        var positionBox = this.FindControl<ComboBox>("PositionBox")!;
        var autoStartBox = this.FindControl<CheckBox>("AutoStartBox")!;

        _config.IcalUrl = icalBox.Text ?? "";
        _config.BgmFilePath = bgmBox.Text ?? "assets\\bgm.mp3";
        _config.AlertSecondsBefore = (int)(alertBox.Value ?? 30);

        var keywordsText = keywordsBox.Text ?? "";
        _config.MeetingKeywords = keywordsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (positionBox.SelectedItem is ComboBoxItem selectedItem)
            _config.WindowPosition = selectedItem.Tag?.ToString() ?? "bottom-right";

        _config.AutoStart = autoStartBox.IsChecked ?? false;
    }

    private async void OnBrowseBgm(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "BGM ファイルを選択",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("音声ファイル") { Patterns = new[] { "*.mp3", "*.wav", "*.wma" } },
                new FilePickerFileType("すべてのファイル") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            var bgmBox = this.FindControl<TextBox>("BgmFileBox")!;
            bgmBox.Text = files[0].Path.LocalPath;
        }
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        SaveToConfig();
        Saved = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Saved = false;
        Close();
    }
}
