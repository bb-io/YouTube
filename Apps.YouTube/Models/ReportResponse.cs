using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class ReportResponse
{
    [Display("Column headers")]
    public IEnumerable<string> ColumnHeaders { get; set; } = [];

    [Display("Rows JSON")]
    public string RowsJson { get; set; } = "[]";

    [Display("Returned count")]
    public int ReturnedCount { get; set; }

    [Display("More results available")]
    public bool MoreResultsAvailable { get; set; }
}
