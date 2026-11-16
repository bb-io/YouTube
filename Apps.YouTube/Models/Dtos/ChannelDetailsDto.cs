using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ChannelDetailsDto
{
    [JsonProperty("relatedPlaylists")]
    public RelatedPlaylistsDto RelatedPlaylists { get; set; } = new();
}
