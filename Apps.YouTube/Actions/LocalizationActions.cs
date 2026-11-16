using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Apps.YouTube;
[ActionList("Localization")]
public sealed class LocalizationActions : Invocable
{
    public LocalizationActions(InvocationContext context) : base(context)
    {
    }

    internal LocalizationActions(YouTubeClient client) : base(client)
    {
    }

    [Action("Get video localizations", Description = "Get the source title and description and all existing metadata translations.")]
    public Task<VideoLocalizationsResponse> GetVideoLocalizations([ActionParameter] VideoInput input) => YouTubeClient.RunAsync<VideoLocalizationsResponse>(() => this.GetLocalizationsAsync(input.VideoId));
    [Action("Update video localization", Description = "Add or update one language’s title and description while preserving other translations. Requires OAuth.")]
    public Task<VideoLocalizationsResponse> UpdateVideoLocalization([ActionParameter] UpdateVideoLocalizationInput input) => YouTubeClient.RunAsync<VideoLocalizationsResponse>(() => this.UpdateLocalizationAsync(input));
    [Action("Search caption tracks", Description = "Find caption tracks and their language, status and draft information for a managed video. Requires OAuth.")]
    public Task<CaptionTracksResponse> SearchCaptionTracks([ActionParameter] SearchCaptionsInput input) => YouTubeClient.RunAsync<CaptionTracksResponse>(async () =>
    {
        var language = input.Language == null ? null : LanguageTag(input.Language);
        var tracks = await this.SearchCaptionsAsync(input.VideoId);
        return new()
        {
            CaptionTracks = tracks.Where(c => (language == null || string.Equals(c.Language, language, StringComparison.OrdinalIgnoreCase)) && (input.IncludeDrafts != false || !c.IsDraft)).ToList()
        };
    });
    [Action("Find missing languages", Description = "Identify required languages missing from complete metadata translations or published captions. Requires OAuth.")]
    public Task<MissingLanguagesResponse> FindMissingLanguages([ActionParameter] MissingLanguagesInput input) => YouTubeClient.RunAsync<MissingLanguagesResponse>(() => this.FindMissingLanguagesAsync(input));
    public async Task<VideoLocalizationsResponse> GetLocalizationsAsync(string videoId) => MapLocalizations(await ReadVideoAsync(YouTubeUrls.ParseVideoId(videoId)));
    public async Task<VideoLocalizationsResponse> UpdateLocalizationAsync(UpdateVideoLocalizationInput input)
    {
        api.RequireOAuth();
        var id = YouTubeUrls.ParseVideoId(input.VideoId);
        var language = LanguageTag(input.Language);
        if (input.Title == null && input.Description == null)
            throw new PluginMisconfigurationException("Provide a localized Title or Description to update.");
        var current = await ReadVideoAsync(id);
        var source = current.Snippet;
        var defaultLanguage = source.DefaultLanguage;
        var setDefault = string.IsNullOrEmpty(defaultLanguage);
        if (setDefault)
        {
            if (string.IsNullOrWhiteSpace(input.DefaultLanguage))
                throw new PluginMisconfigurationException("This video has no source metadata language. Supply Default language, such as en, before adding translations.");
            defaultLanguage = LanguageTag(input.DefaultLanguage);
        }
        else if (input.DefaultLanguage != null && !SameLanguage(defaultLanguage, LanguageTag(input.DefaultLanguage)))
            throw new PluginMisconfigurationException("Default language differs from the video's existing source language. Omit it or supply the existing language.");
        if (SameLanguage(language, defaultLanguage))
            throw new PluginMisconfigurationException("Target language matches the source metadata language. Choose another language; this action updates translations only.");
        var localizations = current.Localizations.ToDictionary(x => x.Key, x => new Dictionary<string, object> { ["title"] = x.Value.Title, ["description"] = x.Value.Description });
        var key = localizations.Keys.FirstOrDefault(x => SameLanguage(x, language)) ?? language;
        localizations.TryGetValue(key, out var existing);
        var title = input.Title ?? existing?.GetValueOrDefault("title")?.ToString() ?? "";
        var description = input.Description ?? existing?.GetValueOrDefault("description")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(title) || title.Length > 100 || title.Contains('<') || title.Contains('>'))
            throw new PluginMisconfigurationException("Supply a non-empty localized Title of at most 100 characters, without < or >.");
        if (Encoding.UTF8.GetByteCount(description) > 5000 || description.Contains('<') || description.Contains('>'))
            throw new PluginMisconfigurationException("The localized Description must be at most 5,000 UTF-8 bytes and must not contain < or >.");
        localizations[key] = new Dictionary<string, object>
        {
            ["title"] = title,
            ["description"] = description
        };
        var payload = new Dictionary<string, object>
        {
            ["id"] = id,
            ["localizations"] = localizations
        };
        var part = "localizations";
        if (setDefault)
        {
            var snippet = new Dictionary<string, object>
            {
                ["title"] = source.Title,
                ["description"] = source.Description,
                ["categoryId"] = source.CategoryId,
                ["defaultLanguage"] = defaultLanguage
            };
            if (source.Tags != null)
                snippet["tags"] = source.Tags;
            if (!string.IsNullOrEmpty(source.DefaultAudioLanguage))
                snippet["defaultAudioLanguage"] = source.DefaultAudioLanguage;
            payload["snippet"] = snippet;
            part += ",snippet";
        }

