using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class LicenseDataSource : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() => [new("any", "Any"), new("creativeCommon", "Creative Commons"), new("youtube", "YouTube standard")];
}
