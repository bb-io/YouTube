using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistVideosResponse
{
    [Display("Videos")]
    public List<PlaylistVideoResponse> Videos { get; set; } = [];

    [Display("Playlist item IDs")]
    public IEnumerable<string> PlaylistItemIds => Videos.Select(x => x.PlaylistItemId);

    [Display("Video IDs")]
    public IEnumerable<string> VideoIds => Videos.Select(x => x.VideoId);

    [Display("Returned count")]
    public int ReturnedCount => Videos.Count;
}
