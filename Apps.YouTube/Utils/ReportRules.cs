using System.Text.RegularExpressions;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.YouTube;

internal static class ReportRules
{
    private static readonly (string Value, string Label)[] Metrics =
    [
        ("views", "Views"), ("engagedViews", "Engaged views"),
        ("estimatedMinutesWatched", "Watch time in minutes"),
        ("averageViewDuration", "Average view duration in seconds"),
        ("averageViewPercentage", "Average view percentage"),
        ("likes", "Likes"), ("comments", "Comments"), ("shares", "Shares"),
        ("subscribersGained", "Subscribers gained"), ("subscribersLost", "Subscribers lost"),
        ("estimatedRevenue", "Estimated revenue — requires monetary access"),
        ("estimatedAdRevenue", "Estimated ad revenue — requires monetary access")
    ];
    private static readonly HashSet<string> KnownMetrics = Metrics.Where(item => !item.Value.Contains(','))
        .Select(item => item.Value).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> RankedSortMetrics = new(StringComparer.Ordinal)
    {
        "views", "estimatedMinutesWatched", "subscribersGained", "subscribersLost", "estimatedRevenue"
    };

    public static IEnumerable<DataSourceItem> MetricOptions(ReportContext context, string? search)
    {
        var selected = context.Metrics?.Where(value => !string.IsNullOrWhiteSpace(value)).ToList() ?? [];
        return Filter(Metrics.Where(item =>
        {
            var combined = string.Join(",", selected.Append(item.Value));
            return Compatible(context, combined, Dimension(context.Dimensions))
                && SortAllowed(context.Sort, combined, Dimension(context.Dimensions), true);
        }), search);
    }

    public static string NormalizeMetrics(IEnumerable<string>? values)
    {
        var selected = values?.Select(value => value?.Trim() ?? "").ToList() ?? [];
        if (selected.Count == 0 || selected.Any(value => !Regex.IsMatch(value, "^[A-Za-z][A-Za-z0-9]*$")))
            throw new PluginMisconfigurationException("Select at least one Report metric. Enter each metric as a separate selection, or map a collection of metric names; comma-separated bundles are not supported.");
        return string.Join(",", selected.Distinct(StringComparer.Ordinal));
    }

    public static void ValidateSelection(ReportInput input, string metrics)
    {
        var dimension = Dimension(input.Dimensions);
        var context = new ReportContext
        {
            ReportType = input.ReportType, Metrics = input.Metrics, Dimensions = input.Dimensions,
            Filters = input.Filters, Sort = input.Sort, VideoId = input.VideoId, CountryCode = input.CountryCode
        };
        if ((string.IsNullOrWhiteSpace(input.ReportType) || input.ReportType == "channel")
            && Tokens(metrics).IsSubsetOf(KnownMetrics)
            && dimension is "none" or "day" or "month" or "country" or "video"
            && ParseFilters(input.Filters) != null
            && (!Compatible(context, metrics, dimension) || !SortAllowed(input.Sort, metrics, dimension, false)))
            throw new PluginMisconfigurationException("Report metrics, breakdown, Sort and filters are not compatible. Sort using a selected metric or the breakdown. By video requires a supported descending metric and no Video ID filter; By country cannot also filter Country. Clear incompatible choices or use a supported advanced report expression.");
    }

    public static IEnumerable<DataSourceItem> DimensionOptions(ReportContext context, string? search) =>
        Filter(new (string Value, string Label)[]
        {
            ("none", "Totals — no breakdown"), ("day", "By day"), ("month", "By month — both dates must be the first of a month"),
            ("country", "By country"), ("video", "By video — ranked; choose a descending Sort")
        }.Where(item => Compatible(context, MetricExpression(context.Metrics), item.Value)
            && SortAllowed(context.Sort, MetricExpression(context.Metrics), item.Value, true)), search);

    public static IEnumerable<DataSourceItem> SortOptions(ReportContext context, string? search)
    {
        var metrics = MetricExpression(context.Metrics);
        var dimension = Dimension(context.Dimensions);
        if (!Compatible(context, metrics, dimension) || dimension == "none")
            return [];
        var selected = Tokens(metrics);
        if (dimension == "video")
            return Filter(Metrics.Where(item => RankedSortMetrics.Contains(item.Value) && selected.Contains(item.Value))
                .Select(item => ("-" + item.Value, item.Label + " — highest first")), search);
        var options = new List<(string Value, string Label)>
        {
            (dimension, dimension == "country" ? "Country code — A to Z" : "Date — oldest first"),
            ("-" + dimension, dimension == "country" ? "Country code — Z to A" : "Date — newest first")
        };
        foreach (var item in Metrics.Where(item => !item.Value.Contains(',') && selected.Contains(item.Value)))
        {
            options.Add(("-" + item.Value, item.Label + " — highest first"));
            options.Add((item.Value, item.Label + " — lowest first"));
        }
        return Filter(options, search);
    }

