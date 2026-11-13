namespace Tests.YouTube;

[TestClass]
public sealed class AnalyticsActionsTests : TestBase
{
    [TestMethod]
    public async Task GetLifetimeStatistics_ReadsConnectedChannelCounters()
    {
        var result = await new AnalyticsActions(InvocationContext).GetLifetimeStatistics(new());
        Assert.AreEqual(await OwnedChannelId(), result.ChannelId);
        Assert.AreEqual("https://www.youtube.com/channel/" + result.ChannelId, result.ChannelUrl);
        Assert.AreEqual(DateTimeKind.Utc, result.RetrievedAt.Kind);
        if (result.SubscriberCountHidden)
            Assert.IsNull(result.SubscriberCount);
        if (result.PublicVideoCount.HasValue)
            Assert.IsTrue(result.PublicVideoCount.Value >= 0);
    }

    [TestMethod]
    public async Task GetVideoStatisticsById_ReadsOwnedVideoCounters()
    {
        var id = (await OwnedVideo()).VideoId;
        var result = await new AnalyticsActions(InvocationContext).GetVideoStatisticsById(new() { VideoId = id });
        Assert.AreEqual(id, result.VideoId);
        Assert.AreEqual(await OwnedChannelId(), result.ChannelId);
        Assert.IsTrue(result.VideoUrl.EndsWith(id));
        Assert.AreEqual(DateTimeKind.Utc, result.RetrievedAt.Kind);
    }

    [TestMethod]
    public async Task GetTimeBoundMetrics_ReadsConnectedChannelSummary()
    {
        var start = DateTime.UtcNow.Date.AddDays(-14);
        var end = DateTime.UtcNow.Date.AddDays(-3);
        var result = await new AnalyticsActions(InvocationContext).GetTimeBoundMetrics(new() { StartDate = start, EndDate = end });
        Assert.AreEqual(await OwnedChannelId(), result.ChannelId);
        Assert.IsNull(result.VideoId);
        Assert.AreEqual(start, result.StartDate);
        Assert.AreEqual(end, result.EndDate);
        if (!result.HasData)
        {
            Assert.IsNull(result.Views);
            Assert.IsNull(result.EstimatedMinutesWatched);
        }
    }

    [TestMethod]
    public async Task GetTimeBoundMetrics_FiltersOwnedVideo()
    {
        var id = (await OwnedVideo()).VideoId;
        var result = await new AnalyticsActions(InvocationContext).GetTimeBoundMetrics(new()
        {
            StartDate = DateTime.UtcNow.Date.AddDays(-14),
            EndDate = DateTime.UtcNow.Date.AddDays(-3),
            VideoId = id
        });
        Assert.AreEqual(id, result.VideoId);
        Assert.AreEqual(await OwnedChannelId(), result.ChannelId);
        Assert.AreEqual(DateTimeKind.Utc, result.RetrievedAt.Kind);
    }
}
