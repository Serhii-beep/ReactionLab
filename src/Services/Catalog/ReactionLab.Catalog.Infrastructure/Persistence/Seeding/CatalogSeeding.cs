using Microsoft.Extensions.DependencyInjection;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

public static class CatalogSeeding
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<CatalogImporter>().ImportAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<ReferenceSeeder>().SeedAsync(cancellationToken);
    }
}
