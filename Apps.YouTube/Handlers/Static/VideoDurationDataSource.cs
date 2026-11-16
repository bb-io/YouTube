using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class VideoDurationDataSource : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() => [new("any", "Any"), new("short", "Under 4 minutes"), new("medium", "4–20 minutes"), new("long", "Over 20 minutes")];
}
