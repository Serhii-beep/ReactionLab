using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Catalog;

namespace ReactionLab.CatalogBuilder;

internal sealed class CatalogBuildFiles
{
    private const string CompressedExtension = ".gz";

    private readonly string _directory;
    private readonly CatalogBuildManifest? _manifest;

    private CatalogBuildFiles(string directory, CatalogBuildManifest? manifest)
    {
        _directory = directory;
        _manifest = manifest;
    }

    public static async Task<CatalogBuildFiles> OpenAsync(string directory, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(directory, CatalogBuildManifest.FileName);

        if (!File.Exists(manifestPath))
        {
            return new CatalogBuildFiles(directory, null);
        }

        await using var stream = File.OpenRead(manifestPath);

        return new CatalogBuildFiles(
            directory,
            await JsonSerializer.DeserializeAsync<CatalogBuildManifest>(stream, CatalogJson.Options, cancellationToken));
    }

    public IAsyncEnumerable<ElementRecord> ElementsAsync(CancellationToken cancellationToken) =>
        ReadAsync<ElementRecord>(_manifest?.ElementShards ?? ["elements.jsonl"], cancellationToken);

    public IAsyncEnumerable<SubstanceRecord> SubstancesAsync(CancellationToken cancellationToken) =>
        ReadAsync<SubstanceRecord>(_manifest?.SubstanceShards ?? ["substances.jsonl"], cancellationToken);

    public IAsyncEnumerable<ReactionRecord> ReactionsAsync(CancellationToken cancellationToken) =>
        ReadAsync<ReactionRecord>(_manifest?.ReactionShards ?? ["reactions.jsonl"], cancellationToken);

    private async IAsyncEnumerable<T> ReadAsync<T>(
        IReadOnlyList<string> files,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            Stream content = File.OpenRead(Path.Combine(_directory, file));

            if (file.EndsWith(CompressedExtension, StringComparison.Ordinal))
            {
                content = new GZipStream(content, CompressionMode.Decompress);
            }

            await using (content)
            {
                using var reader = new StreamReader(content);

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
}
