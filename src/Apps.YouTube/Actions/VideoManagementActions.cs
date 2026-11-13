using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.YouTube;
[ActionList("Video management")]
public sealed class VideoManagementActions(InvocationContext context) : Invocable(context)
{
    [Action("Update video", Description = "Update source metadata while preserving omitted fields, existing translations and publication settings. Requires OAuth.")]
    public Task<ManagedVideoResponse> UpdateVideo([ActionParameter] UpdateVideoInput input) => YouTubeClient.RunAsync<ManagedVideoResponse>(async () =>
    {
        Client.RequireOAuth();
        var id = YouTubeUrls.ParseVideoId(input.VideoId);
        if (input.Title == null && input.Description == null && input.Tags == null && input.ClearTags != true && input.CategoryId == null && input.DefaultLanguage == null && input.DefaultAudioLanguage == null)
            throw new PluginMisconfigurationException("Supply at least one metadata field to update. Region code only controls category suggestions.");
        if (input.ClearTags == true && input.Tags != null)
            throw new PluginMisconfigurationException("Use Tags or Clear tags, not both.");
        var current = await ReadVideo(id);
        var snippet = EditableSnippet(current.Snippet);
        var title = input.Title ?? current.Snippet.Title;
        var description = input.Description ?? current.Snippet.Description;
        InputValidator.VideoText(title, description);
        snippet["title"] = title;
        snippet["description"] = description;
        if (input.CategoryId != null)
        {
            InputValidator.Category(input.CategoryId);
            snippet["categoryId"] = input.CategoryId;
        }

        if (input.Tags != null)
        {
            var tags = input.Tags.ToList();
            if (tags.Any(string.IsNullOrWhiteSpace))
                throw new PluginMisconfigurationException("Tags cannot contain empty values. Use Clear tags to remove all tags.");
            snippet["tags"] = tags;
        }

        if (input.ClearTags == true)
            snippet["tags"] = Array.Empty<string>();
        if (input.DefaultLanguage != null)
            snippet["defaultLanguage"] = LocalizationActions.LanguageTag(input.DefaultLanguage);
        if (input.DefaultAudioLanguage != null)
            snippet["defaultAudioLanguage"] = LocalizationActions.LanguageTag(input.DefaultAudioLanguage);
        var request = new RestRequest("videos", Method.Put).AddQueryParameter("part", "snippet").AddJsonBody(new Dictionary<string, object> { ["id"] = id, ["snippet"] = snippet });
        if (!string.IsNullOrEmpty(current.Etag))
            request.AddHeader("If-Match", current.Etag);
        await Client.ExecuteWithErrorHandling(request);
        return Map(await ReadVideo(id));
    });
    [Action("Set video publication settings", Description = "Change visibility, schedule publication or update audience and distribution settings for an existing video. Omitted settings are preserved. Requires OAuth.")]
    public Task<ManagedVideoResponse> SetVideoPublicationSettings([ActionParameter] VideoPublicationInput input) => YouTubeClient.RunAsync<ManagedVideoResponse>(async () =>
    {
        Client.RequireOAuth();
        var id = YouTubeUrls.ParseVideoId(input.VideoId);
        if (input.PrivacyStatus == null && input.PublishAt == null && input.CancelScheduledPublication != true && input.License == null && input.AllowEmbedding == null && input.PublicStatsViewable == null && input.MadeForKids == null && input.ContainsSyntheticMedia == null)
            throw new PluginMisconfigurationException("Supply at least one publication setting to update.");
        if (input.PublishAt.HasValue && input.CancelScheduledPublication == true)
            throw new PluginMisconfigurationException("Supply Publish at or Cancel scheduled publication, not both.");
        var privacy = input.PrivacyStatus == null ? null : InputValidator.Privacy(input.PrivacyStatus);
        if (input.License != null && input.License is not ("youtube" or "creativeCommon"))
            throw new PluginMisconfigurationException("Choose a valid License from the dropdown.");
        var scheduled = input.PublishAt.HasValue ? InputValidator.Utc(input.PublishAt.Value) : (DateTime? )null;
        if (scheduled <= DateTime.UtcNow)
            throw new PluginMisconfigurationException("Publish at must be in the future. Use public visibility to publish immediately.");
        var current = await ReadVideo(id);
        privacy ??= current.Status.PrivacyStatus;
        if (scheduled.HasValue && (privacy != "private" || current.Status.PrivacyStatus != "private"))
            throw new PluginMisconfigurationException("Only an already private video can be scheduled, with private target visibility. The video must never have been published; Google enforces that restriction.");
        var status = EditableStatus(current.Status);
        status["privacyStatus"] = privacy;
        if (input.CancelScheduledPublication == true || privacy != "private")
            status.Remove("publishAt");
        if (scheduled.HasValue)
            status["publishAt"] = scheduled.Value.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        else if (status.ContainsKey("publishAt") && current.Status.PublishAt <= DateTime.UtcNow)
            throw new PluginApplicationException("The video's stored publication schedule has elapsed. Read its current state or explicitly cancel the schedule before changing other settings.");
        if (input.License != null)
            status["license"] = input.License;
        if (input.AllowEmbedding.HasValue)
            status["embeddable"] = input.AllowEmbedding.Value;
        if (input.PublicStatsViewable.HasValue)
            status["publicStatsViewable"] = input.PublicStatsViewable.Value;
        if (input.MadeForKids.HasValue)
            status["selfDeclaredMadeForKids"] = input.MadeForKids.Value;
        if (input.ContainsSyntheticMedia.HasValue)
            status["containsSyntheticMedia"] = input.ContainsSyntheticMedia.Value;
        var request = new RestRequest("videos", Method.Put).AddQueryParameter("part", "status").AddJsonBody(new Dictionary<string, object> { ["id"] = id, ["status"] = status });
        if (!string.IsNullOrEmpty(current.Etag))
            request.AddHeader("If-Match", current.Etag);
        await Client.ExecuteWithErrorHandling(request);
        return Map(await ReadVideo(id));
    });
    [Action("Search channel videos", Description = "Find existing videos in the connected channel, including private and unlisted uploads. Supports metadata and publication-date filters. Requires the owning channel's OAuth connection.")]
    public Task<ChannelVideosResponse> SearchChannelVideos([ActionParameter] ChannelVideoSearchInput input) => YouTubeClient.RunAsync<ChannelVideosResponse>(async () =>
    {
        Client.RequireOAuth();
        var maximum = InputValidator.MaximumResults(input.MaximumResults);
        var visibility = InputValidator.Visibility(input.VideoVisibility, "all");
        InputValidator.PublicationRange(input.PublishedAfter, input.PublishedBefore);
        var videos = new VideoActions(Client);
        ResolvedChannel channel;
        if (string.IsNullOrWhiteSpace(input.ChannelUrl))
        {
            var request = new RestRequest("channels").AddQueryParameter("part", "contentDetails").AddQueryParameter("mine", "true").AddQueryParameter("maxResults", "50");
            var channels = await Client.ExecuteWithErrorHandling<YouTubePage<ChannelInventoryDto>>(request);
            var owned = channels.Items.SingleOrDefault() ?? throw new PluginMisconfigurationException("No single channel was selected by this OAuth connection. Reconnect using the intended YouTube or Brand Account and supply its Channel URL.");
            channel = new(owned.Id, owned.ContentDetails.RelatedPlaylists.Uploads);
        }
        else
        {
            channel = await videos.ResolveChannelAsync(YouTubeUrls.ParseChannel(input.ChannelUrl));
            await videos.VerifyOwnedChannelAsync(channel.Id);
        }

        var results = new List<Video>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var requestItems = new RestRequest("playlistItems").AddQueryParameter("part", "contentDetails").AddQueryParameter("playlistId", channel.UploadsPlaylistId).AddQueryParameter("maxResults", "50");
        await Client.Paginate<PlaylistEntryDto>(requestItems, continueAfterPage: async page =>
        {
            var ids = page.Select(x => x.ContentDetails.VideoId).Where(x => !string.IsNullOrEmpty(x) && seen.Add(x)).ToList();
            var hydrated = (await videos.GetVideosAsync(ids)).ToDictionary(x => x.VideoId, StringComparer.Ordinal);
            foreach (var videoId in ids)
            {
                if (!hydrated.TryGetValue(videoId, out var video) || video.ChannelId != channel.Id)
                    continue;
                if (video.PrivacyStatus is not ("private" or "unlisted" or "public") || visibility != "all" && video.PrivacyStatus != visibility)
                    continue;
                if (input.PublishedAfter.HasValue && video.PublishedAt < InputValidator.Utc(input.PublishedAfter.Value))
                    continue;
                if (input.PublishedBefore.HasValue && video.PublishedAt > InputValidator.Utc(input.PublishedBefore.Value))
                    continue;
                if (!InputValidator.Matches(video.Title, video.Description, input.SearchTerm))
                    continue;
                results.Add(video);
                if (results.Count == maximum)
                    break;
            }

            return results.Count < maximum;
        });
        return new ChannelVideosResponse
        {
            Videos = results
        };
    });
    private async Task<VideoDto> ReadVideo(string id)
    {
        var request = new RestRequest("videos").AddQueryParameter("part", "snippet,status").AddQueryParameter("id", id);
        var page = await Client.ExecuteWithErrorHandling<YouTubePage<VideoDto>>(request);
        return page.Items.SingleOrDefault() ?? throw new PluginMisconfigurationException("Video not found or not accessible. Check Video ID and the OAuth account's channel permissions.");
    }

