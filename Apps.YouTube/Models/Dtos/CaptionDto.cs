using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class CaptionDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("snippet")]
    public CaptionSnippetDto Snippet { get; set; } = new();
}
