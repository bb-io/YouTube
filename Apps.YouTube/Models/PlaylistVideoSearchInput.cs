using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistVideoSearchInput
{
    [Display("Playlist ID")]
    [DataSource(typeof(PlaylistDataHandler))]
    public string PlaylistId { get; set; } = "";

    [Display("Search term", Description = "Case-insensitive title or description match.")]
    public string? SearchTerm { get; set; }

    [Display("Video visibility", Description = "Defaults to public. Other visibility modes require OAuth.")]
    [StaticDataSource(typeof(VideoVisibilityDataSource))]
    public string? VideoVisibility { get; set; }

    [Display("Maximum results", Description = "Defaults to 100; range 1-10,000. Playlist order and repeated video entries are preserved.")]
    public int? MaximumResults { get; set; }
}
