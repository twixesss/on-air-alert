using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using OnAirAlert.Models;

namespace OnAirAlert.Services;

public class CalendarService
{
    private static readonly HttpClient HttpClient = new();
    private static readonly TimeZoneInfo TokyoTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "error.log");

    private List<MeetingInfo> _cachedMeetings = new();

    public async Task<List<MeetingInfo>> FetchMeetingsAsync(string icalUrl, List<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(icalUrl))
            return _cachedMeetings;

        try
        {
            var icsText = await LoadIcsTextAsync(icalUrl);
            var calendar = Calendar.Load(icsText);
            var now = DateTime.Now;
            var endRange = now.AddDays(1);
            var meetings = new List<MeetingInfo>();

            foreach (var evt in calendar.Events)
            {
                if (evt.IsAllDay)
                    continue;

                var summary = evt.Summary ?? "";

                if (keywords.Count > 0 &&
                    !keywords.Any(kw => summary.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (evt.RecurrenceRules != null && evt.RecurrenceRules.Count > 0)
                {
                    var occurrences = evt.GetOccurrences(
                        new CalDateTime(now.ToUniversalTime()),
                        new CalDateTime(endRange.ToUniversalTime()));
                    foreach (var occ in occurrences)
                    {
                        var startTime = ConvertToLocalTime(occ.Period.StartTime);
                        meetings.Add(new MeetingInfo { Title = summary, StartTime = startTime });
                    }
                }
                else
                {
                    var startTime = ConvertToLocalTime(evt.DtStart);
                    if (startTime > now && startTime < endRange)
                        meetings.Add(new MeetingInfo { Title = summary, StartTime = startTime });
                }
            }

            _cachedMeetings = meetings.OrderBy(m => m.StartTime).ToList();
        }
        catch (Exception ex)
        {
            Log($"Calendar fetch error: {ex.Message}");
        }

        return _cachedMeetings;
    }

    public MeetingInfo? GetNextMeeting(List<MeetingInfo> meetings)
    {
        var now = DateTime.Now;
        return meetings.FirstOrDefault(m => m.StartTime > now);
    }

    private static bool IsLocalFile(string url)
    {
        return !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> LoadIcsTextAsync(string icalUrl)
    {
        if (IsLocalFile(icalUrl))
        {
            var path = Path.IsPathRooted(icalUrl)
                ? icalUrl
                : Path.Combine(AppContext.BaseDirectory, icalUrl);
            return await File.ReadAllTextAsync(path);
        }

        return await HttpClient.GetStringAsync(icalUrl);
    }

    private static DateTime ConvertToLocalTime(IDateTime icalDateTime)
    {
        var dt = icalDateTime.AsDateTimeOffset;
        return TimeZoneInfo.ConvertTime(dt, TokyoTimeZone).DateTime;
    }

    private static void Log(string message)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n"); }
        catch { /* ignore log failures */ }
    }
}
