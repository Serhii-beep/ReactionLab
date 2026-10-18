using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using ReactionLab.Catalog.Infrastructure.Persistence;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

namespace ReactionLab.Catalog.Infrastructure.Releases;

public static class CatalogMaintenance
{
    private const long MaintenanceLockKey = 0x63617461_6C6F6731;

    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(CatalogMaintenance));
        string connectionString;

        await using (var scope = services.CreateAsyncScope())
        {
            connectionString = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString()
                ?? throw new InvalidOperationException("The catalog database has no connection string.");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (await ExecuteLockAsync(connection, "SELECT pg_try_advisory_lock(@key)", cancellationToken) is not true)
        {
            logger.LogInformation("Another catalog import or publish is running; waiting for it to finish.");

            await ExecuteLockAsync(connection, "SELECT pg_advisory_lock(@key)", cancellationToken);
        }

        try
        {
            await CatalogSeeding.SeedAsync(services, cancellationToken);
            await CatalogPublishing.PublishAsync(services, cancellationToken);
        }
        finally
        {
            await ExecuteLockAsync(connection, "SELECT pg_advisory_unlock(@key)", CancellationToken.None);
        }
    }

    private static async Task<object?> ExecuteLockAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 0 };

        command.Parameters.AddWithValue("key", MaintenanceLockKey);

        return await command.ExecuteScalarAsync(cancellationToken);
    }
}
