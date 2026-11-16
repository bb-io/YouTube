using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistEntrySnippetDto
{
    [JsonProperty("playlistId")]
    public string PlaylistId { get; set; } = "";

    [JsonProperty("position")]
    public int? Position { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; } = "";
}
