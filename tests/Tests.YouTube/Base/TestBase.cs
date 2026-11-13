using System.Text.Json;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.YouTube.Base;
public class TestBase
{
    private static Task<UploadedVideoResponse>? video;
    private static Task<CaptionTrack>? caption;
    private static string? playlistId;
    private static readonly List<string> additionalPlaylistIds = [];
    protected readonly InvocationContext InvocationContext;
    protected readonly FileManager FileManager = new();
    protected readonly YouTubeClient Client;
    protected TestBase()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            throw new InvalidOperationException("Copy appsettings.json.example to appsettings.json and configure a disposable YouTube channel's OAuth connection before running live tests.");
        using var config = JsonDocument.Parse(File.ReadAllText(path));
        var credentials = config.RootElement.GetProperty("ConnectionDefinition").EnumerateObject().Select(p => new AuthenticationCredentialsProvider(p.Name, p.Value.GetString() ?? "")).ToList();
        InvocationContext = new InvocationContext
        {
            AuthenticationCredentialsProviders = credentials
        };
        Client = new(credentials);
    }

    protected Task<UploadedVideoResponse> OwnedVideo() => video ??= new PublishingActions(InvocationContext, FileManager).UploadVideo(new() { Title = "Blackbird SDK integration test " + Guid.NewGuid().ToString("N"), Description = "Disposable integration test video.", File = new FileReference { Name = "sample.mp4", ContentType = "video/mp4" }, PrivacyStatus = "private", DefaultLanguage = "en", MadeForKids = false, NotifySubscribers = false, Tags = ["blackbird", "integration"] });
    protected async Task<CaptionTrack> OwnedCaption() => await (caption ??= CreateCaption());
    protected async Task<string> OwnedPlaylist()
    {
        if (playlistId != null)
            return playlistId;
        var playlist = await new PlaylistActions(InvocationContext).CreatePlaylist(new() { Title = "Blackbird disposable integration test", Description = "Disposable test collection.", DefaultLanguage = "en" });
        return playlistId = playlist.PlaylistId;
    }

    protected async Task<string> OwnedChannelId() => (await Client.GetAsync<YouTubePage<PickerItem>>("channels", new() { ["part"] = "id", ["mine"] = "true" })).Items.First().Id;
    protected static void TrackPlaylist(string id) => additionalPlaylistIds.Add(id);
    protected async Task Cleanup()
    {
        var actions = new AdditionalActions(InvocationContext);
        foreach (var id in additionalPlaylistIds.Distinct())
            await actions.ApiRequest(new() { Url = "https://www.googleapis.com/youtube/v3/playlists", Method = "DELETE", QueryJson = JsonSerializer.Serialize(new { id }) });
        if (playlistId != null)
            await actions.ApiRequest(new() { Url = "https://www.googleapis.com/youtube/v3/playlists", Method = "DELETE", QueryJson = JsonSerializer.Serialize(new { id = playlistId }) });
        if (video?.IsCompletedSuccessfully == true)
            await actions.ApiRequest(new() { Url = "https://www.googleapis.com/youtube/v3/videos", Method = "DELETE", QueryJson = JsonSerializer.Serialize(new { id = video.Result.VideoId }) });
    }

    private async Task<CaptionTrack> CreateCaption() => await new CaptionFileActions(InvocationContext, FileManager).UploadCaptions(new() { VideoId = (await OwnedVideo()).VideoId, Language = "fr", Name = "Blackbird test " + Guid.NewGuid().ToString("N"), File = new FileReference { Name = "sample.srt", ContentType = "application/x-subrip" }, IsDraft = true });
}
