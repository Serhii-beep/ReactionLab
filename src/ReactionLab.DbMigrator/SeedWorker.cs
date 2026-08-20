using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactionLab.Infrastructure.Persistence.Seeding;

namespace ReactionLab.DbMigrator;

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
