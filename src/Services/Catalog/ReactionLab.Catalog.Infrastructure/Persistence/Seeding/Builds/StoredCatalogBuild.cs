using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure.Storage.Blobs;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Catalog;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;

internal sealed class StoredCatalogBuild(BlobServiceClient blobs, string id) : ICatalogBuild
{
    private CatalogBuildManifest? _manifest;

    public string Id => id;

    private BlobContainerClient Container => blobs.GetBlobContainerClient(CatalogBuildManifest.ContainerName);

    public async Task<string> ContentHashAsync(CancellationToken cancellationToken) =>
        (await ManifestAsync(cancellationToken)).ContentHash;

    public IAsyncEnumerable<ElementRecord> ElementsAsync(CancellationToken cancellationToken) =>
        ReadShardsAsync<ElementRecord>(manifest => manifest.ElementShards, cancellationToken);

    public IAsyncEnumerable<SubstanceRecord> SubstancesAsync(CancellationToken cancellationToken) =>
        ReadShardsAsync<SubstanceRecord>(manifest => manifest.SubstanceShards, cancellationToken);

    public IAsyncEnumerable<ReactionRecord> ReactionsAsync(CancellationToken cancellationToken) =>
        ReadShardsAsync<ReactionRecord>(manifest => manifest.ReactionShards, cancellationToken);

    private async Task<CatalogBuildManifest> ManifestAsync(CancellationToken cancellationToken)
    {
        if (_manifest is null)
        {
            var content = await Container.GetBlobClient($"{id}/{CatalogBuildManifest.FileName}")
                .DownloadContentAsync(cancellationToken);

            _manifest = content.Value.Content.ToObjectFromJson<CatalogBuildManifest>(CatalogJson.Options)
                ?? throw new InvalidOperationException($"Catalog build {id} has an empty manifest.");
        }

        return _manifest;
    }

    private async IAsyncEnumerable<T> ReadShardsAsync<T>(
        Func<CatalogBuildManifest, IReadOnlyList<string>> shardsOf,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var manifest = await ManifestAsync(cancellationToken);

        foreach (var shard in shardsOf(manifest))
        {
            await using var stream = await Container.GetBlobClient($"{id}/{shard}").OpenReadAsync(cancellationToken: cancellationToken);
            await using var unzipped = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(unzipped);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    yield return JsonSerializer.Deserialize<T>(line, CatalogJson.Options)!;
                }
            }
        }
    }
}
