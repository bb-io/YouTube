using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class VideoCaptionDataSource : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() => [new("any", "Any"), new("closedCaption", "Has captions"), new("none", "Has no captions")];
}
