namespace Apps.YouTube;

internal sealed record TranscriptSelection(CaptionTrack? Track, string Status, string Message, bool Retry, string Reason);
