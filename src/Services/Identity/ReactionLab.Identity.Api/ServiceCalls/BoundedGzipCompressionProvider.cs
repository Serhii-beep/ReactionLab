using System.IO.Compression;
using Grpc.Net.Compression;

namespace ReactionLab.Identity.Api.ServiceCalls;

internal sealed class BoundedGzipCompressionProvider(int maximumMessageBytes) : ICompressionProvider
{
    public string EncodingName => "gzip";

    public Stream CreateCompressionStream(Stream stream, CompressionLevel? compressionLevel) =>
        new GZipStream(stream, compressionLevel ?? CompressionLevel.Fastest, leaveOpen: true);

    public Stream CreateDecompressionStream(Stream stream) =>
        new SizeLimitedReadStream(new GZipStream(stream, CompressionMode.Decompress), maximumMessageBytes);
}
