using System.Security.Cryptography;
using System.Text;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Catalog;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;

internal sealed class CuratedCatalogBuild : ICatalogBuild
{
    public const string BuildId = "curated";

    private static readonly string SourceDirectory = Path.Combine(AppContext.BaseDirectory, "catalog");

    private static readonly string[] Files = ["elements.jsonl", "substances.jsonl", "reactions.jsonl"];

    public string Id => BuildId;

    public async Task<string> ContentHashAsync(CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        foreach (var file in Files)
        {
            var path = Path.Combine(SourceDirectory, file);

            hash.AppendData(Encoding.UTF8.GetBytes(file));

            if (File.Exists(path))
            {
                hash.AppendData(await File.ReadAllBytesAsync(path, cancellationToken));
            }
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    public IAsyncEnumerable<ElementRecord> ElementsAsync(CancellationToken cancellationToken) =>
        CatalogJson.ReadLinesAsync<ElementRecord>(Path.Combine(SourceDirectory, Files[0]), cancellationToken);

    public IAsyncEnumerable<SubstanceRecord> SubstancesAsync(CancellationToken cancellationToken) =>
        CatalogJson.ReadLinesAsync<SubstanceRecord>(Path.Combine(SourceDirectory, Files[1]), cancellationToken);

    public IAsyncEnumerable<ReactionRecord> ReactionsAsync(CancellationToken cancellationToken) =>
        CatalogJson.ReadLinesAsync<ReactionRecord>(Path.Combine(SourceDirectory, Files[2]), cancellationToken);
}
