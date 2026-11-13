using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class DiscoveryMemory
{
    public string Key { get; set; } = "";
    public List<string> SeenIds { get; set; } = [];
    public DateTime? StartedAt { get; set; }
}
