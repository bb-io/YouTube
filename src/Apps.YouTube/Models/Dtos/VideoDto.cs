using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class VideoDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("etag")]
    public string Etag { get; set; } = "";

    [JsonProperty("snippet")]
    public VideoSnippetDto Snippet { get; set; } = new();

    [JsonProperty("status")]
    public VideoStatusDto Status { get; set; } = new();

    [JsonProperty("contentDetails")]
    public VideoDetailsDto ContentDetails { get; set; } = new();

    [JsonProperty("statistics")]
    public VideoStatisticsDto Statistics { get; set; } = new();

    [JsonProperty("localizations")]
    public Dictionary<string, LocalizedMetadataDto> Localizations { get; set; } = new();
}
