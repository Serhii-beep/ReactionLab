using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReactionLabDbContext>();

        _logger.LogInformation("Applying any pending migrations...");
        await context.Database.MigrateAsync(cancellationToken);

        var seeders = scope.ServiceProvider.GetServices<IDataSeeder>()
            .OrderBy(s => s.Order);

        foreach (var seeder in seeders)
        {
            var seederName = seeder.GetType().Name;
            _logger.LogInformation("Running seeder: {SeederName}", seederName);

            try
            {
                await seeder.SeedAsync(cancellationToken);
                _logger.LogInformation("Completed seeder: {SeederName}", seederName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running seeder: {SeederName}", seederName);
                throw;
            }
        }

        _logger.LogInformation("Database seeding completed.");
    }
}
