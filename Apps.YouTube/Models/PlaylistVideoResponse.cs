using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class PlaylistVideoResponse
{
    [Display("Playlist item ID")]
    public string PlaylistItemId { get; set; } = "";

    [Display("Playlist ID")]
    public string PlaylistId { get; set; } = "";

    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Video URL")]
    public string VideoUrl => "https://www.youtube.com/watch?v=" + VideoId;

    [Display("Position")]
    public int? Position { get; set; }

    [Display("Video")]
    public Video? Video { get; set; }
}
