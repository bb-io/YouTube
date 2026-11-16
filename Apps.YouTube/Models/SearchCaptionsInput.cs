using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class SearchCaptionsInput
{
    [Display("Video ID", Description = "With OAuth, choose a video from the connected channel, or enter or map Video ID directly.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = "";

    [Display("Language", Description = "Optional exact language tag. Suggestions show tracks on the selected video and respect Include drafts. Omit for all languages; direct tags remain accepted.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(CaptionLanguageDataHandler))]
    public string? Language { get; set; }

    [Display("Include drafts", Description = "Defaults to true.")]
    public bool? IncludeDrafts { get; set; }
}
