using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class SearchPlaylistsResponse
{
    [Display("Playlists")]
    public List<PlaylistResponse> Playlists { get; set; } = [];

    [Display("Playlist IDs")]
    public IEnumerable<string> PlaylistIds => Playlists.Select(x => x.PlaylistId);

    [Display("Returned count")]
    public int ReturnedCount => Playlists.Count;
}
