using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactionLab.Infrastructure.Persistence;

namespace ReactionLab.DbMigrator;

internal sealed class MigrationWorker(
    IServiceProvider services,
    ILogger<MigrationWorker> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            logger.LogInformation("Applying {PendingCount} database migration(s).", pending.Count);

            await context.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Database schema is up to date.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed.");

            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
