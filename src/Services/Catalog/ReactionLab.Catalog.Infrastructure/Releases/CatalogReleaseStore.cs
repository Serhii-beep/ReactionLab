using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ReactionLab.Catalog.Application.Common.Serialization;

namespace ReactionLab.Catalog.Infrastructure.Releases;

internal sealed class CatalogReleaseStore(BlobServiceClient blobs)
{
    public const string ContainerName = "catalog-releases";

    public const string ManifestName = "current.json";

    private const string JsonContentType = "application/json; charset=utf-8";

    private const string DocumentCacheControl = "public, max-age=31536000, immutable";

    private const string ManifestCacheControl = "public, max-age=60, stale-while-revalidate=300";

    private BlobContainerClient Container => blobs.GetBlobContainerClient(ContainerName);

    public async Task EnsureContainerAsync(CancellationToken cancellationToken) =>
        await Container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

    public async Task<bool> UploadDocumentAsync(string name, byte[] content, CancellationToken cancellationToken)
    {
        try
        {
            await Container.GetBlobClient(name).UploadAsync(
                BinaryData.FromBytes(content),
                UploadOptions(DocumentCacheControl, new BlobRequestConditions { IfNoneMatch = ETag.All }),
                cancellationToken);

            return true;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
        {
            return false;
        }
    }

    public async Task<string?> ManifestVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var manifest = await Container.GetBlobClient(ManifestName).DownloadContentAsync(cancellationToken);

            return manifest.Value.Content.ToObjectFromJson<CatalogManifest>(ResponseJson.Options)?.Version;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task UploadManifestAsync(CatalogManifest manifest, CancellationToken cancellationToken) =>
        await Container.GetBlobClient(ManifestName).UploadAsync(
            BinaryData.FromObjectAsJson(manifest, ResponseJson.Options),
            UploadOptions(ManifestCacheControl, conditions: null),
            cancellationToken);

    private static BlobUploadOptions UploadOptions(string cacheControl, BlobRequestConditions? conditions) => new()
    {
        HttpHeaders = new BlobHttpHeaders { ContentType = JsonContentType, CacheControl = cacheControl },
        Conditions = conditions
    };
}
