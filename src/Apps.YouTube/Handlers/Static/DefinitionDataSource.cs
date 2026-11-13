using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class DefinitionDataSource : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() => [new("any", "Any"), new("high", "HD"), new("standard", "SD")];
}
