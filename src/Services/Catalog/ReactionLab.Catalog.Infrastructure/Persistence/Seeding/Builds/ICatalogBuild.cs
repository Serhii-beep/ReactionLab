using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Catalog;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;

internal interface ICatalogBuild
{
    string Id { get; }

    Task<string> ContentHashAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<ElementRecord> ElementsAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<SubstanceRecord> SubstancesAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<ReactionRecord> ReactionsAsync(CancellationToken cancellationToken);
}
