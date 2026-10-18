using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ReactionLab.Identity.Persistence;

public static class DependencyInjection
{
    private const int ConnectionsPerInstance = 32;

    private const int PreparedStatementsPerConnection = 64;

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(
            configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured."))
        {
            MaxPoolSize = ConnectionsPerInstance,
            MaxAutoPrepare = PreparedStatementsPerConnection,
            AutoPrepareMinUsages = 2
        }.ConnectionString;

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString, NpgsqlSetup.Configure));
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        return services;
    }
}
