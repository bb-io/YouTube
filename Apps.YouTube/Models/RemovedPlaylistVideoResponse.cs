using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class RemovedPlaylistVideoResponse
{
    [Display("Playlist item ID")]
    public string PlaylistItemId { get; set; } = "";

    [Display("Playlist ID")]
    public string PlaylistId { get; set; } = "";

    [Display("Removed")]
    public bool Removed { get; set; }
}
