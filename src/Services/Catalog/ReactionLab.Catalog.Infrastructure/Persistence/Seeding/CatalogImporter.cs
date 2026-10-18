using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

internal sealed class CatalogImporter(
    AppDbContext context,
    ICatalogBuild build,
    IOptions<CatalogImportOptions> options,
    TimeProvider timeProvider,
    ILogger<CatalogImporter> logger)
{
    private const int ReportedRejections = 20;

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        var contentHash = await build.ContentHashAsync(cancellationToken);

        if (await IsImportedAsync(contentHash, cancellationToken))
        {
            logger.LogInformation("Catalog build {Build} ({Hash}) is already imported.", build.Id, contentHash[..12]);

            return;
        }

        var watch = Stopwatch.StartNew();
        var strategy = context.Database.CreateExecutionStrategy();

        var (elements, counts, rejections) = await strategy.ExecuteAsync(
            async token =>
            {
                context.ChangeTracker.Clear();

                var mapper = new CatalogRecordMapper();

                await using var transaction = await context.Database.BeginTransactionAsync(token);

                var seededElements = await SeedElementsAsync(mapper, token);

                var loader = new BulkCatalogLoader(
                    (NpgsqlConnection)context.Database.GetDbConnection(),
                    (NpgsqlTransaction)transaction.GetDbTransaction(),
                    timeProvider.GetUtcNow(),
                    logger);

                var loaded = await loader.LoadAsync(build, mapper, options.Value.AllowMassRetirement, token);

                await RecordAsync(contentHash, loaded, token);
                await transaction.CommitAsync(token);

                return (seededElements, loaded, mapper.Rejections.ToList());
            },
            cancellationToken);

        foreach (var rejection in rejections.Take(ReportedRejections))
        {
            logger.LogWarning("Rejected from {Build}: {Rejection}", build.Id, rejection);
        }

        if (rejections.Count > ReportedRejections)
        {
            logger.LogWarning("Rejected {Count} more record(s) from {Build}.", rejections.Count - ReportedRejections, build.Id);
        }

        logger.LogInformation(
            "Imported catalog build {Build} in {Seconds:F1} s: {Elements} element(s) added; {Substances} substance(s) staged, " +
            "{RekeyedSubstances} re-keyed, {WrittenSubstances} written, {RetiredSubstances} retired; {Reactions} reaction(s) staged, " +
            "{WrittenReactions} written, {RetiredReactions} retired.",
            build.Id, watch.Elapsed.TotalSeconds, elements,
            counts.StagedSubstances, counts.RekeyedSubstances, counts.WrittenSubstances, counts.RetiredSubstances,
            counts.StagedReactions, counts.WrittenReactions, counts.RetiredReactions);
    }

    private async Task<bool> IsImportedAsync(string contentHash, CancellationToken cancellationToken) =>
        await context.Database
            .SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1 FROM catalog_imports
                    WHERE build_id = {build.Id} AND content_hash = {contentHash}) AS "Value"
                """)
            .SingleAsync(cancellationToken);

    private async Task RecordAsync(string contentHash, ImportCounts counts, CancellationToken cancellationToken) =>
        await context.Database.ExecuteSqlAsync($"""
            INSERT INTO catalog_imports (build_id, content_hash, imported_at, substances, reactions, retired_substances, retired_reactions)
            VALUES ({build.Id}, {contentHash}, {timeProvider.GetUtcNow()}, {counts.StagedSubstances}, {counts.StagedReactions},
                {counts.RetiredSubstances}, {counts.RetiredReactions})
            ON CONFLICT (build_id) DO UPDATE SET
                content_hash = excluded.content_hash,
                imported_at = excluded.imported_at,
                substances = excluded.substances,
                reactions = excluded.reactions,
                retired_substances = excluded.retired_substances,
                retired_reactions = excluded.retired_reactions
            """, cancellationToken);

    private async Task<int> SeedElementsAsync(CatalogRecordMapper mapper, CancellationToken cancellationToken)
    {
        var existing = await context.Elements.ToDictionaryAsync(
            element => element.Symbol.Value, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var missing = new List<Element>();

        await foreach (var record in build.ElementsAsync(cancellationToken))
        {
            if (mapper.MapElement(record) is not { } element)
            {
                continue;
            }

            if (!existing.TryGetValue(element.Symbol.Value, out var known))
            {
                missing.Add(element);
            }
            else if (known.Radii is null && element.Radii is not null)
            {
                var updated = known.UpdateAppearance(known.DisplayColor, element.Radii);

                if (updated.IsFailure)
                {
                    logger.LogWarning("Skipping radii for {Symbol}: {Error}", known.Symbol.Value, updated.Error);
                }
            }
        }

        context.Elements.AddRange(missing);
        await context.SaveChangesAsync(cancellationToken);

        return missing.Count;
    }
}
