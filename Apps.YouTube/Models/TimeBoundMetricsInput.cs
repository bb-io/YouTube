using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class TimeBoundMetricsInput
{
    [Display("Start date", Description = "Inclusive YouTube reporting date in Pacific time. Time-of-day is ignored.")]
    public DateTime StartDate { get; set; }

    [Display("End date", Description = "Inclusive requested end date. Recent reporting data can be delayed; time-of-day is ignored.")]
    public DateTime EndDate { get; set; }

    [Display("Video ID", Description = "Optional. Choose, enter or map a video owned by the connected channel. Omit for channel-wide totals.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string? VideoId { get; set; }
}
