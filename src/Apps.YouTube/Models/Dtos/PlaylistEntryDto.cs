using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistEntryDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("snippet")]
    public PlaylistEntrySnippetDto Snippet { get; set; } = new();

    [JsonProperty("contentDetails")]
    public PlaylistEntryDetailsDto ContentDetails { get; set; } = new();
}
