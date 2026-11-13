using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class VideoStatisticsResponse
{
    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Video URL")]
    public string VideoUrl { get; set; } = "";

    [Display("Title")]
    public string Title { get; set; } = "";

    [Display("Channel ID")]
    public string ChannelId { get; set; } = "";

    [Display("Video visibility")]
    public string PrivacyStatus { get; set; } = "";

    [Display("View count", Description = "Current cumulative counter. Unavailable counts are empty, not zero.")]
    public long? ViewCount { get; set; }

    [Display("Like count", Description = "Current cumulative counter. Unavailable counts are empty, not zero.")]
    public long? LikeCount { get; set; }

    [Display("Comment count", Description = "Current cumulative counter. Unavailable counts are empty, not zero.")]
    public long? CommentCount { get; set; }

    [Display("Retrieved at", Description = "UTC retrieval time for this snapshot.")]
    public DateTime RetrievedAt { get; set; }
}
