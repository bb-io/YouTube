using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class LocalizedMetadataDto
{
    [JsonProperty("title")]
    public string Title { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";
}
