using System.Text.Json;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Apps.YouTube;
[ActionList("Publishing")]
public sealed class PublishingActions : Invocable
{
    private readonly IFileManagementClient files;
    public PublishingActions(InvocationContext context, IFileManagementClient files) : base(context)
    {
        this.files = files;
    }

    internal PublishingActions(YouTubeClient client) : base(client)
    {
        files = null !;
    }

    [Action("Upload video", Description = "Upload a video file with metadata, an optional publication schedule and thumbnail. New videos default to private. Requires OAuth.")]
    public Task<UploadedVideoResponse> UploadVideo([ActionParameter] UploadVideoInput input) => YouTubeClient.RunAsync<UploadedVideoResponse>(async () =>
    {
        var api = Client;
        api.RequireOAuth();
        _ = PublishingActions.UploadMetadata(input);
        if (input.File == null)
            throw new PluginMisconfigurationException("Supply a Video file.");
        await using var stream = await files.DownloadAsync(input.File);
        var service = this;
        var result = await service.UploadVideoAsync(input, stream, input.File.Size, input.File.ContentType);
        if (input.Thumbnail != null)
        {
            try
            {
                if (input.Thumbnail.Size > 50_000_000)
                    throw new PluginMisconfigurationException("Use a thumbnail no larger than 50 MB.");
                await using var image = await files.DownloadAsync(input.Thumbnail);
                await service.SetThumbnailAsync(result.VideoId, image, input.Thumbnail.ContentType);
            }
            catch (Exception ex)when (ex is not OperationCanceledException && ex is not OutOfMemoryException)
            {
                result.Warnings.Add("Video " + result.VideoId + " was created, but its thumbnail was not updated. Use Update video thumbnail after processing completes. " + (ex is PluginMisconfigurationException or PluginApplicationException ? ex.Message : "The thumbnail file could not be transferred."));
            }
        }

        return result;
    });
    [Action("Update video thumbnail", Description = "Set a JPEG or PNG thumbnail for a managed video. Requires OAuth and a channel eligible for custom thumbnails.")]
    public Task<ThumbnailResponse> UpdateVideoThumbnail([ActionParameter] ThumbnailInput input) => YouTubeClient.RunAsync<ThumbnailResponse>(async () =>
    {
        var api = Client;
        api.RequireOAuth();
        if (input.File == null)
            throw new PluginMisconfigurationException("Supply a Thumbnail file.");
        if (input.File.Size > 50_000_000)
            throw new PluginMisconfigurationException("Use a thumbnail no larger than 50 MB.");
        await using var image = await files.DownloadAsync(input.File);
        return await this.SetThumbnailAsync(input.VideoId, image, input.File.ContentType);
    });
    public async Task<UploadedVideoResponse> UploadVideoAsync(UploadVideoInput input, Stream file, long declaredLength = 0, string? mime = null)
    {
        api.RequireOAuth();
        var metadata = UploadMetadata(input);
        var length = declaredLength > 0 ? declaredLength : file.CanSeek ? file.Length - file.Position : 0;
        if (length <= 0 || length > 256L * 1024 * 1024 * 1024)
            throw new PluginMisconfigurationException("Supply a non-empty video file with a known size, no larger than YouTube's 256 GB limit.");
        mime = mime?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true ? mime : "application/octet-stream";
        using var json = new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json");
        using var start = await api.SendAsync(HttpMethod.Post, "upload/youtube/v3/videos", new() { ["uploadType"] = "resumable", ["part"] = "snippet,status,localizations,recordingDetails", ["notifySubscribers"] = (input.NotifySubscribers ?? true) ? "true" : "false" }, json, headers: new() { ["X-Upload-Content-Type"] = mime, ["X-Upload-Content-Length"] = length.ToString(CultureInfo.InvariantCulture) });
        var session = start.Headers.Location?.ToString();
        if (string.IsNullOrEmpty(session))
            throw new PluginApplicationException("YouTube did not return an upload-session URL. Check channel uploads before retrying.");
        if (!Uri.TryCreate(session, UriKind.Absolute, out var sessionUri) || sessionUri.Host != "www.googleapis.com" || sessionUri.AbsolutePath != "/upload/youtube/v3/videos")
            throw new PluginApplicationException("YouTube returned an unexpected upload session endpoint. No video bytes were sent.");
        _ = ApiUrls.Build(session, []);
        using var content = new StreamContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue(mime);
        content.Headers.ContentLength = length;
        using var completed = await api.SendAsync(HttpMethod.Put, session, [], content);
        var data = await YouTubeClient.ReadJsonAsync(completed);
        var id = YouTubeClient.Text(data, "id");
        if (string.IsNullOrWhiteSpace(id))
            throw new PluginApplicationException("YouTube upload returned no video ID. Inspect channel uploads before retrying.");
        var status = data.TryGetProperty("status", out var s) ? s : default;
        return new()
        {
            VideoId = id,
            VideoUrl = "https://www.youtube.com/watch?v=" + id,
            PrivacyStatus = status.ValueKind == JsonValueKind.Object ? YouTubeClient.Text(status, "privacyStatus") : "",
            UploadStatus = status.ValueKind == JsonValueKind.Object ? YouTubeClient.Text(status, "uploadStatus") : ""
        };
    }

