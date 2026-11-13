using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.YouTube;
[ActionList("Caption files")]
public sealed class CaptionFileActions(InvocationContext context, IFileManagementClient fileManagementClient) : Invocable(context)
{
    [Action("Download captions", Description = "Download an authorized caption track as a file in the selected subtitle format. Requires OAuth and permission to edit the video.")]
    public Task<CaptionFileResponse> DownloadCaptions([ActionParameter] DownloadCaptionsInput input) => YouTubeClient.RunAsync<CaptionFileResponse>(async () =>
    {
        var format = input.Format?.Trim() ?? "srt";
        await using var stream = await new LocalizationActions(Client).DownloadCaptionsAsync(input.CaptionId, format);
        var mime = format == "vtt" ? "text/vtt" : format == "ttml" ? "application/ttml+xml" : "application/octet-stream";
        var file = await fileManagementClient.UploadAsync(stream, mime, "captions." + format);
        return new()
        {
            File = file,
            CaptionId = input.CaptionId
        };
    });
    [Action("Upload captions", Description = "Create a caption track from a time-coded subtitle file. New tracks default to draft. Requires OAuth.")]
    public Task<CaptionTrack> UploadCaptions([ActionParameter] UploadCaptionsInput input) => YouTubeClient.RunAsync<CaptionTrack>(async () =>
    {
        var api = Client;
        api.RequireOAuth();
        if (input.File == null)
            throw new PluginMisconfigurationException("Supply a time-coded caption File to upload.");
        if (input.File.Size > 100_000_000)
            throw new PluginMisconfigurationException("Caption files must not exceed 100 MB.");
        await using var stream = await fileManagementClient.DownloadAsync(input.File);
        return await new LocalizationActions(api).UploadCaptionsAsync(input, stream);
    });
    [Action("Update captions", Description = "Replace a caption file, change its draft status, or both. Omitted draft status is preserved. Requires OAuth.")]
    public Task<CaptionTrack> UpdateCaptions([ActionParameter] UpdateCaptionsInput input) => YouTubeClient.RunAsync<CaptionTrack>(async () =>
    {
        var api = Client;
        api.RequireOAuth();
        if (input.File?.Size > 100_000_000)
            throw new PluginMisconfigurationException("Caption files must not exceed 100 MB.");
        await using var stream = input.File == null ? null : await fileManagementClient.DownloadAsync(input.File);
        return await new LocalizationActions(api).UpdateCaptionsAsync(input, stream);
    });
}
