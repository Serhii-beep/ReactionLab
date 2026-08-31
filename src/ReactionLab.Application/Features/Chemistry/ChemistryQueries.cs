using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Reference;

namespace ReactionLab.Application.Features.Chemistry;

internal static class ChemistryQueries
{
    public static async Task<string> DocumentAsync(
        IQueryable<ChemistryReference> references,
        CancellationToken cancellationToken)
    {
        var rows = await references
            .OrderBy(reference => reference.Key)
            .Select(reference => new { reference.Key, reference.Payload })
            .ToListAsync(cancellationToken);

        using var buffer = new MemoryStream();

        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();

        foreach (var row in rows)
        {
            writer.WritePropertyName(row.Key.Value);

            using var payload = JsonDocument.Parse(row.Payload);

            payload.RootElement.WriteTo(writer);
        }

        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
