using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactionLab.Catalog.Infrastructure.Releases;

namespace ReactionLab.Catalog.Migrator;

internal sealed class CatalogWorker(
    IServiceProvider services,
    IHostApplicationLifetime lifetime,
    ILogger<CatalogWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CatalogMaintenance.RunAsync(services, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Catalog seeding or publishing failed.");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
