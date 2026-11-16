using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class ApiRequestResponse
{
    [Display("Status code")]
    public int StatusCode { get; set; }

    [Display("Body")]
    public string Body { get; set; } = "";

    [Display("Headers JSON")]
    public string HeadersJson { get; set; } = "";
}
