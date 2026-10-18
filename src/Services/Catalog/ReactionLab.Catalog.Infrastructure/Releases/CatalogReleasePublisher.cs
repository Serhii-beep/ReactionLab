using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using ReactionLab.Catalog.Application.Common.Serialization;
using ReactionLab.Catalog.Application.Features.Releases;
using ReactionLab.Catalog.Domain.Releases;
using ReactionLab.Catalog.Infrastructure.Persistence;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Releases;

internal sealed class CatalogReleasePublisher(
    AppDbContext context,
    CatalogDocumentBuilder documents,
    CatalogReleaseStore store,
    ReleaseSnapshotBuilder snapshots,
    TimeProvider timeProvider,
    ILogger<CatalogReleasePublisher> logger)
{
    public static readonly TimeSpan InstanceTimeout = TimeSpan.FromSeconds(90);

    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan ActivationPoll = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan DropMargin = TimeSpan.FromMinutes(10);

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        await store.EnsureContainerAsync(cancellationToken);

        var boundedDocuments = await UploadBoundedDocumentsAsync(cancellationToken);
        var version = VersionOf(boundedDocuments, await ChangeMarkAsync(cancellationToken));

        var release = await context.CatalogReleases
            .Where(candidate => candidate.Status == CatalogReleaseStatus.Built || candidate.Status == CatalogReleaseStatus.Active)
            .OrderByDescending(candidate => candidate.PublishedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (release is null || release.Version != version)
        {
            await AbandonBuiltAsync(cancellationToken);
            release = await BuildAsync(boundedDocuments, cancellationToken);
        }

        if (release.Status == CatalogReleaseStatus.Built)
        {
            await ActivateAsync(release, boundedDocuments, cancellationToken);
        }
        else
        {
            await EnsureManifestAsync(release, boundedDocuments, cancellationToken);

            logger.LogInformation("Catalog release {Version} is current.", release.Version);
        }

        await DropExpiredAsync(cancellationToken);
    }

    private async Task<CatalogManifestDocuments> UploadBoundedDocumentsAsync(CancellationToken cancellationToken)
    {
        var elements = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var locale in SupportedLocale.All.OrderBy(locale => locale.Code, StringComparer.Ordinal))
        {
            var content = JsonSerializer.SerializeToUtf8Bytes(
                await documents.ElementsAsync(locale, cancellationToken),
                ResponseJson.Options);

            elements[locale.Code] = await UploadAsync(content, cancellationToken);
        }

        var reference = await UploadAsync(
            Encoding.UTF8.GetBytes(await documents.ReferenceAsync(cancellationToken)),
            cancellationToken);

        return new CatalogManifestDocuments(elements, reference);
    }

    private async Task<string> UploadAsync(byte[] content, CancellationToken cancellationToken)
    {
        var name = $"{ContentHash(content)}.json";

        if (await store.UploadDocumentAsync(name, content, cancellationToken))
        {
            logger.LogInformation("Uploaded catalog document {Name}: {Bytes} bytes.", name, content.Length);
        }

        return name;
    }

    private async Task<long> ChangeMarkAsync(CancellationToken cancellationToken) =>
        await context.Database
            .SqlQuery<long>($"""SELECT count(*) AS "Value" FROM catalog_changes""")
            .SingleAsync(cancellationToken);

    private async Task AbandonBuiltAsync(CancellationToken cancellationToken)
    {
        foreach (var abandoned in await context.CatalogReleases
            .Where(candidate => candidate.Status == CatalogReleaseStatus.Built)
            .ToListAsync(cancellationToken))
        {
            await DropSnapshotTablesAsync(abandoned.Id.Value, cancellationToken);
            EnsureSucceeded(abandoned.Abandon());
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Abandoned catalog release {Version}, which was built but never activated.", abandoned.Version);
        }
    }

    private async Task<CatalogRelease> BuildAsync(CatalogManifestDocuments boundedDocuments, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        var strategy = context.Database.CreateExecutionStrategy();

        var release = await strategy.ExecuteAsync(
            async token =>
            {
                context.ChangeTracker.Clear();

                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);

                var changeMark = await ChangeMarkAsync(token);
                var built = CatalogRelease.Build(
                    VersionOf(boundedDocuments, changeMark),
                    CatalogAnswers.Version,
                    changeMark,
                    timeProvider.GetUtcNow()).Value;

                await snapshots.BuildAsync(
                    built.Id.Value,
                    (NpgsqlConnection)context.Database.GetDbConnection(),
                    (NpgsqlTransaction)transaction.GetDbTransaction(),
                    token);

                context.CatalogReleases.Add(built);
                await context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                return built;
            },
            cancellationToken);

        logger.LogInformation(
            "Built catalog release {Version} ({Release}) in {Seconds:F1} s.",
            release.Version, release.Id, watch.Elapsed.TotalSeconds);

        return release;
    }

    private async Task ActivateAsync(CatalogRelease release, CatalogManifestDocuments boundedDocuments, CancellationToken cancellationToken)
    {
        var deadline = timeProvider.GetUtcNow() + ActivationTimeout;

        while (await LaggingInstancesAsync(release.Id.Value, cancellationToken) is var lagging and > 0)
        {
            if (timeProvider.GetUtcNow() > deadline)
            {
                logger.LogCritical(
                    "Catalog release {Version} is built, but {Count} catalog instance(s) haven't loaded it after {Minutes} minutes. " +
                    "It stays inactive until the next publish.",
                    release.Version, lagging, ActivationTimeout.TotalMinutes);

                return;
            }

            logger.LogInformation("Waiting for {Count} catalog instance(s) to load release {Version}.", lagging, release.Version);

            await Task.Delay(ActivationPoll, timeProvider, cancellationToken);
        }

        await store.UploadManifestAsync(new CatalogManifest(release.Id.Value, release.Version, boundedDocuments), cancellationToken);

        var now = timeProvider.GetUtcNow();

        foreach (var previous in await context.CatalogReleases
            .Where(candidate => candidate.Status == CatalogReleaseStatus.Active)
            .ToListAsync(cancellationToken))
        {
            EnsureSucceeded(previous.Retire(now));
        }

        EnsureSucceeded(release.Activate(now));
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Pointed the catalog manifest at release {Version}.", release.Version);
    }

    private async Task<int> LaggingInstancesAsync(Guid releaseId, CancellationToken cancellationToken)
    {
        var since = timeProvider.GetUtcNow() - InstanceTimeout;

        return await context.Database
            .SqlQuery<int>($"""
                SELECT count(*)::integer AS "Value"
                FROM catalog_instances
                WHERE seen_at > {since} AND NOT ({releaseId} = ANY (held_releases))
                """)
            .SingleAsync(cancellationToken);
    }

    private async Task EnsureManifestAsync(CatalogRelease release, CatalogManifestDocuments boundedDocuments, CancellationToken cancellationToken)
    {
        var manifest = new CatalogManifest(release.Id.Value, release.Version, boundedDocuments);
        var stored = await store.ManifestAsync(cancellationToken);

        if (stored is null
            || stored.Release != manifest.Release
            || stored.Documents.Reference != boundedDocuments.Reference
            || !stored.Documents.Elements.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .SequenceEqual(boundedDocuments.Elements.OrderBy(pair => pair.Key, StringComparer.Ordinal)))
        {
            await store.UploadManifestAsync(manifest, cancellationToken);

            logger.LogInformation("Pointed the catalog manifest at release {Version}.", release.Version);
        }
    }

    private async Task DropExpiredAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - CatalogRelease.GracePeriod - DropMargin;
        var expired = await context.CatalogReleases
            .Where(candidate => candidate.Status == CatalogReleaseStatus.Retired && candidate.RetiredAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var release in expired)
        {
            await DropSnapshotTablesAsync(release.Id.Value, cancellationToken);
            EnsureSucceeded(release.DropSnapshot());
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Dropped the snapshot of catalog release {Version}.", release.Version);
        }
    }

    private async Task DropSnapshotTablesAsync(Guid releaseId, CancellationToken cancellationToken)
    {
        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await ReleaseSnapshotBuilder.DropAsync(releaseId, (NpgsqlConnection)context.Database.GetDbConnection(), cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static void EnsureSucceeded(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"{result.Error.Code}: {result.Error.Description}");
        }
    }

    private static string VersionOf(CatalogManifestDocuments boundedDocuments, long changeMark)
    {
        var description = string.Join(
            ';',
            [
                $"answers={CatalogAnswers.Version}",
                $"changes={changeMark}",
                $"reference={boundedDocuments.Reference}",
                .. boundedDocuments.Elements.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}")
            ]);

        return ContentHash(Encoding.UTF8.GetBytes(description));
    }

    private static string ContentHash(byte[] content) =>
        Convert.ToHexStringLower(SHA256.HashData(content))[..CatalogRelease.VersionLength];
}
