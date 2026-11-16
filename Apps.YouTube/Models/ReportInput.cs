using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class ReportInput
{
    [Display("Start date")]
    public DateTime StartDate { get; set; }

    [Display("End date")]
    public DateTime EndDate { get; set; }

    [Display("Report metrics", Description = "Select one or more individual metrics, or map a collection of metric names. Defaults to Views. Guided choices follow the report context. Revenue metrics require monetary access.")]
    [DataSource(typeof(ReportMetricDataHandler))]
    public IEnumerable<string> Metrics { get; set; } = ["views"];

    [Display("Report type", Description = "channel (default) or contentOwner. Content-owner reports require an authorized YouTube partner identity.")]
    [StaticDataSource(typeof(ReportTypeDataSource))]
    public string? ReportType { get; set; }

    [Display("Channel ID", Description = "Omit to use the authenticated channel. OAuth suggestions show the selected account; choosing an ID does not grant access. Ignored for content-owner reports.")]
    [DataSource(typeof(ChannelIdDataHandler))]
    public string? ChannelId { get; set; }

    [Display("Content owner ID", Description = "Partner-only identity for Content owner reports. Use advanced metrics, breakdown and Filters for partner-specific combinations; normal channel OAuth does not grant partner access.")]
    public string? ContentOwnerId { get; set; }

    [Display("Report breakdown", Description = "Choose a compatible breakdown, or enter/map dimensions. Totals omits the breakdown. Monthly reports require both dates on the first of a month. Ranked video reports require a supported descending Sort. Custom combinations are validated by Google.")]
    [DataSource(typeof(ReportDimensionDataHandler))]
    public string? Dimensions { get; set; }

    [Display("Filters", Description = "Advanced Analytics filter expression. Without helper fields it is passed unchanged. With helpers, only compatible video/country equalities can be merged; conflicts raise an error.")]
    public string? Filters { get; set; }

    [Display("Sort", Description = "Choose a compatible sort or enter/map an Analytics sort expression. Omit to retain Google's default. Ranked video reports require a supported descending metric.")]
    [DataSource(typeof(ReportSortDataHandler))]
    public string? Sort { get; set; }

    [Display("Video ID", Description = "Optional owned-video filter for common channel reports. Choose, enter or map an ID or URL. Not supported by the guided ranked-video breakdown. Omit when using advanced custom filters.")]
    [DataSource(typeof(VideoDataHandler))]
    public string? VideoId { get; set; }

    [Display("Country", Description = "Optional country filter for compatible channel reports. Cannot be combined with a country breakdown or conflicting advanced filters. Omit to keep all countries.")]
    [DataSource(typeof(ReportCountryDataHandler))]
    public string? CountryCode { get; set; }

    [Display("Maximum rows", Description = "Default 1,000; range 1–10,000. Pagination is internal.")]
    public int? MaximumRows { get; set; }
}
