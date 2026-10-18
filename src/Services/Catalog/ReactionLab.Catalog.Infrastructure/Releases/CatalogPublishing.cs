using Microsoft.Extensions.DependencyInjection;

namespace ReactionLab.Catalog.Infrastructure.Releases;

public static class CatalogPublishing
{
    public static async Task PublishAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<CatalogReleasePublisher>().PublishAsync(cancellationToken);
    }
}
