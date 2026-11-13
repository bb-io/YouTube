using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.YouTube;

[ActionList("Transcripts")]
public sealed class TranscriptActions(InvocationContext context, IFileManagementClient fileManagementClient) : Invocable(context)
{
    [Action("Get video transcript", Description = "Get caption-based text, timed segments and a WebVTT file for an authorized video. Select an exact language or Caption ID, or use deterministic source-language selection. Does not generate speech recognition.")]
    public Task<TranscriptResponse> GetVideoTranscript([ActionParameter] TranscriptInput input) =>
        YouTubeClient.RunAsync<TranscriptResponse>(async () =>
        {
            Client.RequireOAuth();
            var id = YouTubeUrls.ParseVideoId(input.VideoId);
            var requestedLanguage = string.IsNullOrWhiteSpace(input.Language) ? null : LocalizationActions.LanguageTag(input.Language);
            if (input.CaptionId != null && (input.CaptionId.Length > 500 || input.CaptionId.Any(char.IsControl)))
                throw new PluginMisconfigurationException("Choose or enter a valid Caption ID, or clear the override.");
            var videos = await Client.GetAsync<YouTubePage<VideoDto>>("videos", new() { ["part"] = "snippet", ["id"] = id });
            var video = videos.Items.SingleOrDefault() ?? throw new PluginMisconfigurationException("Video not found or inaccessible. Check Video ID and the OAuth connection.");
            var captions = new LocalizationActions(Client);
            var tracks = await captions.SearchCaptionsAsync(id);
            var selection = TranscriptSelector.Select(tracks, video, input);
            var response = new TranscriptResponse
            {
                VideoId = id, VideoUrl = "https://www.youtube.com/watch?v=" + id, Title = video.Snippet.Title,
                RequestedLanguage = requestedLanguage, Status = selection.Status, StatusMessage = selection.Message,
                RetryRecommended = selection.Retry, SelectionReason = selection.Reason,
                AvailableLanguages = tracks.Select(track => track.Language).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(language => language, StringComparer.OrdinalIgnoreCase).ToList()
            };
            var selected = selection.Track;
            if (selected == null)
                return response;
            response.CaptionId = selected.CaptionId;
            response.Language = selected.Language;
            response.TrackName = selected.Name;
            response.TrackKind = selected.TrackKind;
            response.IsDraft = selected.IsDraft;
            response.IsAutomatic = selected.TrackKind == "ASR";
            response.IsForced = selected.TrackKind == "forced";
            response.Warnings.Add("Caption-based text is not verified against audio and is not guaranteed to cover all speech.");
            if (selected.TrackKind == "ASR")
                response.Warnings.Add("Automatic speech-recognition text is preserved without correction.");
            if (selected.TrackKind == "forced")
                response.Warnings.Add("This explicit forced track may cover only selected dialogue.");
            if (selected.TrackKind is not ("standard" or "ASR" or "forced"))
                response.Warnings.Add("The explicit track has an unknown type; its coverage is unknown.");
            if (selection.Status != "ready")
                return response;
            await using var source = await captions.DownloadCaptionsAsync(selected.CaptionId, "vtt");
            var path = Path.Combine(Path.GetTempPath(), "blackbird-youtube-transcript-" + Guid.NewGuid().ToString("N") + ".vtt");
            await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 65_536, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
            await source.CopyToAsync(file);
            file.Position = 0;
            var segments = await WebVttTranscriptParser.ParseAsync(file);
            if (segments.Count == 0)
            {
                response.Status = "empty_transcript";
                response.StatusMessage = "The selected caption file contains no non-empty text cues.";
                return response;
            }
            file.Position = 0;
            var languageName = string.Concat(selected.Language.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_'));
            response.File = await fileManagementClient.UploadAsync(file, "text/vtt", "transcript-" + id + "-" + languageName + ".vtt");
            response.Segments = segments;
            response.Text = string.Join("\n", segments.Select(segment => segment.Text));
            response.TranscriptAvailable = true;
            response.Status = "available";
            response.StatusMessage = "Caption-based transcript returned. See the selected track metadata and coverage notes.";
            return response;
        });
}
