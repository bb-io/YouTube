using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.YouTube;
public sealed class TransferStream(Stream input, long limit, IDisposable? owner = null, byte[]? prefix = null) : Stream
{
    private long transferred;
    private int prefixOffset;
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.IsEmpty)
            return 0;
        int count;
        if (prefix != null && prefixOffset < prefix.Length)
        {
            count = Math.Min(buffer.Length, prefix.Length - prefixOffset);
            prefix.AsMemory(prefixOffset, count).CopyTo(buffer);
            prefixOffset += count;
        }
        else
        {
            try
            {
                count = await input.ReadAsync(buffer, cancellationToken);
            }
            catch (IOException)
            {
                throw new PluginApplicationException("The file transfer was interrupted. For writes, check the resource before retrying.");
            }
        }

        transferred += count;
        if (transferred > limit)
            throw new PluginMisconfigurationException($"The file exceeds the {limit / 1_000_000} MB limit.");
        if (count == 0 && transferred == 0)
            throw new PluginMisconfigurationException("The file is empty. Supply a non-empty file.");
        return count;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            input.Dispose();
            owner?.Dispose();
        }

        base.Dispose(disposing);
    }
}
