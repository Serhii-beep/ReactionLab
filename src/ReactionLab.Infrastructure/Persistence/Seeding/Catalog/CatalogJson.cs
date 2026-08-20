using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReactionLab.Infrastructure.Persistence.Seeding.Catalog;

public static class CatalogJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static async IAsyncEnumerable<T> ReadLinesAsync<T>(
        string path,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        using var reader = new StreamReader(path);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return JsonSerializer.Deserialize<T>(line, Options)!;
            }
        }
    }

    public static async Task WriteLinesAsync<T>(
        string path,
        IEnumerable<T> records,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var writer = new StreamWriter(path, append: false);

        foreach (var record in records)
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(record, Options).AsMemory(), cancellationToken);
        }
    }
}
