using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistResponse
{
    [Display("Playlist ID")]
    public string PlaylistId { get; set; } = "";

    [Display("Playlist URL")]
    public string PlaylistUrl => "https://www.youtube.com/playlist?list=" + PlaylistId;

    [Display("Title")]
    public string Title { get; set; } = "";

    [Display("Description")]
    public string Description { get; set; } = "";

    [Display("Channel ID")]
    public string ChannelId { get; set; } = "";

    [Display("Privacy status")]
    public string PrivacyStatus { get; set; } = "";

    [Display("Default language")]
    public string DefaultLanguage { get; set; } = "";

    [Display("Podcast status")]
    public string? PodcastStatus { get; set; }

    [Display("Published at")]
    public DateTime PublishedAt { get; set; }

    [Display("Video count")]
    public long VideoCount { get; set; }

    [Display("Localizations")]
    public List<VideoLocalization> Localizations { get; set; } = [];
}
