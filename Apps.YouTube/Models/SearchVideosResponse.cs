using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class SearchVideosResponse
{
    [Display("Videos")]
    public List<Video> Videos { get; set; } = [];

    [Display("Video IDs")]
    public IEnumerable<string> VideoIds => Videos.Select(x => x.VideoId).ToArray();

    [Display("Video URLs")]
    public IEnumerable<string> VideoUrls => Videos.Select(x => x.VideoUrl).ToArray();

    [Display("Returned count")]
    public int ReturnedCount => Videos.Count;

    [Display("First video")]
    public Video? FirstVideo => Videos.FirstOrDefault();

    [Display("First video URL")]
    public string FirstVideoUrl => FirstVideo?.VideoUrl ?? string.Empty;

    [Display("More results available", Description = "True when additional matching videos were found beyond Maximum results.")]
    public bool MoreResultsAvailable { get; set; }

    [Display("Search limit reached", Description = "True when a channel keyword search reaches YouTube's 500-video search ceiling. Narrow the date range to retrieve a different subset. Search results are not a complete channel inventory.")]
    public bool SearchLimitReached { get; set; }
}
