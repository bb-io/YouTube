namespace Tests.YouTube;
[TestClass]
public sealed class VideoActionsTests : TestBase
{
    [TestMethod]
    public async Task SearchVideos_FindsPublicVideos()
    {
        var result = await new VideoActions(InvocationContext).SearchVideos(new() { SearchTerm = "Blackbird.io", MaximumResults = 5 });
        Assert.IsTrue(result.Videos.Count > 0);
        Assert.AreEqual(result.Videos.Count, result.Videos.Select(v => v.VideoId).Distinct().Count());
    }

    [TestMethod]
    public async Task GetVideo_UsesSearchResult()
    {
        var actions = new VideoActions(InvocationContext);
        var search = await actions.SearchVideos(new() { SearchTerm = "Blackbird.io", MaximumResults = 1 });
        var result = await actions.GetVideoUrl(new() { VideoId = search.Videos.First().VideoId });
        Assert.AreEqual(search.Videos.First().VideoId, result.VideoId);
        Assert.IsTrue(result.VideoUrl.StartsWith("https://www.youtube.com/watch?v="));
    }
}
