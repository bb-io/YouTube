using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class YouTubePage<T>
{
    [JsonProperty("items")]
    public List<T> Items { get; set; } = [];

    [JsonProperty("nextPageToken")]
    public string? NextPageToken { get; set; }
}
