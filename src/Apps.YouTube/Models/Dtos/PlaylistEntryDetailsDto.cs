using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistEntryDetailsDto
{
    [JsonProperty("videoId")]
    public string VideoId { get; set; } = "";
}