    public static Dictionary<string, object> UploadMetadata(UploadVideoInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 100 || input.Title.IndexOfAny(['<', '>']) >= 0)
            throw new PluginMisconfigurationException("Title must contain 1–100 characters without angle brackets.");
        if (Encoding.UTF8.GetByteCount(input.Description ?? "") > 5000 || (input.Description ?? "").IndexOfAny(['<', '>']) >= 0)
            throw new PluginMisconfigurationException("Description must fit 5,000 UTF-8 bytes without angle brackets.");
        var privacy = input.PrivacyStatus ?? "private";
        if (privacy is not ("private" or "public" or "unlisted"))
            throw new PluginMisconfigurationException("Choose a valid Privacy status.");
        var category = input.CategoryId ?? "22";
        if (!Regex.IsMatch(category, "^[0-9]+$"))
            throw new PluginMisconfigurationException("Category ID must be numeric.");
        var snippet = new Dictionary<string, object>
        {
            ["title"] = input.Title,
            ["description"] = input.Description ?? "",
            ["categoryId"] = category
        };
        if (input.Tags != null)
            snippet["tags"] = input.Tags.ToArray();
        if (input.DefaultLanguage != null)
            snippet["defaultLanguage"] = LocalizationActions.LanguageTag(input.DefaultLanguage);
        if (input.DefaultAudioLanguage != null)
            snippet["defaultAudioLanguage"] = LocalizationActions.LanguageTag(input.DefaultAudioLanguage);
        var status = new Dictionary<string, object>
        {
            ["privacyStatus"] = privacy
        };
        if (input.PublishAt.HasValue)
        {
            var time = Utc(input.PublishAt.Value);
            if (privacy != "private" || time <= DateTime.UtcNow)
                throw new PluginMisconfigurationException("Publish at must be in the future and Privacy status must be private.");
            status["publishAt"] = time.ToString("O", CultureInfo.InvariantCulture);
        }

        if (input.License != null)
        {
            if (input.License is not ("youtube" or "creativeCommon"))
                throw new PluginMisconfigurationException("Choose a valid License.");
            status["license"] = input.License;
        }

