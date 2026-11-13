using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class CaptionFormatDataSource : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() => [new("srt", "SRT"), new("vtt", "WebVTT"), new("ttml", "TTML"), new("sbv", "SBV"), new("scc", "SCC")];
}
