using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class ApiRequestInput
{
    [Display("URL", Description = "HTTPS YouTube Data/Upload API v3 or YouTube Analytics reports URL. Authentication comes from the connection.")]
    public string Url { get; set; } = "";

    [Display("HTTP method")]
    [StaticDataSource(typeof(HttpMethodDataSource))]
    public string Method { get; set; } = "GET";

    [Display("Query parameters JSON", Description = "Advanced JSON object of query key/value pairs. Dedicated actions provide guided fields; this action preserves custom API expressions.")]
    public string? QueryJson { get; set; }

    [Display("Additional headers JSON", Description = "Authentication, routing and cookie headers cannot be overridden.")]
    public string? HeadersJson { get; set; }

    [Display("Body", Description = "Optional UTF-8 request body. For files use dedicated upload actions.")]
    public string? Body { get; set; }

    [Display("Content type")]
    public string? ContentType { get; set; }

    [Display("Stop on error", Description = "Defaults to true.")]
    public bool? StopOnError { get; set; }
}