        if (input.AllowEmbedding.HasValue)
            status["embeddable"] = input.AllowEmbedding.Value;
        if (input.PublicStatsViewable.HasValue)
            status["publicStatsViewable"] = input.PublicStatsViewable.Value;
        if (input.MadeForKids.HasValue)
            status["selfDeclaredMadeForKids"] = input.MadeForKids.Value;
        if (input.ContainsSyntheticMedia.HasValue)
            status["containsSyntheticMedia"] = input.ContainsSyntheticMedia.Value;
        var body = new Dictionary<string, object>
        {
            ["snippet"] = snippet,
            ["status"] = status
        };
        if (input.Language != null)
        {
            var language = LocalizationActions.LanguageTag(input.Language);
            if (input.DefaultLanguage == null || string.IsNullOrWhiteSpace(input.LocalizedTitle) || input.LocalizedTitle.Length > 100 || input.LocalizedTitle.IndexOfAny(['<', '>']) >= 0 || Encoding.UTF8.GetByteCount(input.LocalizedDescription ?? "") > 5000 || (input.LocalizedDescription ?? "").IndexOfAny(['<', '>']) >= 0)
                throw new PluginMisconfigurationException("Localized upload metadata requires Default language, a valid Localized title and a description within YouTube's limits.");
            body["localizations"] = new Dictionary<string, object>
            {
                [language] = new Dictionary<string, object>
                {
                    ["title"] = input.LocalizedTitle,
                    ["description"] = input.LocalizedDescription ?? ""
                }
            };
        }
        else if (input.LocalizedTitle != null || input.LocalizedDescription != null)
            throw new PluginMisconfigurationException("Supply Language code for localized upload metadata.");
        if (input.RecordingDate.HasValue)
            body["recordingDetails"] = new Dictionary<string, object>
            {
                ["recordingDate"] = Utc(input.RecordingDate.Value).ToString("O", CultureInfo.InvariantCulture)
            };
        return body;
    }

    public async Task<ThumbnailResponse> SetThumbnailAsync(string videoId, Stream image, string? mime = null)
    {
        api.RequireOAuth();
        var id = YouTubeUrls.ParseVideoId(videoId);
        var prefix = new byte[8];
        var read = 0;
        while (read < prefix.Length)
        {
            var count = await image.ReadAsync(prefix.AsMemory(read));
            if (count == 0)
                break;
            read += count;
        }

        if (read == 0)
            throw new PluginMisconfigurationException("Thumbnail file is empty.");
        var png = read == 8 && prefix.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = read >= 3 && prefix[0] == 255 && prefix[1] == 216 && prefix[2] == 255;
        if (!png && !jpeg)
            throw new PluginMisconfigurationException("Supply a JPEG or PNG thumbnail.");
        using var bounded = new TransferStream(image, 50_000_000, prefix: prefix[..read]);
        using var content = new StreamContent(bounded);
        content.Headers.ContentType = new(png ? "image/png" : "image/jpeg");
        var data = await api.WriteAsync(HttpMethod.Post, "upload/youtube/v3/thumbnails/set", new() { ["videoId"] = id, ["uploadType"] = "media" }, content);
        return new()
        {
            ThumbnailsJson = data.GetRawText()
        };
    }

    public async Task<PlaylistVideoResponse> AddToPlaylistAsync(AddPlaylistVideoInput input)
    {
        api.RequireOAuth();
        var playlist = PlaylistId(input.PlaylistId);
        var id = YouTubeUrls.ParseVideoId(input.VideoId);
        if (input.Position < 0 || input.Note?.Length > 280)
            throw new PluginMisconfigurationException("Position must be zero or greater and Note at most 280 characters.");
        var snippet = new Dictionary<string, object>
        {
            ["playlistId"] = playlist,
            ["resourceId"] = new Dictionary<string, object>
            {
                ["kind"] = "youtube#video",
                ["videoId"] = id
            }
        };
        if (input.Position.HasValue)
            snippet["position"] = input.Position.Value;
        var body = new Dictionary<string, object>
        {
            ["snippet"] = snippet
        };
        if (input.Note != null)
            body["contentDetails"] = new Dictionary<string, object>
            {
                ["note"] = input.Note
            };
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var result = await api.WriteAsync(HttpMethod.Post, "playlistItems", new() { ["part"] = "snippet,contentDetails" }, content);
        var s = result.GetProperty("snippet");
        return new()
        {
            PlaylistItemId = YouTubeClient.Text(result, "id"),
            PlaylistId = playlist,
            VideoId = id,
            Position = s.TryGetProperty("position", out var pos) ? pos.GetInt32() : null
        };
    }

    public async Task<ReportResponse> GetReportAsync(ReportInput input)
    {
        api.RequireOAuth();
        var max = input.MaximumRows ?? 1000;
        if (input.StartDate == default || input.EndDate == default || input.StartDate.Date > input.EndDate.Date || max is < 1 or > 10000)
            throw new PluginMisconfigurationException("Supply valid Start date, End date, Report metrics and Maximum rows (1–10,000).");
        var type = string.IsNullOrWhiteSpace(input.ReportType) ? "channel" : input.ReportType;
        if (type is not ("channel" or "contentOwner"))
            throw new PluginMisconfigurationException("Choose Channel or Content owner report type.");
        if (type == "contentOwner" && string.IsNullOrWhiteSpace(input.ContentOwnerId))
            throw new PluginMisconfigurationException("Content-owner reports require Content owner ID and a partner-authorized identity.");
        if (input.Dimensions?.Split(',', StringSplitOptions.TrimEntries).Contains("month", StringComparer.Ordinal) == true
            && (input.StartDate.Day != 1 || input.EndDate.Day != 1))
            throw new PluginMisconfigurationException("Monthly reports require Start date and End date on the first day of a month. Choose By day to use arbitrary dates. Dates are not adjusted automatically.");
        var metrics = ReportRules.NormalizeMetrics(input.Metrics);
        ReportRules.ValidateSelection(input, metrics);
        var filters = ReportRules.BuildFilters(input);
        var channelId = string.IsNullOrWhiteSpace(input.ChannelId) ? "MINE" : input.ChannelId.Trim();
        if (!string.IsNullOrWhiteSpace(input.VideoId))
        {
            var videoId = YouTubeUrls.ParseVideoId(input.VideoId);
            var videos = await api.GetAsync<YouTubePage<VideoDto>>("videos", new() { ["part"] = "snippet", ["id"] = videoId });
            var video = videos.Items.SingleOrDefault() ?? throw new PluginMisconfigurationException("The Video ID helper is not accessible. Check the video or use its owning OAuth connection.");
            if (channelId != "MINE" && channelId != video.Snippet.ChannelId)
                throw new PluginMisconfigurationException("The Video ID helper must belong to Channel ID. Choose matching channel/video values or clear the helper and use advanced Filters.");
            await new VideoActions(api).VerifyOwnedChannelAsync(video.Snippet.ChannelId);
        }
        var query = new Dictionary<string, string>
        {
            ["ids"] = type == "channel" ? "channel==" + channelId : "contentOwner==" + input.ContentOwnerId,
            ["startDate"] = input.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["endDate"] = input.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["metrics"] = metrics
        };
        if (!string.IsNullOrWhiteSpace(input.Dimensions) && ReportRules.Dimension(input.Dimensions) != "none")
            query["dimensions"] = input.Dimensions;
        if (!string.IsNullOrWhiteSpace(filters))
            query["filters"] = filters;
        if (!string.IsNullOrWhiteSpace(input.Sort))
            query["sort"] = input.Sort;
        var rows = new List<JsonElement>();
        var headers = new List<string>();
        for (var offset = 1; rows.Count <= max;)
        {
            var pageSize = Math.Min(200, max + 1 - rows.Count);
            query["maxResults"] = pageSize.ToString(CultureInfo.InvariantCulture);
            query["startIndex"] = offset.ToString(CultureInfo.InvariantCulture);
            var data = await api.GetAsync("https://youtubeanalytics.googleapis.com/v2/reports", query);
            if (data.TryGetProperty("columnHeaders", out var columns))
                headers = columns.EnumerateArray().Select(c => YouTubeClient.Text(c, "name")).ToList();
            if (!data.TryGetProperty("rows", out var returned) || returned.ValueKind != JsonValueKind.Array || returned.GetArrayLength() == 0)
                break;
            rows.AddRange(returned.EnumerateArray());
            offset += returned.GetArrayLength();
            if (returned.GetArrayLength() < pageSize)
                break;
        }

        return new()
        {
            ColumnHeaders = headers,
            RowsJson = JsonSerializer.Serialize(rows.Take(max)),
            ReturnedCount = Math.Min(rows.Count, max),
            MoreResultsAvailable = rows.Count > max
        };
    }

    public async Task<ApiRequestResponse> RequestAsync(ApiRequestInput input)
    {
        if (!input.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Supply a full HTTPS YouTube API URL.");
        var query = DictionaryJson(input.QueryJson);
        var headers = DictionaryJson(input.HeadersJson);
        _ = ApiUrls.Build(input.Url, query);
        if (input.Method is not ("GET" or "POST" or "PUT" or "PATCH" or "DELETE"))
            throw new PluginMisconfigurationException("Choose a supported HTTP method.");
        if (input.Method != "GET")
            api.RequireOAuth();
        foreach (var h in headers)
        {
            if (new[]
            {
                "Authorization",
                "Proxy-Authorization",
                "Host",
                "Cookie",
                "X-Goog-Api-Key",
                "Content-Length",
                "Transfer-Encoding",
                "Connection",
                "X-HTTP-Method-Override",
                "X-Method-Override"
            }.Contains(h.Key, StringComparer.OrdinalIgnoreCase) || h.Key.Any(char.IsControl) || h.Value.Any(c => c is '\r' or '\n'))
                throw new PluginMisconfigurationException("Authentication, routing, cookie and transport headers cannot be overridden.");
        }

        if (input.ContentType != null && !MediaTypeHeaderValue.TryParse(input.ContentType, out _))
            throw new PluginMisconfigurationException("Supply a valid Content type.");
        if (input.ContentType?.IndexOfAny(['\r', '\n']) >= 0)
            throw new PluginMisconfigurationException("Content type contains invalid characters.");
        if (input.Method == "GET" && input.Body != null)
            throw new PluginMisconfigurationException("GET requests cannot include a Body.");
        using var body = input.Body == null ? null : new StringContent(input.Body, Encoding.UTF8);
        if (body != null)
            body.Headers.ContentType = MediaTypeHeaderValue.Parse(input.ContentType ?? "application/json");
        using var response = await api.SendAsync(new HttpMethod(input.Method), input.Url, query, body, headers: headers, stopOnError: input.StopOnError ?? true);
        var text = await response.Content.ReadAsStringAsync();
        if (Encoding.UTF8.GetByteCount(text) > 10_000_000)
            throw new PluginApplicationException("API response exceeds this action's 10 MB text-output limit. Narrow the request or use a file action.");
        return new()
        {
            StatusCode = (int)response.StatusCode,
            Body = api.Redact(text),
            HeadersJson = api.Redact(JsonSerializer.Serialize(response.Headers.Where(h => !h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).ToDictionary(h => h.Key, h => string.Join(",", h.Value))))
        };
    }

    public static Dictionary<string, string> DictionaryJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? throw new JsonException();
            if (result.Values.Any(v => v == null))
                throw new JsonException();
            return result;
        }
        catch (JsonException)
        {
            throw new PluginMisconfigurationException("Query parameters and headers must be JSON objects containing string values.");
        }
    }

    public static string PlaylistId(string value)
    {
        value = value?.Trim() ?? "";
        if (Uri.TryCreate(value, UriKind.Absolute, out var u))
        {
            if (u.Scheme != "https" || u.Host is not ("youtube.com" or "www.youtube.com" or "m.youtube.com") || u.UserInfo != "")
                throw new PluginMisconfigurationException("Use a YouTube playlist ID or HTTPS playlist URL.");
            value = u.Query.TrimStart('?').Split('&').Select(x => x.Split('=', 2)).Where(x => x.Length == 2 && x[0] == "list").Select(x => Uri.UnescapeDataString(x[1])).FirstOrDefault() ?? "";
        }

        if (!Regex.IsMatch(value, "^[A-Za-z0-9_-]{2,200}$"))
            throw new PluginMisconfigurationException("Enter a valid Playlist ID or YouTube URL containing list=.");
        return value;
    }

    private YouTubeClient api => Client;

    internal static DateTime Utc(DateTime time) => time.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(time, DateTimeKind.Utc) : time.ToUniversalTime();
}