        using var body = JsonBody(payload);
        await api.WriteAsync(HttpMethod.Put, "videos", new() { ["part"] = part }, body, current.Etag);
        return await GetLocalizationsAsync(id);
    }

    public async Task<List<CaptionTrack>> SearchCaptionsAsync(string videoId)
    {
        api.RequireOAuth();
        var id = YouTubeUrls.ParseVideoId(videoId);
        var data = await api.GetAsync<YouTubePage<CaptionDto>>("captions", new() { ["part"] = "snippet", ["videoId"] = id });
        return data.Items.Select(MapCaption).ToList();
    }

    public async Task<Stream> DownloadCaptionsAsync(string captionId, string format)
    {
        api.RequireOAuth();
        ValidateId(captionId);
        if (!new[]
        {
            "srt",
            "vtt",
            "ttml",
            "sbv",
            "scc"
        }.Contains(format))
            throw new PluginMisconfigurationException("Choose a supported caption format from the dropdown.");
        var response = await api.SendAsync(HttpMethod.Get, "captions/" + Uri.EscapeDataString(captionId), new() { ["tfmt"] = format }, streamResponse: true);
        try
        {
            if (response.Content.Headers.ContentLength > 100_000_000)
                throw new PluginMisconfigurationException("Caption files must not exceed 100 MB.");
            var stream = await response.Content.ReadAsStreamAsync();
            return new TransferStream(stream, 100_000_000, response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    public async Task<CaptionTrack> UploadCaptionsAsync(UploadCaptionsInput input, Stream file)
    {
        api.RequireOAuth();
        var id = YouTubeUrls.ParseVideoId(input.VideoId);
        var language = LanguageTag(input.Language);
        var name = input.Name?.Trim() ?? language;
        if (name.Length > 150)
            throw new PluginMisconfigurationException("Caption track name must be at most 150 characters.");
        var payload = new Dictionary<string, object>
        {
            ["snippet"] = new Dictionary<string, object>
            {
                ["videoId"] = id,
                ["language"] = language,
                ["name"] = name,
                ["isDraft"] = input.IsDraft ?? true
            }
        };
        using var bytes = new TransferStream(file, 100_000_000);
        using var body = CaptionBody(payload, bytes);
        var result = await api.WriteAsync(HttpMethod.Post, "upload/youtube/v3/captions", new() { ["part"] = "snippet", ["uploadType"] = "multipart" }, body);
        return MapCaption(Newtonsoft.Json.JsonConvert.DeserializeObject<CaptionDto>(result.GetRawText())!);
    }

    public async Task<CaptionTrack> UpdateCaptionsAsync(UpdateCaptionsInput input, Stream? file)
    {
        api.RequireOAuth();
        ValidateId(input.CaptionId);
        if (file == null && input.IsDraft == null)
            throw new PluginMisconfigurationException("Supply a replacement caption File or Is draft value to update.");
        var current = (await SearchCaptionsAsync(input.VideoId)).SingleOrDefault(c => c.CaptionId == input.CaptionId) ?? throw new PluginMisconfigurationException("Caption ID does not belong to this video or is not accessible with this connection.");
        var payload = new Dictionary<string, object>
        {
            ["id"] = input.CaptionId,
            ["snippet"] = new Dictionary<string, object>
            {
                ["isDraft"] = input.IsDraft ?? current.IsDraft
            }
        };
        JsonElement result;
        if (file != null)
        {
            using var bytes = new TransferStream(file, 100_000_000);
            using var body = CaptionBody(payload, bytes);
            result = await api.WriteAsync(HttpMethod.Put, "upload/youtube/v3/captions", new() { ["part"] = "snippet", ["uploadType"] = "multipart" }, body);
        }
        else
        {
            using var body = JsonBody(payload);
            result = await api.WriteAsync(HttpMethod.Put, "captions", new() { ["part"] = "snippet" }, body);
        }

        return MapCaption(Newtonsoft.Json.JsonConvert.DeserializeObject<CaptionDto>(result.GetRawText())!);
    }

    public async Task<MissingLanguagesResponse> FindMissingLanguagesAsync(MissingLanguagesInput input)
    {
        api.RequireOAuth();
        var required = input.RequiredLanguages?.Select(LanguageTag).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        if (required.Count == 0)
            throw new PluginMisconfigurationException("Supply at least one required language.");
        var localizations = await GetLocalizationsAsync(input.VideoId);
        var captions = await SearchCaptionsAsync(input.VideoId);
        bool Complete(string title, string description) => !string.IsNullOrWhiteSpace(title) && (string.IsNullOrWhiteSpace(localizations.SourceDescription) || !string.IsNullOrWhiteSpace(description));
        var metadataLanguages = localizations.Localizations.Where(x => Complete(x.Title, x.Description)).Select(x => x.Language).ToList();
        if (!string.IsNullOrEmpty(localizations.DefaultLanguage) && Complete(localizations.SourceTitle, localizations.SourceDescription))
            metadataLanguages.Add(localizations.DefaultLanguage);
        var captionLanguages = captions.Where(c => !c.IsDraft && c.Status == "serving" && (input.CountAutoGeneratedCaptions == true || c.TrackKind != "ASR")).Select(c => c.Language).ToList();
        metadataLanguages = metadataLanguages.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        captionLanguages = captionLanguages.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new()
        {
            AvailableMetadataLanguages = metadataLanguages,
            AvailableCaptionLanguages = captionLanguages,
            MissingMetadataLanguages = required.Where(l => !metadataLanguages.Any(a => SameLanguage(a, l))).ToList(),
            MissingCaptionLanguages = required.Where(l => !captionLanguages.Any(a => SameLanguage(a, l))).ToList()
        };
    }

    public static string LanguageTag(string value)
    {
        value = value?.Trim() ?? "";
        if (!Regex.IsMatch(value, "^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*$"))
            throw new PluginMisconfigurationException("Enter a language tag such as en, fr, de, pt-BR or zh-Hans. Google validates which tags it supports.");
        return value;
    }

    private YouTubeClient api => Client;

    private async Task<VideoDto> ReadVideoAsync(string id)
    {
        var page = await api.GetAsync<YouTubePage<VideoDto>>("videos", new() { ["part"] = "snippet,localizations", ["id"] = id });
        return page.Items.SingleOrDefault() ?? throw new PluginMisconfigurationException("The video was not found or is not accessible with this connection.");
    }

    private static VideoLocalizationsResponse MapLocalizations(VideoDto video) => new()
    {
        VideoId = video.Id,
        DefaultLanguage = video.Snippet.DefaultLanguage,
        SourceTitle = video.Snippet.Title,
        SourceDescription = video.Snippet.Description,
        Localizations = video.Localizations.Select(p => new VideoLocalization { Language = p.Key, Title = p.Value.Title, Description = p.Value.Description }).ToList()
    };
    private static CaptionTrack MapCaption(CaptionDto caption) => new()
    {
        CaptionId = caption.Id,
        VideoId = caption.Snippet.VideoId,
        Language = caption.Snippet.Language,
        Name = caption.Snippet.Name,
        TrackKind = caption.Snippet.TrackKind,
        Status = caption.Snippet.Status,
        FailureReason = caption.Snippet.FailureReason,
        IsDraft = caption.Snippet.IsDraft,
        LastUpdated = caption.Snippet.LastUpdated
    };
    private static bool SameLanguage(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 500 || id.Any(char.IsControl))
            throw new PluginMisconfigurationException("Enter a valid Caption ID from Search caption tracks.");
    }

    private static StringContent JsonBody(object data) => new(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
    private static MultipartContent CaptionBody(object data, Stream file)
    {
        var body = new MultipartContent("related");
        body.Add(JsonBody(data));
        var content = new StreamContent(file);
        content.Headers.ContentType = new("application/octet-stream");
        body.Add(content);
        return body;
    }
}
