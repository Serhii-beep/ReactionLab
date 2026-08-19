using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactionLab.Infrastructure.Persistence;

namespace ReactionLab.DbMigrator;

internal sealed class MigrationWorker(
    IServiceProvider services,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pending = (await context.Database.GetPendingMigrationsAsync(stoppingToken)).ToList();

            logger.LogInformation("Applying {PendingCount} database migration(s).", pending.Count);

            await context.Database.MigrateAsync(stoppingToken);

            logger.LogInformation("Database schema is up to date.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed.");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
