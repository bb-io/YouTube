using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class VideoSortDataSource : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() => [new("relevance", "Relevance"), new("rating", "Rating"), new("date", "Newest first"), new("title", "Title"), new("viewCount", "View count")];
}
