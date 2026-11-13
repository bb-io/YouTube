using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ChannelVideoSearchInput
{
    [Display("Channel URL", Description = "Omit to use the connected YouTube or Brand Account channel. OAuth suggestions do not grant access to other accounts. A supplied URL must belong to the selected OAuth identity.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(ChannelUrlDataHandler))]
    public string? ChannelUrl { get; set; }

    [Display("Video visibility", Description = "Defaults to all accessible videos of the owned channel.")]
    [StaticDataSource(typeof(VideoVisibilityDataSource))]
    public string? VideoVisibility { get; set; }

    [Display("Search term", Description = "Case-insensitive match in title or description; uses the upload inventory rather than the search index.")]
    public string? SearchTerm { get; set; }

    [Display("Published after", Description = "Inclusive UTC publication date.")]
    public DateTime? PublishedAfter { get; set; }

    [Display("Published before", Description = "Inclusive UTC publication date.")]
    public DateTime? PublishedBefore { get; set; }

    [Display("Maximum results", Description = "Defaults to 100; range 1-10,000. Results follow channel upload order.")]
    public int? MaximumResults { get; set; }
}
