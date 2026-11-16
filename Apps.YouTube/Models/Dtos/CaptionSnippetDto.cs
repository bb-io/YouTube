using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class CaptionSnippetDto
{
    [JsonProperty("videoId")]
    public string VideoId { get; set; } = "";

    [JsonProperty("language")]
    public string Language { get; set; } = "";

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("trackKind")]
    public string TrackKind { get; set; } = "";

    [JsonProperty("status")]
    public string Status { get; set; } = "";

    [JsonProperty("failureReason")]
    public string FailureReason { get; set; } = "";

    [JsonProperty("isDraft")]
    public bool IsDraft { get; set; }

    [JsonProperty("lastUpdated")]
    public DateTime? LastUpdated { get; set; }
}
