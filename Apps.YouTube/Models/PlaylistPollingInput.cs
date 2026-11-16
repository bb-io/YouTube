using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class PlaylistPollingInput
{
    [Display("Playlist ID")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(PlaylistDataHandler))]
    public string PlaylistId { get; set; } = "";

    [Display("Video visibility", Description = "Public only by default. Other modes require the playlist owner's OAuth connection.")]
    [StaticDataSource(typeof(VideoVisibilityDataSource))]
    public string? VideoVisibility { get; set; }
}
