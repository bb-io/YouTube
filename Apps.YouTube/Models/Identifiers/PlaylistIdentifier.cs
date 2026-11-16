using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistIdentifier
{
    [Display("Playlist ID", Description = "Playlist ID or supported playlist URL. Suggestions show owned playlists.")]
    [DataSource(typeof(PlaylistDataHandler))]
    public string PlaylistId { get; set; } = "";
}
