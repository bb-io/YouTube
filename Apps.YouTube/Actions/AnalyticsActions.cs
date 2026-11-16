using System.Text.Json;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;

[ActionList("Analytics")]
public sealed class AnalyticsActions(InvocationContext context) : Invocable(context)
{
    [Action("Get lifetime statistics", Description = "Get a snapshot of cumulative channel views, rounded subscribers and public video count. Supply Channel URL or use the OAuth-connected channel.")]
    public Task<LifetimeStatisticsResponse> GetLifetimeStatistics([ActionParameter] LifetimeStatisticsInput input) =>
        YouTubeClient.RunAsync<LifetimeStatisticsResponse>(async () =>
        {
            var query = new Dictionary<string, string> { ["part"] = "snippet,statistics", ["maxResults"] = "50" };
            if (string.IsNullOrWhiteSpace(input.ChannelUrl))
            {
                if (!Client.HasOAuth)
                    throw new PluginMisconfigurationException("Supply Channel URL for an API-key connection, or use OAuth to read the connected channel.");
                query["mine"] = "true";
            }
            else
            {
                var channel = YouTubeUrls.ParseChannel(input.ChannelUrl);
                query[channel.Filter] = channel.Value;
            }
            var channels = await Client.Paginate<ChannelStatisticsResourceDto>("channels", query, 2);
            if (channels.Count != 1)
                throw new PluginMisconfigurationException("No single channel was found. Check Channel URL or reconnect using the intended YouTube or Brand Account.");
            var current = channels[0];
            return new LifetimeStatisticsResponse
            {
                ChannelId = current.Id,
                ChannelUrl = "https://www.youtube.com/channel/" + current.Id,
                ChannelTitle = current.Snippet.Title,
                ViewCount = current.Statistics.ViewCount,
                SubscriberCount = current.Statistics.HiddenSubscriberCount ? null : current.Statistics.SubscriberCount,
                SubscriberCountHidden = current.Statistics.HiddenSubscriberCount,
                PublicVideoCount = current.Statistics.VideoCount,
                RetrievedAt = DateTime.UtcNow
            };
        });

    [Action("Get time-bound metrics", Description = "Get date-range views, watch time, engagement and subscriber changes as directly mappable fields. Omit Video ID for channel totals. Requires the owning channel's Analytics OAuth access.")]
    public Task<TimeBoundMetricsResponse> GetTimeBoundMetrics([ActionParameter] TimeBoundMetricsInput input) =>
        YouTubeClient.RunAsync<TimeBoundMetricsResponse>(async () =>
        {
            Client.RequireOAuth();
            if (input.StartDate == default || input.EndDate == default || input.StartDate.Date > input.EndDate.Date)
                throw new PluginMisconfigurationException("Supply Start date and End date, with Start date on or before End date. Dates use YouTube's Pacific reporting calendar.");
            var videoId = string.IsNullOrWhiteSpace(input.VideoId) ? null : YouTubeUrls.ParseVideoId(input.VideoId);
            var channels = await Client.Paginate<PickerItem>("channels", new()
            {
                ["part"] = "id",
                ["mine"] = "true",
                ["maxResults"] = "50"
            }, 2);
            if (channels.Count != 1)
                throw new PluginMisconfigurationException("Reconnect using the intended YouTube or Brand Account to read one channel's Analytics reports.");
            var channelId = channels[0].Id;
            if (videoId != null)
            {
                var video = await ReadVideo(videoId);
                if (video.Snippet.ChannelId != channelId)
                    throw new PluginMisconfigurationException("Video ID must belong to the channel selected by this OAuth connection. Reconnect using the video's owning channel or choose another video.");
            }
            var report = await new PublishingActions(Client).GetReportAsync(new ReportInput
            {
                StartDate = input.StartDate.Date,
                EndDate = input.EndDate.Date,
                ChannelId = channelId,
                Metrics = ["views", "engagedViews", "estimatedMinutesWatched", "averageViewDuration", "averageViewPercentage", "likes", "comments", "shares", "subscribersGained", "subscribersLost"],
                Filters = videoId == null ? null : "video==" + videoId,
                MaximumRows = 1
            });
            using var document = JsonDocument.Parse(report.RowsJson);
            var hasData = document.RootElement.GetArrayLength() > 0;
            var values = hasData
                ? report.ColumnHeaders.Zip(document.RootElement[0].EnumerateArray(), (name, value) => (name, value)).ToDictionary(item => item.name, item => item.value, StringComparer.Ordinal)
                : new Dictionary<string, JsonElement>();
            return new TimeBoundMetricsResponse
            {
                ChannelId = channelId,
                VideoId = videoId,
                StartDate = input.StartDate.Date,
                EndDate = input.EndDate.Date,
                HasData = hasData,
                Views = Count(values, "views"),
                EngagedViews = Count(values, "engagedViews"),
                EstimatedMinutesWatched = Number(values, "estimatedMinutesWatched"),
                AverageViewDurationSeconds = Number(values, "averageViewDuration"),
                AverageViewPercentage = Number(values, "averageViewPercentage"),
                Likes = Count(values, "likes"),
                Comments = Count(values, "comments"),
                Shares = Count(values, "shares"),
                SubscribersGained = Count(values, "subscribersGained"),
                SubscribersLost = Count(values, "subscribersLost"),
                RetrievedAt = DateTime.UtcNow
            };
        });

    [Action("Get video statistics by ID", Description = "Get a snapshot of cumulative views, likes and comments for one video. Video ID supports an OAuth dropdown, direct ID, URL or a mapped value. Private videos require authorized OAuth access.")]
    public Task<VideoStatisticsResponse> GetVideoStatisticsById([ActionParameter] VideoInput input) =>
        YouTubeClient.RunAsync<VideoStatisticsResponse>(async () =>
        {
            var id = YouTubeUrls.ParseVideoId(input.VideoId);
            var video = await ReadVideo(id);
            return new VideoStatisticsResponse
            {
                VideoId = video.Id,
                VideoUrl = "https://www.youtube.com/watch?v=" + video.Id,
                Title = video.Snippet.Title,
                ChannelId = video.Snippet.ChannelId,
                PrivacyStatus = video.Status.PrivacyStatus,
                ViewCount = video.Statistics.ViewCount,
                LikeCount = video.Statistics.LikeCount,
                CommentCount = video.Statistics.CommentCount,
                RetrievedAt = DateTime.UtcNow
            };
        });

    private async Task<VideoDto> ReadVideo(string id)
    {
        var videos = await Client.GetAsync<YouTubePage<VideoDto>>("videos", new()
        {
            ["part"] = "snippet,status,statistics",
            ["id"] = id
        });
        return videos.Items.SingleOrDefault() ?? throw new PluginMisconfigurationException("Video not found or not accessible. Check Video ID and use the owning channel's OAuth connection for private content.");
    }

    private static long? Count(Dictionary<string, JsonElement> values, string metric) =>
        values.TryGetValue(metric, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt64() : null;

    private static double? Number(Dictionary<string, JsonElement> values, string metric) =>
        values.TryGetValue(metric, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
}
