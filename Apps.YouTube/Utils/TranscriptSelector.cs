using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.YouTube;

internal static class TranscriptSelector
{
    public static TranscriptSelection Select(List<CaptionTrack> tracks, VideoDto video, TranscriptInput input)
    {
        var language = string.IsNullOrWhiteSpace(input.Language) ? null : LocalizationActions.LanguageTag(input.Language);
        if (!string.IsNullOrWhiteSpace(input.CaptionId))
        {
            var chosen = tracks.SingleOrDefault(track => track.CaptionId == input.CaptionId.Trim())
                ?? throw new PluginMisconfigurationException("Caption ID was not found on this video. Refresh caption suggestions or clear the override.");
            if (language != null && !chosen.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                throw new PluginMisconfigurationException("Caption ID and Language refer to different languages. Choose matching values or clear one of them.");
            if (chosen.IsDraft && input.IncludeDrafts != true)
                throw new PluginMisconfigurationException("This Caption ID is a draft. Enable Include drafts or choose a non-draft track.");
            if (chosen.TrackKind == "ASR" && input.AllowAutomaticCaptions != true)
                throw new PluginMisconfigurationException("This Caption ID is automatic. Enable Allow automatic captions or choose a standard track.");
            return Availability(chosen, "caption_id_override");
        }
        if (tracks.Count == 0)
            return new(null, "no_captions", "No caption tracks are available yet. A later attempt may help, but captions are not guaranteed to appear.", true, "");
        var reason = "requested_language";
        if (language == null)
        {
            if (!string.IsNullOrWhiteSpace(video.Snippet.DefaultAudioLanguage))
            {
                language = video.Snippet.DefaultAudioLanguage;
                reason = "declared_audio_language";
            }
            else if (!string.IsNullOrWhiteSpace(video.Snippet.DefaultLanguage))
            {
                language = video.Snippet.DefaultLanguage;
                reason = "declared_metadata_language";
            }
            else
            {
                var languages = tracks.Where(track => Eligible(track, input)).Select(track => track.Language)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (languages.Count > 1)
                    return new(null, "language_required", "Several eligible languages are available and no source language is declared. Select Language or Caption ID explicitly.", false, "");
                if (languages.Count == 0)
                    return new(null, "no_eligible_track", "No automatic-selection candidate matches the draft/automatic settings. Forced tracks require an explicit Caption ID.", false, "");
                language = languages[0];
                reason = "sole_eligible_language";
            }
        }
        var matching = tracks.Where(track => track.Language.Equals(language, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matching.Count == 0)
            return new(null, "language_unavailable", "No exact caption language matches " + language + ". Choose an available language; no language or regional fallback was applied.", false, reason);
        var eligible = matching.Where(track => Eligible(track, input)).ToList();
        if (eligible.Count == 0)
            return new(null, "no_eligible_track", "Matching tracks are excluded by draft/automatic settings or track type. Enable the appropriate setting or choose Caption ID explicitly.", false, reason);
        var ready = Ordered(eligible.Where(track => track.Status == "serving")).FirstOrDefault();
        if (ready != null)
            return Availability(ready, reason);
        var processing = Ordered(eligible.Where(track => track.Status == "syncing")).FirstOrDefault();
        return Availability(processing ?? Ordered(eligible).First(), reason);
    }

    public static bool Eligible(CaptionTrack track, TranscriptInput input) =>
        (!track.IsDraft || input.IncludeDrafts == true)
        && (track.TrackKind == "standard" || track.TrackKind == "ASR" && input.AllowAutomaticCaptions == true);

    private static IOrderedEnumerable<CaptionTrack> Ordered(IEnumerable<CaptionTrack> tracks) =>
        tracks.OrderBy(track => track.TrackKind == "standard" ? 0 : 1)
            .ThenBy(track => track.IsDraft ? 1 : 0).ThenBy(track => track.CaptionId, StringComparer.Ordinal);

    private static TranscriptSelection Availability(CaptionTrack track, string reason) => track.Status switch
    {
        "serving" => new(track, "ready", "The selected caption track is ready to download.", false, reason),
        "syncing" => new(track, "captions_processing", "The selected caption track is processing. Retry later.", true, reason),
        "failed" => new(track, "captions_failed", "YouTube reports that the selected caption track failed processing. Repair or replace the track.", false, reason),
        _ => new(track, "caption_not_ready", "The selected track is not in a known ready state. Check it in YouTube or retry later.", true, reason)
    };
}
