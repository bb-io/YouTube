using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistSearchInput
{
    [Display("Channel URL", Description = "Omit with OAuth to search the connected account's playlists. OAuth suggestions show the selected channel. With an API key, enter or map a public channel URL.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(ChannelUrlDataHandler))]
    public string? ChannelUrl { get; set; }

    [Display("Search term", Description = "Case-insensitive title or description match.")]
    public string? SearchTerm { get; set; }

    [Display("Maximum results", Description = "Defaults to 100; range 1-10,000.")]
    public int? MaximumResults { get; set; }
}
