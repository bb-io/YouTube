using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class ReportTypeDataSource : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() => [new("channel", "Channel"), new("contentOwner", "Content owner")];
}
