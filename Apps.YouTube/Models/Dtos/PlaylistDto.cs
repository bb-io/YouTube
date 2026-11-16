using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("etag")]
    public string Etag { get; set; } = "";

    [JsonProperty("snippet")]
    public PlaylistSnippetDto Snippet { get; set; } = new();

    [JsonProperty("status")]
    public PlaylistStatusDto Status { get; set; } = new();

    [JsonProperty("contentDetails")]
    public PlaylistDetailsDto ContentDetails { get; set; } = new();

    [JsonProperty("localizations")]
    public Dictionary<string, LocalizedMetadataDto> Localizations { get; set; } = new();
}
