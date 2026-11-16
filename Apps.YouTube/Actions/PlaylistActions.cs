using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.YouTube;
[ActionList("Playlists")]
public sealed class PlaylistActions(InvocationContext context) : Invocable(context)
{
    [Action("Create playlist", Description = "Create a playlist with a title, description and visibility. New playlists default to private. Requires OAuth.")]
    public Task<PlaylistResponse> CreatePlaylist([ActionParameter] CreatePlaylistInput input) => YouTubeClient.RunAsync<PlaylistResponse>(async () =>
    {
        Client.RequireOAuth();
        InputValidator.PlaylistTitle(input.Title);
        var snippet = new Dictionary<string, object>
        {
            ["title"] = input.Title,
            ["description"] = input.Description ?? ""
        };
        if (input.DefaultLanguage != null)
            snippet["defaultLanguage"] = LocalizationActions.LanguageTag(input.DefaultLanguage);
        var request = new RestRequest("playlists", Method.Post).AddQueryParameter("part", "snippet,status,contentDetails").AddJsonBody(new Dictionary<string, object> { ["snippet"] = snippet, ["status"] = new Dictionary<string, object> { ["privacyStatus"] = InputValidator.Privacy(input.PrivacyStatus ?? "private") } });
        return Map(await Client.ExecuteWithErrorHandling<PlaylistDto>(request));
    });
    [Action("Get playlist", Description = "Get playlist metadata, visibility, video count and existing metadata translations using its ID or URL.")]
    public Task<PlaylistResponse> GetPlaylist([ActionParameter] PlaylistIdentifier input) => YouTubeClient.RunAsync<PlaylistResponse>(async () => Map(await ReadPlaylist(PublishingActions.PlaylistId(input.PlaylistId))));
    [Action("Search playlists", Description = "Find playlists by channel and optional title or description text. Omit the channel to search the connected account's playlists with OAuth.")]
    public Task<SearchPlaylistsResponse> SearchPlaylists([ActionParameter] PlaylistSearchInput input) => YouTubeClient.RunAsync<SearchPlaylistsResponse>(async () =>
    {
        var maximum = InputValidator.MaximumResults(input.MaximumResults);
        var request = new RestRequest("playlists").AddQueryParameter("part", "snippet,status,contentDetails,localizations").AddQueryParameter("maxResults", "50");
        if (string.IsNullOrWhiteSpace(input.ChannelUrl))
        {
            Client.RequireOAuth();
            request.AddQueryParameter("mine", "true");
        }
        else
        {
            var channel = await new VideoActions(Client).ResolveChannelAsync(YouTubeUrls.ParseChannel(input.ChannelUrl));
            request.AddQueryParameter("channelId", channel.Id);
        }

        var results = new List<PlaylistResponse>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        await Client.Paginate<PlaylistDto>(request, continueAfterPage: page =>
        {
            foreach (var playlist in page)
            {
                if (!seen.Add(playlist.Id) || !InputValidator.Matches(playlist.Snippet.Title, playlist.Snippet.Description, input.SearchTerm))
                    continue;
                results.Add(Map(playlist));
                if (results.Count == maximum)
                    break;
            }

            return Task.FromResult(results.Count < maximum);
        });
        return new SearchPlaylistsResponse
        {
            Playlists = results
        };
    });
    [Action("Update playlist", Description = "Update a playlist's title, description, source language or visibility while preserving omitted settings and existing translations. Requires OAuth.")]
    public Task<PlaylistResponse> UpdatePlaylist([ActionParameter] UpdatePlaylistInput input) => YouTubeClient.RunAsync<PlaylistResponse>(async () =>
    {
        Client.RequireOAuth();
        var id = PublishingActions.PlaylistId(input.PlaylistId);
        if (input.Title == null && input.Description == null && input.PrivacyStatus == null && input.DefaultLanguage == null)
            throw new PluginMisconfigurationException("Supply a Title, Description, Privacy status or Default language to update.");
        if (input.Title != null)
            InputValidator.PlaylistTitle(input.Title);
        var privacy = input.PrivacyStatus == null ? null : InputValidator.Privacy(input.PrivacyStatus);
        var language = input.DefaultLanguage == null ? null : LocalizationActions.LanguageTag(input.DefaultLanguage);
        var current = await ReadPlaylist(id);
        var snippet = new Dictionary<string, object>
        {
            ["title"] = input.Title ?? current.Snippet.Title,
            ["description"] = input.Description ?? current.Snippet.Description
        };
        var sourceLanguage = language ?? current.Snippet.DefaultLanguage;
        if (!string.IsNullOrEmpty(sourceLanguage))
            snippet["defaultLanguage"] = sourceLanguage;
        var payload = new Dictionary<string, object>
        {
            ["id"] = id,
            ["snippet"] = snippet
        };
        var part = "snippet";
        if (privacy != null)
        {
            var status = new Dictionary<string, object>
            {
                ["privacyStatus"] = privacy
            };
            if (current.Status.PodcastStatus != null)
                status["podcastStatus"] = current.Status.PodcastStatus;
            payload["status"] = status;
            part += ",status";
        }

        var request = new RestRequest("playlists", Method.Put).AddQueryParameter("part", part).AddJsonBody(payload);
        if (!string.IsNullOrEmpty(current.Etag))
            request.AddHeader("If-Match", current.Etag);
        await Client.ExecuteWithErrorHandling(request);
        return Map(await ReadPlaylist(id));
    });
    [Action("Search playlist videos", Description = "Get accessible video entries from a playlist, with optional text and visibility filters. Preserves playlist order and repeated entries.")]
    public Task<PlaylistVideosResponse> SearchPlaylistVideos([ActionParameter] PlaylistVideoSearchInput input) => YouTubeClient.RunAsync<PlaylistVideosResponse>(async () =>
    {
        var id = PublishingActions.PlaylistId(input.PlaylistId);
        var maximum = InputValidator.MaximumResults(input.MaximumResults);
        var visibility = InputValidator.Visibility(input.VideoVisibility);
        if (visibility != "public")
            Client.RequireOAuth();
        await ReadPlaylist(id);
        var request = new RestRequest("playlistItems").AddQueryParameter("part", "snippet,contentDetails").AddQueryParameter("playlistId", id).AddQueryParameter("maxResults", "50");
        var results = new List<PlaylistVideoResponse>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var videoActions = new VideoActions(Client);
        await Client.Paginate<PlaylistEntryDto>(request, continueAfterPage: async page =>
        {
            var videos = (await videoActions.GetVideosAsync(page.Select(x => x.ContentDetails.VideoId).Where(x => !string.IsNullOrEmpty(x)))).ToDictionary(x => x.VideoId, StringComparer.Ordinal);
            foreach (var entry in page)
            {
                if (!seen.Add(entry.Id) || !videos.TryGetValue(entry.ContentDetails.VideoId, out var video))
                    continue;
                if (video.PrivacyStatus is not ("public" or "private" or "unlisted") || visibility != "all" && video.PrivacyStatus != visibility)
                    continue;
                if (!InputValidator.Matches(video.Title, video.Description, input.SearchTerm))
                    continue;
                results.Add(new PlaylistVideoResponse { PlaylistId = id, PlaylistItemId = entry.Id, VideoId = video.VideoId, Position = entry.Snippet.Position, Video = video });
                if (results.Count == maximum)
                    break;
            }

            return results.Count < maximum;
        });
        return new PlaylistVideosResponse
        {
            Videos = results
        };
    });
    [Action("Remove video from playlist", Description = "Remove a specific playlist entry using Playlist item ID. The video itself is preserved. Requires OAuth.")]
    public Task<RemovedPlaylistVideoResponse> RemoveVideoFromPlaylist([ActionParameter] RemovePlaylistVideoInput input) => YouTubeClient.RunAsync<RemovedPlaylistVideoResponse>(async () =>
    {
        Client.RequireOAuth();
        var id = InputValidator.PlaylistItemId(input.PlaylistItemId);
        var expected = input.PlaylistId == null ? null : PublishingActions.PlaylistId(input.PlaylistId);
        var read = new RestRequest("playlistItems").AddQueryParameter("part", "snippet").AddQueryParameter("id", id);
        var page = await Client.ExecuteWithErrorHandling<YouTubePage<PlaylistEntryDto>>(read);
        var entry = page.Items.SingleOrDefault() ?? throw new PluginMisconfigurationException("Playlist item ID was not found or is not accessible. Use Search playlist videos to obtain the entry ID.");
        if (expected != null && entry.Snippet.PlaylistId != expected)
            throw new PluginMisconfigurationException("Playlist item ID belongs to a different playlist. Check the expected Playlist ID before removing the entry.");
        var request = new RestRequest("playlistItems", Method.Delete).AddQueryParameter("id", id);
        await Client.ExecuteWithErrorHandling(request);
        return new RemovedPlaylistVideoResponse
        {
            PlaylistItemId = id,
            PlaylistId = entry.Snippet.PlaylistId,
            Removed = true
        };
    });
    private async Task<PlaylistDto> ReadPlaylist(string id)
    {
        var request = new RestRequest("playlists").AddQueryParameter("part", "snippet,status,contentDetails,localizations").AddQueryParameter("id", id);
        var page = await Client.ExecuteWithErrorHandling<YouTubePage<PlaylistDto>>(request);
        return page.Items.SingleOrDefault() ?? throw new PluginMisconfigurationException("Playlist not found or not accessible. Check Playlist ID and use an authorized OAuth connection for private playlists.");
    }

    private static PlaylistResponse Map(PlaylistDto value) => new()
    {
        PlaylistId = value.Id,
        Title = value.Snippet.Title,
        Description = value.Snippet.Description,
        ChannelId = value.Snippet.ChannelId,
        PublishedAt = value.Snippet.PublishedAt,
        PrivacyStatus = value.Status.PrivacyStatus,
        DefaultLanguage = value.Snippet.DefaultLanguage,
        PodcastStatus = value.Status.PodcastStatus,
        VideoCount = value.ContentDetails.ItemCount,
        Localizations = value.Localizations.Select(x => new VideoLocalization { Language = x.Key, Title = x.Value.Title, Description = x.Value.Description }).ToList()
    };
}
