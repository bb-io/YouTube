using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PickerItem
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("snippet")]
    public PickerSnippet Snippet { get; set; } = new();
}
