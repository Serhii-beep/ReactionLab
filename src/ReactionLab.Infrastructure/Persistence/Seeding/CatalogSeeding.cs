using Microsoft.Extensions.DependencyInjection;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

public static class CatalogSeeding
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<ReferenceSeeder>().SeedAsync(cancellationToken);
    }
}
