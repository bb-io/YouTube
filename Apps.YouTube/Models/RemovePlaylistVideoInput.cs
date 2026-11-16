using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class RemovePlaylistVideoInput
{
    [Display("Playlist item ID", Description = "Entry ID from Search playlist videos or Add video to playlist. This is different from Video ID.")]
    [DataSource(typeof(PlaylistItemDataHandler))]
    public string PlaylistItemId { get; set; } = "";

    [Display("Playlist ID", Description = "Optional. Populates playlist-entry suggestions and verifies that the entry belongs to this playlist before removal.")]
    [DataSource(typeof(PlaylistDataHandler))]
    public string? PlaylistId { get; set; }
}
