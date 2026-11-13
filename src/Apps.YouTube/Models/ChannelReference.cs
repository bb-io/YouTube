using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;
public sealed record ChannelReference(string Filter, string Value)
{
    public string Key => $"{Filter}:{Value}";
}
