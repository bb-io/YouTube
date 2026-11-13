using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;

public sealed class ReportDimensionDataHandler(InvocationContext context, [ActionParameter] ReportContext input) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) =>
        YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(() => Task.FromResult(ReportRules.DimensionOptions(input, context.SearchString)));
}
