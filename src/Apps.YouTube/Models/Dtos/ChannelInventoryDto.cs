using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ChannelInventoryDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("contentDetails")]
    public ChannelDetailsDto ContentDetails { get; set; } = new();
}
