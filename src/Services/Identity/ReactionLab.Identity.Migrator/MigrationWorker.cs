using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactionLab.Identity.Persistence;

namespace ReactionLab.Identity.Migrator;

internal sealed class MigrationWorker(
    IServiceProvider services,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    private const int CommandTimeoutSeconds = 3600;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Database.SetCommandTimeout(CommandTimeoutSeconds);

            var pending = (await context.Database.GetPendingMigrationsAsync(stoppingToken)).ToList();

            logger.LogInformation("Applying {PendingCount} database migration(s).", pending.Count);

            await context.Database.MigrateAsync(stoppingToken);

            logger.LogInformation("Database schema is up to date.");

            await scope.ServiceProvider.GetRequiredService<OpenIdRegistration>().RegisterAsync(stoppingToken);
            await scope.ServiceProvider.GetRequiredService<PlatformAdminSeeding>().SeedAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration, OpenID registration or platform admin seeding failed.");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
