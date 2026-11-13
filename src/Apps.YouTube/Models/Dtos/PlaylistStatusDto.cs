using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistStatusDto
{
    [JsonProperty("privacyStatus")]
    public string PrivacyStatus { get; set; } = "";

    [JsonProperty("podcastStatus")]
    public string? PodcastStatus { get; set; }
}
