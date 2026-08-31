using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReactionLab.Domain.Reference;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

internal sealed class ReferenceSeeder(AppDbContext context, ILogger<ReferenceSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "reference");

        if (!Directory.Exists(directory))
        {
            logger.LogWarning("No reference directory at {Directory}.", directory);

            return;
        }

        var stored = await context.ChemistryReferences.ToListAsync(cancellationToken);
        var byKey = stored.ToDictionary(reference => reference.Key.Value, StringComparer.Ordinal);
        var added = 0;
        var replaced = 0;
        var unchanged = 0;

        foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var key = ReferenceKey.Create(name);

            if (key.IsFailure)
            {
                logger.LogWarning("Skipping {File}: {Code} - {Description}", name, key.Error.Code, key.Error.Description);

                continue;
            }

            var payload = (await File.ReadAllTextAsync(file, cancellationToken)).Trim();

            if (!IsJsonObject(payload))
            {
                logger.LogWarning("Skipping {File}: the payload is not a JSON object.", name);

                continue;
            }

            if (byKey.TryGetValue(key.Value.Value, out var existing))
            {
                if (string.Equals(existing.Payload, payload, StringComparison.Ordinal))
                {
                    unchanged++;

                    continue;
                }

                var outcome = existing.Replace(payload);

                if (outcome.IsFailure)
                {
                    logger.LogWarning("Skipping {File}: {Code} - {Description}", name, outcome.Error.Code, outcome.Error.Description);

                    continue;
                }

                replaced++;

                continue;
            }

            var created = ChemistryReference.Create(key.Value, payload);

            if (created.IsFailure)
            {
                logger.LogWarning("Skipping {File}: {Code} - {Description}", name, created.Error.Code, created.Error.Description);

                continue;
            }

            context.ChemistryReferences.Add(created.Value);
            added++;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reference data: {Added} added, {Replaced} replaced, {Unchanged} unchanged.", added, replaced, unchanged);
    }

    private static bool IsJsonObject(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);

            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
