using Blackbird.Applications.Sdk.Common.Polling;

namespace Tests.YouTube;
[TestClass]
public sealed class PollingEventsTests : TestBase
{
    [TestMethod]
    public async Task OnNewVideoAdded_OwnerAllVisibility_BaselinesSilently()
    {
        var id = await OwnedChannelId();
        var events = new VideoPollingEvents(InvocationContext);
        var input = new ChannelInput
        {
            ChannelUrl = "https://youtube.com/channel/" + id,
            VideoVisibility = "all"
        };
        var first = await events.OnNewVideoAdded(new PollingEventRequest<PollingMemory>(), input);
        Assert.IsFalse(first.FlyBird);
        Assert.IsNotNull(first.Memory);
        var second = await events.OnNewVideoAdded(new PollingEventRequest<PollingMemory> { Memory = first.Memory }, input);
        Assert.IsFalse(second.FlyBird);
    }

    [TestMethod]
    public async Task OnNewVideoMatchingSearch_BaselinesSilently()
    {
        var result = await new AdditionalPollingEvents(InvocationContext).OnNewVideoBySearch(new(), new() { SearchTerm = "Blackbird.io", MaximumResults = 5 });
        Assert.IsFalse(result.FlyBird);
        Assert.IsNotNull(result.Memory);
    }

    [TestMethod]
    public async Task OnNewVideoInPlaylist_BaselinesSilently()
    {
        var result = await new AdditionalPollingEvents(InvocationContext).OnNewPlaylistVideo(new(), new() { PlaylistId = await OwnedPlaylist(), VideoVisibility = "all" });
        Assert.IsFalse(result.FlyBird);
        Assert.IsNotNull(result.Memory);
    }

    [TestMethod]
    public async Task OnNewLiveStream_BaselinesOwnedChannelSilently()
    {
        var result = await new AdditionalPollingEvents(InvocationContext).OnNewLiveStream(new(), new() { Channel = await OwnedChannelId(), OwnChannel = true });
        Assert.IsFalse(result.FlyBird);
        Assert.IsNotNull(result.Memory);
    }

    [TestMethod]
    public async Task OnNewComment_BaselinesAccessibleCommentsSilently()
    {
        var videos = await new VideoActions(InvocationContext).SearchVideos(new() { SearchTerm = "Blackbird.io", MaximumResults = 10 });
        foreach (var video in videos.Videos)
        {
            try
            {
                var result = await new AdditionalPollingEvents(InvocationContext).OnNewComment(new(), new() { VideoId = video.VideoId, IncludeReplies = false });
                Assert.IsFalse(result.FlyBird);
                Assert.IsNotNull(result.Memory);
                return;
            }
            catch (Blackbird.Applications.Sdk.Common.Exceptions.PluginMisconfigurationException ex)when (ex.Message.Contains("Comments are disabled", StringComparison.OrdinalIgnoreCase))
            {
            }
        }

        Assert.Fail("No search result with accessible comments was available. Use a test channel with comments enabled.");
    }
}
