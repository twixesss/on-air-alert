using System.Collections.Generic;

namespace OnAirAlert.Models;

public class AppConfig
{
    public string IcalUrl { get; set; } = "";
    public string BgmFilePath { get; set; } = "assets\\bgm.mp3";
    public int AlertSecondsBefore { get; set; } = 30;
    public string WindowPosition { get; set; } = "bottom-right";
    public List<string> MeetingKeywords { get; set; } = new()
    {
        "ミーティング", "会議", "MTG", "sync", "meet", "zoom", "teams"
    };
    public bool AutoStart { get; set; } = false;
}
