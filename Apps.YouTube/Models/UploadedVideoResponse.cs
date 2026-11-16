using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class UploadedVideoResponse
{
    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Video URL")]
    public string VideoUrl { get; set; } = "";

    [Display("Privacy status")]
    public string PrivacyStatus { get; set; } = "";

    [Display("Upload status")]
    public string UploadStatus { get; set; } = "";

    [Display("Warnings")]
    public List<string> Warnings { get; set; } = [];
}
