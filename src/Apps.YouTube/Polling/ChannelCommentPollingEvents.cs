using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using RestSharp;

namespace Apps.YouTube;
[PollingEventList("Channel comments")]
public sealed class ChannelCommentPollingEvents(InvocationContext context) : Invocable(context)
{
    private const int MaximumEvents = 100;
    private const int MaximumRequests = 2000;
    [PollingEvent("On new comment on channel", "Start a workflow when a new published comment appears under any video in the selected channel, optionally including replies on older discussions.")]
    [MultipleEvents]
    public Task<PollingEventResponse<ChannelCommentMemory, List<ChannelVideoComment>>> OnNewCommentOnChannel(PollingEventRequest<ChannelCommentMemory> request, [PollingEventParameter] ChannelCommentPollingInput input) => YouTubeClient.RunAsync<PollingEventResponse<ChannelCommentMemory, List<ChannelVideoComment>>>(async () =>
    {
        var reference = YouTubeUrls.ParseChannel(input.ChannelUrl);
        var includeReplies = input.IncludeReplies ?? true;
        var remainingRequests = MaximumRequests;
        void CountRequest()
        {
            if (remainingRequests-- <= 0)
                throw new PluginApplicationException("This channel's comment scan exceeded 2,000 logical page reads (read retries may add requests). Polling memory was not advanced. Use video-level monitoring for a smaller source or contact support to design a larger-channel scan.");
        }

        CountRequest();
        var channelRequest = new RestRequest("channels").AddQueryParameter("part", "id").AddQueryParameter(reference.Filter, reference.Value);
        var channels = await Client.ExecuteWithErrorHandling<YouTubePage<ChannelCommentChannelDto>>(channelRequest);
        var channel = channels.Items.SingleOrDefault() ?? throw new PluginMisconfigurationException("Channel not found. Check Channel URL and use its current @handle or /channel/UC... URL.");
        var previous = request.Memory;
        var sameChannel = previous?.ChannelId == channel.Id;
        var baseline = !sameChannel || previous!.IncludeReplies != includeReplies;
        var seen = new HashSet<string>(sameChannel ? previous!.SeenCommentIds : [], StringComparer.Ordinal);
        var pendingIds = new HashSet<string>(StringComparer.Ordinal);
        var candidates = new SortedSet<ChannelVideoComment>(Comparer<ChannelVideoComment>.Create((left, right) =>
        {
            var time = left.PublishedAt.CompareTo(right.PublishedAt);
            return time != 0 ? time : StringComparer.Ordinal.Compare(left.CommentId, right.CommentId);
        }));
        void Observe(ChannelCommentDto comment, string videoId)
        {
            if (!string.IsNullOrEmpty(comment.Snippet.ModerationStatus) && comment.Snippet.ModerationStatus != "published")
                return;
            if (baseline)
            {
                seen.Add(comment.Id);
                return;
            }

            if (seen.Contains(comment.Id) || !pendingIds.Add(comment.Id))
                return;
            candidates.Add(Map(comment, channel.Id, videoId));
            if (candidates.Count <= MaximumEvents)
                return;
            var newest = candidates.Max!;
            candidates.Remove(newest);
            pendingIds.Remove(newest.CommentId);
        }

        var threadRequest = new RestRequest("commentThreads").AddQueryParameter("part", "snippet").AddQueryParameter("allThreadsRelatedToChannelId", channel.Id).AddQueryParameter("order", "time").AddQueryParameter("maxResults", "100").AddQueryParameter("textFormat", "plainText");
        await Client.Paginate<ChannelCommentThreadDto>(threadRequest, continueAfterPage: async threads =>
        {
            foreach (var thread in threads)
            {
                if (string.IsNullOrEmpty(thread.Snippet.VideoId))
                    continue;
                Observe(thread.Snippet.TopLevelComment, thread.Snippet.VideoId);
                if (!includeReplies || thread.Snippet.TotalReplyCount == 0)
                    continue;
                var repliesRequest = new RestRequest("comments").AddQueryParameter("part", "snippet").AddQueryParameter("parentId", thread.Snippet.TopLevelComment.Id).AddQueryParameter("maxResults", "100").AddQueryParameter("textFormat", "plainText");
                await Client.Paginate<ChannelCommentDto>(repliesRequest, continueAfterPage: replies =>
                {
                    foreach (var reply in replies)
                        Observe(reply, thread.Snippet.VideoId);
                    return Task.FromResult(true);
                }, beforePage: CountRequest, collectItems: false);
            }

            return true;
        }, beforePage: CountRequest, collectItems: false);
        var result = candidates.ToList();
        seen.UnionWith(result.Select(x => x.CommentId));
        return new PollingEventResponse<ChannelCommentMemory, List<ChannelVideoComment>>
        {
            FlyBird = result.Count > 0,
            Result = result.Count == 0 ? null : result,
            Memory = new ChannelCommentMemory
            {
                ChannelId = channel.Id,
                IncludeReplies = includeReplies,
                SeenCommentIds = seen.Order(StringComparer.Ordinal).ToList()
            }
        };
    });
    private static ChannelVideoComment Map(ChannelCommentDto comment, string channelId, string videoId) => new()
    {
        ChannelId = channelId,
        VideoId = videoId,
        CommentId = comment.Id,
        ParentCommentId = comment.Snippet.ParentId,
        Author = comment.Snippet.AuthorDisplayName,
        Text = string.IsNullOrEmpty(comment.Snippet.TextOriginal) ? comment.Snippet.TextDisplay : comment.Snippet.TextOriginal,
        PublishedAt = comment.Snippet.PublishedAt.ToUniversalTime(),
        UpdatedAt = comment.Snippet.UpdatedAt?.ToUniversalTime(),
        LikeCount = comment.Snippet.LikeCount
    };
}
