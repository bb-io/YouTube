namespace Tests.YouTube;
[TestClass]
public sealed class AdditionalActionsTests : TestBase
{
    [TestMethod]
    public async Task FindVideo_FindsPublicVideo()
    {
        var result = await new AdditionalActions(InvocationContext).FindVideo(new() { SearchTerm = "Blackbird.io" });
        Assert.IsTrue(result.Found);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.VideoId));
    }

    [TestMethod]
    public async Task AddVideoToPlaylist_InsertsOwnedVideo()
    {
        var playlist = await OwnedPlaylist();
        var video = await OwnedVideo();
        var result = await new AdditionalActions(InvocationContext).AddVideoToPlaylist(new() { PlaylistId = playlist, VideoId = video.VideoId });
        Assert.AreEqual(playlist, result.PlaylistId);
        Assert.AreEqual(video.VideoId, result.VideoId);
    }

    [TestMethod]
    public async Task GetReport_ReadsChannelAnalytics()
    {
        var result = await new AdditionalActions(InvocationContext).GetReport(new() { StartDate = DateTime.UtcNow.Date.AddDays(-30), EndDate = DateTime.UtcNow.Date.AddDays(-3), Metrics = ["views"] });
        Assert.IsNotNull(result.RowsJson);
        Assert.IsTrue(result.ColumnHeaders.Contains("views"));
    }

    [TestMethod]
    public async Task ApiRequest_ReadsOwnedChannel()
    {
        var result = await new AdditionalActions(InvocationContext).ApiRequest(new() { Url = "https://www.googleapis.com/youtube/v3/channels", QueryJson = "{\"part\":\"id\",\"mine\":\"true\"}" });
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(result.Body.Contains("items"));
    }
}
