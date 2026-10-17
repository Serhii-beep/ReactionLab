using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

namespace ReactionLab.Catalog.Migrator;

internal sealed class SeedWorker(
    IServiceProvider services,
    IHostApplicationLifetime lifetime,
    ILogger<SeedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CatalogSeeding.SeedAsync(services, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Catalog seeding failed.");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
