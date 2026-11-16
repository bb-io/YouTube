using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class LiveVideo
{
    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Video URL")]
    public string VideoUrl => "https://www.youtube.com/watch?v=" + VideoId;

    [Display("Event type", Description = "created or started.")]
    public string EventType { get; set; } = "";

    [Display("Title")]
    public string Title { get; set; } = "";

    [Display("Broadcast status")]
    public string Status { get; set; } = "";

    [Display("Scheduled start")]
    public DateTime? ScheduledStart { get; set; }

    [Display("Actual start")]
    public DateTime? ActualStart { get; set; }
}
