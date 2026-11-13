using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using HtmlAgilityPack;

namespace Apps.YouTube;

internal static class WebVttTranscriptParser
{
    private const int MaximumCueCharacters = 65_536;
    private const int MaximumSegments = 10_000;
    private const int MaximumTextCharacters = 250_000;
    private static readonly Regex Timing = new("^(?<start>(?:[0-9]+:)?[0-9]{2}:[0-9]{2}\\.[0-9]{3})[ \\t]+-->[ \\t]+(?<end>(?:[0-9]+:)?[0-9]{2}:[0-9]{2}\\.[0-9]{3})(?:[ \\t]+.*)?$", RegexOptions.CultureInvariant);

    public static async Task<List<TranscriptSegment>> ParseAsync(Stream stream)
    {
        try
        {
            using var reader = new CaptionLineReader(stream);
            var header = (await reader.ReadLineAsync())?.TrimStart('\uFEFF');
            if (header == null || !Regex.IsMatch(header, "^WEBVTT(?:[ \\t].*)?$", RegexOptions.CultureInvariant) || header.Contains("-->"))
                throw Invalid("The downloaded file has no valid WebVTT header.");
            string? line;
            while ((line = await reader.ReadLineAsync()) != null && !string.IsNullOrWhiteSpace(line))
            {
                if (line.Contains("X-TIMESTAMP-MAP", StringComparison.OrdinalIgnoreCase))
                    throw Invalid("Segmented timestamp maps are not supported by this transcript parser.");
                if (line.Contains("-->"))
                    throw Invalid("The WebVTT header is not separated from its cues.");
            }
            var segments = new List<TranscriptSegment>();
            var totalCharacters = 0;
            var previousStart = -1d;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var metadata = line is "STYLE" or "REGION" or "NOTE" || line.StartsWith("NOTE ", StringComparison.Ordinal) || line.StartsWith("NOTE\t", StringComparison.Ordinal);
                var block = new List<string>();
                var styleOrRegion = line is "STYLE" or "REGION";
                var characters = 0;
                do
                {
                    if (styleOrRegion && line.Contains("-->"))
                        throw Invalid("A style or region block contains ambiguous cue timing.");
                    if (!metadata)
                    {
                        characters += line.Length + 1;
                        if (characters > MaximumCueCharacters)
                            throw Invalid("A caption cue exceeds the transcript cue-size limit.");
                        block.Add(line);
                    }
                    line = await reader.ReadLineAsync();
                } while (line != null && !string.IsNullOrWhiteSpace(line));
                if (metadata)
                    continue;
                var timingIndex = block[0].Contains("-->", StringComparison.Ordinal) ? 0 : 1;
                if (timingIndex >= block.Count)
                    throw Invalid("A caption cue is missing its timing line.");
                var timing = Timing.Match(block[timingIndex]);
                if (!timing.Success)
                    throw Invalid("A caption cue has unsupported or invalid timing.");
                var start = Seconds(timing.Groups["start"].Value);
                var end = Seconds(timing.Groups["end"].Value);
                if (end <= start || start < previousStart)
                    throw Invalid("Caption cue times are reversed or out of order.");
                previousStart = start;
                var text = CleanText(string.Join("\n", block.Skip(timingIndex + 1)));
                if (text.Length == 0)
                    continue;
                totalCharacters += text.Length + (segments.Count > 0 ? 1 : 0);
                if (segments.Count >= MaximumSegments || totalCharacters > MaximumTextCharacters)
                    throw Invalid("The transcript exceeds 10,000 segments or 250,000 text characters. No partial transcript is returned.");
                segments.Add(new TranscriptSegment { StartSeconds = start, EndSeconds = end, Text = text });
            }
            return segments;
        }
        catch (DecoderFallbackException)
        {
            throw Invalid("The downloaded captions are not valid UTF-8 text.");
        }
    }

    private static double Seconds(string value)
    {
        var parts = value.Split(':');
        if (!int.TryParse(parts[^2], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) || minutes > 59
            || !double.TryParse(parts[^1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds) || seconds >= 60)
            throw Invalid("A caption timestamp is invalid.");
        var hours = 0;
        if (parts.Length == 3 && !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out hours))
            throw Invalid("A caption timestamp is too large.");
        return hours * 3600d + minutes * 60d + seconds;
    }

    private static string CleanText(string text)
    {
        text = Regex.Replace(text, "<(?:[0-9]+:)?[0-9]{2}:[0-9]{2}\\.[0-9]{3}>", "");
        var document = new HtmlDocument();
        document.LoadHtml(text);
        foreach (var node in document.DocumentNode.SelectNodes("//rt")?.ToArray() ?? [])
            node.Remove();
        foreach (var node in document.DocumentNode.SelectNodes("//br")?.ToArray() ?? [])
            node.ParentNode.ReplaceChild(document.CreateTextNode("\n"), node);
        var decoded = WebUtility.HtmlDecode(document.DocumentNode.InnerText).Replace('\u00A0', ' ');
        return string.Join("\n", decoded.Split('\n').Select(line => Regex.Replace(line.Trim(), "[ \\t]+", " ")).Where(line => line.Length > 0));
    }

    private static PluginApplicationException Invalid(string detail) =>
        new("YouTube captions could not be fully parsed. " + detail + " Use Download captions to inspect the subtitle file.");
}
