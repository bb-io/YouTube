using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class TimeBoundMetricsResponse
{
    [Display("Channel ID")]
    public string ChannelId { get; set; } = "";

    [Display("Video ID", Description = "Empty for channel-wide totals.")]
    public string? VideoId { get; set; }

    [Display("Requested start date", Description = "Inclusive reporting date in Pacific time.")]
    public DateTime StartDate { get; set; }

    [Display("Requested end date", Description = "Requested boundary, not a guarantee that data is available through this date.")]
    public DateTime EndDate { get; set; }

    [Display("Has data", Description = "False when YouTube returns no report row. Metrics remain empty when unavailable.")]
    public bool HasData { get; set; }

    [Display("Views", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public long? Views { get; set; }

    [Display("Engaged views", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public long? EngagedViews { get; set; }

    [Display("Estimated minutes watched", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public double? EstimatedMinutesWatched { get; set; }

    [Display("Average view duration in seconds", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public double? AverageViewDurationSeconds { get; set; }

    [Display("Average view percentage", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public double? AverageViewPercentage { get; set; }

    [Display("Likes", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public long? Likes { get; set; }

    [Display("Comments", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public long? Comments { get; set; }

    [Display("Shares", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public long? Shares { get; set; }

    [Display("Subscribers gained", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public long? SubscribersGained { get; set; }

    [Display("Subscribers lost", Description = "Empty when unavailable. These are values for the requested reporting period.")]
    public long? SubscribersLost { get; set; }

    [Display("Retrieved at", Description = "UTC retrieval time. Recent data may lag or be revised.")]
    public DateTime RetrievedAt { get; set; }
}
