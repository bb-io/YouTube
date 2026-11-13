using System.Text.Json;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Tests.YouTube;
[TestClass]
public sealed class ChannelCommentPollingEventsTests : TestBase
{
    [TestMethod]
    public async Task OnNewCommentOnChannel_FirstPollAndUnchangedPoll_AreSilent()
    {
        var channel = await OwnedChannelId();
        var input = new ChannelCommentPollingInput
        {
            ChannelUrl = "https://www.youtube.com/channel/" + channel,
            IncludeReplies = false
        };
        var events = new ChannelCommentPollingEvents(InvocationContext);
        var first = await events.OnNewCommentOnChannel(new(), input);
        Assert.IsFalse(first.FlyBird);
        Assert.IsNull(first.Result);
        Assert.IsNotNull(first.Memory);
        Assert.AreEqual(channel, first.Memory.ChannelId);
        var second = await events.OnNewCommentOnChannel(new() { Memory = first.Memory }, input);
        Assert.IsFalse(second.FlyBird);
        Assert.IsNull(second.Result);
        CollectionAssert.AreEquivalent(first.Memory.SeenCommentIds, second.Memory!.SeenCommentIds);
    }

    [TestMethod]
    public async Task OnNewCommentOnChannel_ReplyFilterChange_EstablishesNewBaseline()
    {
        var input = new ChannelCommentPollingInput
        {
            ChannelUrl = "https://youtube.com/channel/" + await OwnedChannelId(),
            IncludeReplies = false
        };
        var events = new ChannelCommentPollingEvents(InvocationContext);
        var first = await events.OnNewCommentOnChannel(new(), input);
        var serialized = JsonSerializer.Serialize(first.Memory);
        input.IncludeReplies = true;
        var changed = await events.OnNewCommentOnChannel(new() { Memory = first.Memory }, input);
        Assert.IsFalse(changed.FlyBird);
        Assert.IsNull(changed.Result);
        Assert.IsTrue(changed.Memory!.IncludeReplies);
        Assert.AreEqual(serialized, JsonSerializer.Serialize(first.Memory));
    }

    [TestMethod]
    public async Task OnNewCommentOnChannel_DetectsNewReplyOnBaselinedThreadAndNewTopLevelComment()
    {
        var channelId = await OwnedChannelId();
        var uploaded = await new PublishingActions(InvocationContext, FileManager).UploadVideo(new() { Title = "Blackbird disposable comment test " + Guid.NewGuid().ToString("N"), Description = "Disposable channel comment integration test.", File = new() { Name = "sample.mp4", ContentType = "video/mp4" }, PrivacyStatus = "unlisted", MadeForKids = false, NotifySubscribers = false });
        var api = new AdditionalActions(InvocationContext);
        try
        {
            var parentId = await AddTopLevelComment(api, channelId, uploaded.VideoId);
            var input = new ChannelCommentPollingInput
            {
                ChannelUrl = "https://youtube.com/channel/" + channelId
            };
            var events = new ChannelCommentPollingEvents(InvocationContext);
            PollingEventResponse<ChannelCommentMemory, List<ChannelVideoComment>> baseline = null !;
            for (var attempt = 0; attempt < 15; attempt++)
            {
                baseline = await events.OnNewCommentOnChannel(new(), input);
                if (baseline.Memory!.SeenCommentIds.Contains(parentId))
                    break;
                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            Assert.IsTrue(baseline.Memory!.SeenCommentIds.Contains(parentId), "The baseline must contain the parent thread before a reply is added.");
            var replyResponse = await api.ApiRequest(new() { Url = "https://www.googleapis.com/youtube/v3/comments", Method = "POST", QueryJson = "{\"part\":\"snippet\"}", Body = JsonSerializer.Serialize(new { snippet = new { parentId, textOriginal = "Blackbird disposable reply " + Guid.NewGuid().ToString("N") } }) });
            using var replyJson = JsonDocument.Parse(replyResponse.Body);
            var replyId = replyJson.RootElement.GetProperty("id").GetString()!;
            var withReply = await WaitForComment(events, input, baseline.Memory, replyId);
            var reply = withReply.Result!.Single(x => x.CommentId == replyId);
            Assert.AreEqual(parentId, reply.ParentCommentId);
            Assert.AreEqual(uploaded.VideoId, reply.VideoId);
            Assert.AreEqual(channelId, reply.ChannelId);
            Assert.IsTrue(reply.IsReply);
            Assert.IsTrue(reply.CommentUrl.Contains("lc="));
            Assert.AreEqual(DateTimeKind.Utc, reply.PublishedAt.Kind);
            var newTopId = await AddTopLevelComment(api, channelId, uploaded.VideoId);
            var withTop = await WaitForComment(events, input, withReply.Memory!, newTopId);
            var top = withTop.Result!.Single(x => x.CommentId == newTopId);
            Assert.IsFalse(top.IsReply);
            Assert.AreEqual(uploaded.VideoId, top.VideoId);
            var unchanged = await events.OnNewCommentOnChannel(new() { Memory = withTop.Memory }, input);
            Assert.IsFalse(unchanged.Result?.Any(x => x.CommentId == replyId || x.CommentId == newTopId) ?? false);
        }
        finally
        {
            await api.ApiRequest(new() { Url = "https://www.googleapis.com/youtube/v3/videos", Method = "DELETE", QueryJson = JsonSerializer.Serialize(new { id = uploaded.VideoId }) });
        }
    }

    private static async Task<string> AddTopLevelComment(AdditionalActions api, string channelId, string videoId)
    {
        var response = await api.ApiRequest(new() { Url = "https://www.googleapis.com/youtube/v3/commentThreads", Method = "POST", QueryJson = "{\"part\":\"snippet\"}", Body = JsonSerializer.Serialize(new { snippet = new { channelId, videoId, topLevelComment = new { snippet = new { textOriginal = "Blackbird disposable comment " + Guid.NewGuid().ToString("N") } } } }) });
        using var json = JsonDocument.Parse(response.Body);
        return json.RootElement.GetProperty("snippet").GetProperty("topLevelComment").GetProperty("id").GetString()!;
    }

    private static async Task<PollingEventResponse<ChannelCommentMemory, List<ChannelVideoComment>>> WaitForComment(ChannelCommentPollingEvents events, ChannelCommentPollingInput input, ChannelCommentMemory memory, string commentId)
    {
        for (var attempt = 0; attempt < 15; attempt++)
        {
            var result = await events.OnNewCommentOnChannel(new() { Memory = memory }, input);
            if (result.Result?.Any(x => x.CommentId == commentId) == true)
                return result;
            memory = result.Memory!;
            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        Assert.Fail("The created comment did not become visible in the channel scan within the indexing wait. Check comments, moderation and channel permissions.");
        throw new InvalidOperationException();
    }
}