    private static Dictionary<string, object> EditableSnippet(VideoSnippetDto value)
    {
        var snippet = new Dictionary<string, object>
        {
            ["title"] = value.Title,
            ["description"] = value.Description,
            ["categoryId"] = value.CategoryId
        };
        if (value.Tags != null)
            snippet["tags"] = value.Tags.ToList();
        if (!string.IsNullOrEmpty(value.DefaultLanguage))
            snippet["defaultLanguage"] = value.DefaultLanguage;
        if (!string.IsNullOrEmpty(value.DefaultAudioLanguage))
            snippet["defaultAudioLanguage"] = value.DefaultAudioLanguage;
        return snippet;
    }

    private static Dictionary<string, object> EditableStatus(VideoStatusDto value)
    {
        var status = new Dictionary<string, object>
        {
            ["privacyStatus"] = value.PrivacyStatus
        };
        if (value.PublishAt.HasValue)
            status["publishAt"] = InputValidator.Utc(value.PublishAt.Value).ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        if (value.License != null)
            status["license"] = value.License;
        if (value.Embeddable.HasValue)
            status["embeddable"] = value.Embeddable.Value;
        if (value.PublicStatsViewable.HasValue)
            status["publicStatsViewable"] = value.PublicStatsViewable.Value;
        if (value.SelfDeclaredMadeForKids.HasValue)
            status["selfDeclaredMadeForKids"] = value.SelfDeclaredMadeForKids.Value;
        if (value.ContainsSyntheticMedia.HasValue)
            status["containsSyntheticMedia"] = value.ContainsSyntheticMedia.Value;
        return status;
    }

    private static ManagedVideoResponse Map(VideoDto value) => new()
    {
        VideoId = value.Id,
        Title = value.Snippet.Title,
        Description = value.Snippet.Description,
        CategoryId = value.Snippet.CategoryId,
        DefaultLanguage = value.Snippet.DefaultLanguage,
        DefaultAudioLanguage = value.Snippet.DefaultAudioLanguage,
        Tags = value.Snippet.Tags ?? [],
        PrivacyStatus = value.Status.PrivacyStatus,
        UploadStatus = value.Status.UploadStatus,
        PublishAt = value.Status.PublishAt,
        License = value.Status.License,
        AllowEmbedding = value.Status.Embeddable,
        PublicStatsViewable = value.Status.PublicStatsViewable,
        MadeForKids = value.Status.SelfDeclaredMadeForKids,
        ContainsSyntheticMedia = value.Status.ContainsSyntheticMedia
    };
}
