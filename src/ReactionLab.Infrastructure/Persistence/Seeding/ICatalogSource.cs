namespace ReactionLab.Infrastructure.Persistence.Seeding;

internal interface ICatalogSource
{
    string Name { get; }

    Task<CatalogBatch> LoadAsync(CancellationToken cancellationToken);
}