    public static bool CanFilterCountry(ReportContext context) =>
        Dimension(context.Dimensions) != "country" && Compatible(context, MetricExpression(context.Metrics), Dimension(context.Dimensions));

    public static string? BuildFilters(ReportInput input)
    {
        if (string.IsNullOrWhiteSpace(input.VideoId) && string.IsNullOrWhiteSpace(input.CountryCode))
            return input.Filters;
        var context = new ReportContext
        {
            ReportType = input.ReportType, Metrics = input.Metrics, Dimensions = input.Dimensions,
            Filters = input.Filters, VideoId = input.VideoId, CountryCode = input.CountryCode
        };
        var dimension = Dimension(input.Dimensions);
        if (!Compatible(context, NormalizeMetrics(input.Metrics), dimension) || !SortAllowed(input.Sort, NormalizeMetrics(input.Metrics), dimension, false))
            throw new PluginMisconfigurationException("Video ID and Country helpers require a compatible channel report. Choose common channel metrics and a supported breakdown/Sort, or clear the helpers and use advanced Filters for custom or content-owner reports.");
        var filters = ParseFilters(input.Filters)!;
        if (!string.IsNullOrWhiteSpace(input.VideoId))
            AddFilter(filters, "video", YouTubeUrls.ParseVideoId(input.VideoId));
        if (!string.IsNullOrWhiteSpace(input.CountryCode))
        {
            var country = input.CountryCode.Trim().ToUpperInvariant();
            if (!Regex.IsMatch(country, "^[A-Z]{2}$"))
                throw new PluginMisconfigurationException("Choose a two-letter Country code, or clear Country to report on all countries.");
            AddFilter(filters, "country", country);
        }
        return string.Join(";", filters.Select(item => item.Key + "==" + item.Value));
    }

    public static string Dimension(string? value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();

    private static bool Compatible(ReportContext context, string metrics, string dimension)
    {
        if (!string.IsNullOrWhiteSpace(context.ReportType) && context.ReportType != "channel")
            return false;
        var selected = Tokens(metrics);
        if (selected.Count == 0 || !selected.IsSubsetOf(KnownMetrics) || dimension is not ("none" or "day" or "month" or "country" or "video"))
            return false;
        var filters = ParseFilters(context.Filters);
        if (filters == null)
            return false;
        var country = filters.ContainsKey("country") || !string.IsNullOrWhiteSpace(context.CountryCode);
        var video = filters.ContainsKey("video") || !string.IsNullOrWhiteSpace(context.VideoId);
        return (dimension != "country" || !country)
            && (dimension != "video" || !video && selected.Overlaps(RankedSortMetrics));
    }

    private static bool SortAllowed(string? expression, string metrics, string dimension, bool allowMissing)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return allowMissing || dimension != "video";
        var parts = expression.Split(',').Select(value => value.Trim()).ToList();
        var selected = Tokens(metrics);
        if (dimension == "video")
            return parts.Count == 1 && parts[0].StartsWith('-') && selected.Contains(parts[0][1..]) && RankedSortMetrics.Contains(parts[0][1..]);
        return parts.All(part =>
        {
            var key = part.StartsWith('-') ? part[1..] : part;
            return selected.Contains(key) || dimension != "none" && key == dimension;
        });
    }

    private static Dictionary<string, string>? ParseFilters(string? expression)
    {
        var filters = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(expression))
            return filters;
        foreach (var clause in expression.Split(';'))
        {
            var match = Regex.Match(clause.Trim(), "^(video|country)==([^;=]+)$");
            if (!match.Success || !filters.TryAdd(match.Groups[1].Value, match.Groups[2].Value.Trim()))
                return null;
        }
        return filters;
    }

    private static void AddFilter(Dictionary<string, string> filters, string name, string value)
    {
        if (filters.TryGetValue(name, out var existing) && !existing.Equals(value, name == "country" ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new PluginMisconfigurationException("The " + (name == "country" ? "Country" : "Video ID") + " helper conflicts with advanced Filters. Use matching values or clear one of the conflicting inputs.");
        filters[name] = value;
    }

    private static HashSet<string> Tokens(string? expression) =>
        (expression ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);

    private static string MetricExpression(IEnumerable<string>? values) => values == null || !values.Any() ? "views" : string.Join(",", values);

    private static IEnumerable<DataSourceItem> Filter(IEnumerable<(string Value, string Label)> options, string? search)
    {
        search = search?.Trim();
        return options.Where(item => string.IsNullOrWhiteSpace(search)
                || item.Value.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.Label.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Select(item => new DataSourceItem(item.Value, item.Label)).Take(50).ToList();
    }
}
