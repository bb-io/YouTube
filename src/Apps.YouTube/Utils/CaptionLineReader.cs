using System.Text;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.YouTube;

internal sealed class CaptionLineReader(Stream stream) : IDisposable
{
    private const int MaximumLineCharacters = 16_384;
    private readonly StreamReader reader = new(stream, new UTF8Encoding(false, true), false, 4096, true);
    private readonly char[] buffer = new char[4096];
    private int offset;
    private int length;
    private bool skipLineFeed;

    public async Task<string?> ReadLineAsync()
    {
        var line = new StringBuilder();
        while (true)
        {
            if (offset == length)
            {
                length = await reader.ReadAsync(buffer.AsMemory());
                offset = 0;
                if (length == 0)
                    return line.Length == 0 ? null : line.ToString();
            }
            if (skipLineFeed)
            {
                skipLineFeed = false;
                if (buffer[offset] == '\n')
                {
                    offset++;
                    if (offset == length)
                        continue;
                }
            }
            var relative = buffer.AsSpan(offset, length - offset).IndexOfAny('\r', '\n');
            var end = relative < 0 ? -1 : offset + relative;
            var count = (end < 0 ? length : end) - offset;
            if (line.Length + count > MaximumLineCharacters)
                throw new PluginApplicationException("The caption file exceeds the transcript line-length limit. Use Download captions to retrieve the subtitle file.");
            line.Append(buffer, offset, count);
            offset += count;
            if (end >= 0)
            {
                skipLineFeed = buffer[offset] == '\r';
                offset++;
                return line.ToString();
            }
        }
    }

    public void Dispose() => reader.Dispose();
}
