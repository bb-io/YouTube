using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class LifetimeStatisticsResponse
{
    [Display("Channel ID")]
    public string ChannelId { get; set; } = "";

    [Display("Channel URL")]
    public string ChannelUrl { get; set; } = "";

    [Display("Channel title")]
    public string ChannelTitle { get; set; } = "";

    [Display("View count", Description = "Current cumulative channel counter from the Data API, not a date-range Analytics report.")]
    public long? ViewCount { get; set; }

    [Display("Subscriber count", Description = "Rounded by YouTube to three significant figures. Unavailable or hidden counts are empty.")]
    public long? SubscriberCount { get; set; }

    [Display("Subscriber count hidden")]
    public bool SubscriberCountHidden { get; set; }

    [Display("Public video count", Description = "Counts public videos only, including when the connection is the owner.")]
    public long? PublicVideoCount { get; set; }

    [Display("Retrieved at", Description = "UTC retrieval time for this snapshot.")]
    public DateTime RetrievedAt { get; set; }
}
