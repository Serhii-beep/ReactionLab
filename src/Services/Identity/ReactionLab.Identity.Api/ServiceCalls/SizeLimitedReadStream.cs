using Grpc.Core;

namespace ReactionLab.Identity.Api.ServiceCalls;

internal sealed class SizeLimitedReadStream(Stream source, int maximumBytes) : Stream
{
    private long _totalBytesRead;

    public override bool CanRead => source.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        CountBytesRead(source.Read(buffer, offset, count));

    public override int Read(Span<byte> buffer) => CountBytesRead(source.Read(buffer));

    public override void Flush()
    {

    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            source.Dispose();
        }

        base.Dispose(disposing);
    }

    private int CountBytesRead(int bytesRead)
    {
        _totalBytesRead += bytesRead;

        return _totalBytesRead > maximumBytes
            ? throw new RpcException(new Status(StatusCode.ResourceExhausted, "The decompressed message is too large."))
            : bytesRead;
    }
}
