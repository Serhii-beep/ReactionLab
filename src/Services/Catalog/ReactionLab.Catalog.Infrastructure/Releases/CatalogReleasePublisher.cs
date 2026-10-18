using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReactionLab.Catalog.Application.Common.Serialization;
using ReactionLab.Catalog.Application.Features.Releases;
using ReactionLab.Catalog.Domain.Releases;
using ReactionLab.Catalog.Infrastructure.Persistence;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Releases;

internal sealed class CatalogReleasePublisher(
    AppDbContext context,
    CatalogDocumentBuilder builder,
    CatalogReleaseStore store,
    TimeProvider timeProvider,
    ILogger<CatalogReleasePublisher> logger)
{
    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        var documents = await BuildSnapshotAsync(cancellationToken);
        var version = ContentHash(Encoding.UTF8.GetBytes(
            string.Join(';', documents.Select(document => $"{document.Locale}={document.Name}"))));

        await store.EnsureContainerAsync(cancellationToken);

        foreach (var document in documents.DistinctBy(document => document.Name))
        {
            if (await store.UploadDocumentAsync(document.Name, document.Content, cancellationToken))
            {
                logger.LogInformation(
                    "Uploaded catalog document {Name}: {Bytes} bytes.",
                    document.Name, document.Content.Length);
            }
        }

        if (await store.ManifestVersionAsync(cancellationToken) != version)
        {
            await store.UploadManifestAsync(
                new CatalogManifest(version, documents.ToDictionary(document => document.Locale, document => document.Name)),
                cancellationToken);

            logger.LogInformation("Pointed the catalog manifest at release {Version}.", version);
        }

        var latestVersion = await context.CatalogReleases
            .AsNoTracking()
            .OrderByDescending(release => release.PublishedAt)
            .Select(release => release.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestVersion == version)
        {
            logger.LogInformation("Catalog release {Version} is current.", version);

            return;
        }

        context.CatalogReleases.Add(CatalogRelease.Publish(version, timeProvider.GetUtcNow()).Value);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Published catalog release {Version}.", version);
    }

    private async Task<List<LocaleDocument>> BuildSnapshotAsync(CancellationToken cancellationToken) =>
        await context.Database.CreateExecutionStrategy().ExecuteAsync(
            async token =>
            {
                await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);

                var documents = new List<LocaleDocument>();

                foreach (var locale in SupportedLocale.All.OrderBy(locale => locale.Code, StringComparer.Ordinal))
                {
                    var content = JsonSerializer.SerializeToUtf8Bytes(
                        await builder.BuildAsync(locale, token),
                        ResponseJson.Options);

                    documents.Add(new LocaleDocument(locale.Code, $"{ContentHash(content)}.json", content));
                }

                return documents;
            },
            cancellationToken);

    private static string ContentHash(byte[] content) =>
        Convert.ToHexStringLower(SHA256.HashData(content))[..CatalogRelease.VersionLength];

    private sealed record LocaleDocument(string Locale, string Name, byte[] Content);
}
