using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;
public sealed class VideoVisibilityDataSource : Blackbird.Applications.Sdk.Common.Dictionaries.IStaticDataSourceItemHandler
{
    public IEnumerable<Blackbird.Applications.Sdk.Common.Dynamic.DataSourceItem> GetData() => [new("public", "Public only"), new("unlisted", "Unlisted only"), new("private", "Private only"), new("all", "All accessible videos")];
}
