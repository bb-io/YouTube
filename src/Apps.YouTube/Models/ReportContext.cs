using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class ReportContext
{
    [Display("Report type")]
    public string? ReportType { get; set; }

    [Display("Report metrics")]
    public IEnumerable<string>? Metrics { get; set; }

    [Display("Report breakdown")]
    public string? Dimensions { get; set; }

    [Display("Filters")]
    public string? Filters { get; set; }

    [Display("Sort")]
    public string? Sort { get; set; }

    [Display("Video ID")]
    public string? VideoId { get; set; }

    [Display("Country")]
    public string? CountryCode { get; set; }
}
