using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public class SearchVideosInput
{
    [Display("Channel URL", Description = "Optional public channel URL. OAuth suggestions use the selected account; direct public URLs remain accepted. Supply Channel URL or Channel ID, not both. Without either, Search term searches public videos globally.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(ChannelUrlDataHandler))]
    public string? ChannelUrl { get; set; }

    [Display("Search term", Description = "Optional YouTube search query. Supports YouTube's OR (|) and exclusion (-) operators. Without a term, retrieves the channel's uploads.")]
    public string? SearchTerm { get; set; }

    [Display("Published after", Description = "Include videos published at or after this time. Dates without a timezone are interpreted as UTC.")]
    public DateTime? PublishedAfter { get; set; }

    [Display("Published before", Description = "Include videos published at or before this time. Dates without a timezone are interpreted as UTC.")]
    public DateTime? PublishedBefore { get; set; }

    [Display("Sort by", Description = "Defaults to relevance for keyword searches and newest first for channel uploads. Relevance without a search term uses newest first.")]
    [StaticDataSource(typeof(VideoSortDataSource))]
    public string? SortBy { get; set; }

    [Display("Maximum results", Description = "Maximum number of matching videos to return, from 1 to 10,000. Defaults to 100. Pagination is automatic; larger searches consume more API quota.")]
    public int? MaximumResults { get; set; }

    [Display("Caption availability")]
    [StaticDataSource(typeof(VideoCaptionDataSource))]
    public string? CaptionAvailability { get; set; }

    [Display("Duration", Description = "Short means under four minutes, not the YouTube Shorts format.")]
    [StaticDataSource(typeof(VideoDurationDataSource))]
    public string? Duration { get; set; }

    [Display("Channel ID", Description = "Alternative to Channel URL. OAuth suggestions show owned channel IDs; public channel IDs can be entered or mapped. Supply one channel selector.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(ChannelIdDataHandler))]
    public string? ChannelId { get; set; }

    [Display("Video definition")]
    [StaticDataSource(typeof(DefinitionDataSource))]
    public string? Definition { get; set; }

    [Display("Video dimension")]
    [StaticDataSource(typeof(DimensionDataSource))]
    public string? Dimension { get; set; }

    [Display("Video license")]
    [StaticDataSource(typeof(LicenseDataSource))]
    public string? License { get; set; }

    [Display("Safe search")]
    [StaticDataSource(typeof(SafeSearchDataSource))]
    public string? SafeSearch { get; set; }

    [Display("Region code", Description = "Two-letter country code, e.g. HU or US. Limits indexed results to videos viewable there.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(RegionDataHandler))]
    public string? RegionCode { get; set; }

    [Display("Relevance language", Description = "Language preference for indexed search; does not guarantee the video's language.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(LanguageDataHandler))]
    public string? RelevanceLanguage { get; set; }

    [Display("Video category ID")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(CategoryDataHandler))]
    public string? CategoryId { get; set; }
}
