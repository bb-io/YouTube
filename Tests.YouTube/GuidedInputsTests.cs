namespace Tests.YouTube;

[TestClass]
public sealed class GuidedInputsTests : TestBase
{
    [TestMethod]
    public async Task ChannelPickers_ReadSelectedOAuthIdentityWithCorrectValueTypes()
    {
        var id = await OwnedChannelId();
        var ids = await new ChannelIdDataHandler(InvocationContext, new()).GetDataAsync(new(), CancellationToken.None);
        var urls = await new ChannelUrlDataHandler(InvocationContext).GetDataAsync(new(), CancellationToken.None);
        Assert.IsTrue(ids.Any(item => item.Value == id && item.DisplayName.Contains(id)));
        Assert.IsTrue(urls.Any(item => item.Value == "https://www.youtube.com/channel/" + id));
    }

    [TestMethod]
    public async Task CaptionLanguagePicker_ReadsLanguagesPresentOnVideo()
    {
        var caption = await OwnedCaption();
        var values = (await new CaptionLanguageDataHandler(InvocationContext, new() { VideoId = caption.VideoId })
            .GetDataAsync(new(), CancellationToken.None)).ToList();
        Assert.IsTrue(values.Any(item => item.Value.Equals(caption.Language, StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(values.Count, values.Select(item => item.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [TestMethod]
    public async Task GuidedReportCatalog_RunsSelectedMonthlyPerformanceReport()
    {
        var context = new ReportContext();
        var options = (await new ReportMetricDataHandler(InvocationContext, context).GetDataAsync(new(), CancellationToken.None)).ToList();
        var metrics = new[] { "views", "estimatedMinutesWatched", "averageViewDuration" };
        Assert.IsTrue(metrics.All(value => options.Any(item => item.Value == value)));
        Assert.IsFalse(options.Any(item => item.Value.Contains(',')));
        context.Metrics = metrics;
        var dimensions = await new ReportDimensionDataHandler(InvocationContext, context).GetDataAsync(new(), CancellationToken.None);
        Assert.IsTrue(dimensions.Any(item => item.Value == "month"));
        context.Dimensions = "month";
        var sort = await new ReportSortDataHandler(InvocationContext, context).GetDataAsync(new(), CancellationToken.None);
        Assert.IsTrue(sort.Any(item => item.Value == "month"));
        var result = await new AdditionalActions(InvocationContext).GetReport(new()
        {
            StartDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-2),
            EndDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1),
            Metrics = metrics, Dimensions = "month", Sort = "month", MaximumRows = 50
        });
        Assert.IsNotNull(result.RowsJson);
        Assert.IsTrue(metrics.All(value => result.ColumnHeaders.Contains(value)));
        Assert.IsTrue(result.ReturnedCount <= 50);
    }

    [TestMethod]
    public async Task GuidedCountryPicker_RunsCompatibleChannelReport()
    {
        var countries = await new ReportCountryDataHandler(InvocationContext, new() { Metrics = ["views"] })
            .GetDataAsync(new() { SearchString = "US" }, CancellationToken.None);
        Assert.IsTrue(countries.Any(item => item.Value == "US"));
        var result = await new AdditionalActions(InvocationContext).GetReport(new()
        {
            StartDate = DateTime.UtcNow.Date.AddDays(-14), EndDate = DateTime.UtcNow.Date.AddDays(-3),
            Metrics = ["views"], CountryCode = "US", MaximumRows = 50
        });
        Assert.IsNotNull(result.RowsJson);
    }

    [TestMethod]
    public async Task GuidedVideoFilter_RunsOwnedVideoReport()
    {
        var result = await new AdditionalActions(InvocationContext).GetReport(new()
        {
            StartDate = DateTime.UtcNow.Date.AddDays(-14), EndDate = DateTime.UtcNow.Date.AddDays(-3),
            Metrics = ["views"], VideoId = (await OwnedVideo()).VideoId, MaximumRows = 50
        });
        Assert.IsNotNull(result.RowsJson);
    }
}
