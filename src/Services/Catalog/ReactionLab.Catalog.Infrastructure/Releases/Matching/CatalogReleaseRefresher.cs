using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using ReactionLab.Catalog.Domain.Releases;
using ReactionLab.Catalog.Infrastructure.Persistence;

namespace ReactionLab.Catalog.Infrastructure.Releases.Matching;

internal sealed class CatalogReleaseRefresher(
    IServiceScopeFactory scopes,
    CatalogReleaseRegistry registry,
    TimeProvider timeProvider,
    ILogger<CatalogReleaseRefresher> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.ExecuteSqlAsync(
                $"DELETE FROM catalog_instances WHERE instance_id = {registry.InstanceId}",
                cancellationToken);
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not remove this instance's catalog heartbeat.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);

        do
        {
            try
            {
                await RefreshAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Refreshing the catalog releases failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = timeProvider.GetUtcNow();

        var releases = await context.CatalogReleases
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var current = registry.State;
        var heldByReleaseId = new Dictionary<Guid, HeldRelease>();

        foreach (var release in releases.Where(release => release.IsReadableAt(now)))
        {
            var id = release.Id.Value;
            var readableUntil = release.Status == CatalogReleaseStatus.Retired ? release.RetiredAt + CatalogRelease.GracePeriod : null;

            heldByReleaseId[id] = current.HeldByReleaseId.TryGetValue(id, out var existing)
                ? existing with { AnswerVersion = release.AnswerVersion, ReadableUntil = readableUntil }
                : new HeldRelease(await LoadIndexAsync(context, id, cancellationToken), release.AnswerVersion, readableUntil);
        }

        var ids = releases.Select(release => release.Id.Value).ToList();
        var active = NewestOf(releases.Where(release => release.Status == CatalogReleaseStatus.Active).Select(release => release.Id.Value));

        registry.Publish(new RegistryState(active, heldByReleaseId, ids.ToHashSet(), NewestOf(ids), now));

        Guid[] holding = [.. heldByReleaseId.Keys];

        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO catalog_instances (instance_id, held_releases, seen_at)
            VALUES ({registry.InstanceId}, {holding}, {now})
            ON CONFLICT (instance_id) DO UPDATE SET held_releases = excluded.held_releases, seen_at = excluded.seen_at
            """,
            cancellationToken);
    }

    private static Guid? NewestOf(IEnumerable<Guid> releaseIds) =>
        releaseIds.Aggregate(
            (Guid?)null,
            (newest, id) => newest is { } known && !CatalogReleaseRegistry.IsNewer(id, known) ? known : id);

    private async Task<ReactionMatchIndex> LoadIndexAsync(AppDbContext context, Guid releaseId, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();

        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = new NpgsqlCommand(
                """
                SELECT id, reactant_ids
                FROM release_reactions
                WHERE release_id = @release AND retired_at IS NULL
                ORDER BY rank
                """,
                connection);

            command.Parameters.AddWithValue("release", releaseId);
            command.CommandTimeout = 600;

            var rows = new List<(Guid, Guid[])>();

            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    rows.Add((reader.GetGuid(0), await reader.GetFieldValueAsync<Guid[]>(1, cancellationToken)));
                }
            }

            var index = ReactionMatchIndex.Build(rows);

            logger.LogInformation(
                "Loaded catalog release {Release} for matching: {Reactions} reaction(s), {Megabytes:F1} MB, in {Milliseconds} ms.",
                releaseId, index.Reactions, index.Bytes / 1048576.0, watch.ElapsedMilliseconds);

            return index;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
