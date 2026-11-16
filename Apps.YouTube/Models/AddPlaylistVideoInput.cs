using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class AddPlaylistVideoInput
{
    [Display("Playlist ID", Description = "Playlist ID or a YouTube playlist URL.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(PlaylistDataHandler))]
    public string PlaylistId { get; set; } = "";

    [Display("Video ID", Description = "With OAuth, choose a video from the connected channel, or enter or map Video ID directly.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = "";

    [Display("Position", Description = "Optional zero-based position. The playlist must use manual ordering. Omit to use YouTube's default placement; adding an existing video may create another distinct entry.")]
    public int? Position { get; set; }

    [Display("Note", Description = "Optional note, at most 280 characters.")]
    public string? Note { get; set; }
}
