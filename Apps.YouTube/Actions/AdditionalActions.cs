using System.Text.Json;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.YouTube;
[ActionList("Playlists and reports")]
public sealed class AdditionalActions(InvocationContext context) : Invocable(context)
{
    [Action("Find video", Description = "Find the first public video matching the search filters. Outputs Found as false when nothing matches.")]
    public Task<FindVideoResponse> FindVideo([ActionParameter] SearchVideosInput input) => YouTubeClient.RunAsync<FindVideoResponse>(async () =>
    {
        var copy = JsonSerializer.Deserialize<SearchVideosInput>(JsonSerializer.Serialize(input))!;
        copy.MaximumResults = 1;
        var result = await new VideoActions(Client).SearchAsync(copy);
        return new()
        {
            Video = result.FirstVideo
        };
    });
    [Action("Add video to playlist", Description = "Add a video to a managed playlist, optionally specifying its position and a note. Requires OAuth.")]
    public Task<PlaylistVideoResponse> AddVideoToPlaylist([ActionParameter] AddPlaylistVideoInput input) => YouTubeClient.RunAsync<PlaylistVideoResponse>(() => new PublishingActions(Client).AddToPlaylistAsync(input));
    [Action("Get report", Description = "Get channel or partner content-owner analytics for the selected dates and metrics. Requires Analytics API access and OAuth.")]
    public Task<ReportResponse> GetReport([ActionParameter] ReportInput input) => YouTubeClient.RunAsync<ReportResponse>(() => new PublishingActions(Client).GetReportAsync(input));
    [Action("API request", Description = "Send an authenticated text or JSON request to an allowed video or analytics endpoint. Credentials are restricted to those endpoints.")]
    public Task<ApiRequestResponse> ApiRequest([ActionParameter] ApiRequestInput input) => YouTubeClient.RunAsync<ApiRequestResponse>(() => new PublishingActions(Client).RequestAsync(input));
}
