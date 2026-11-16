using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;
public sealed class Video
{
    [Display("Video ID")]
    public string VideoId { get; set; } = string.Empty;

    [Display("Video URL")]
    public string VideoUrl { get; set; } = string.Empty;

    [Display("Title")]
    public string Title { get; set; } = string.Empty;

    [Display("Description")]
    public string Description { get; set; } = string.Empty;

    [Display("Channel ID")]
    public string ChannelId { get; set; } = string.Empty;

    [Display("Channel title")]
    public string ChannelTitle { get; set; } = string.Empty;

    [Display("Published at")]
    public DateTime PublishedAt { get; set; }

    [Display("Duration in seconds")]
    public double? DurationSeconds { get; set; }

    [Display("Has captions")]
    public bool? HasCaptions { get; set; }

    [Display("View count")]
    public long? ViewCount { get; set; }

    [Display("Video visibility")]
    public string PrivacyStatus { get; set; } = string.Empty;
}
