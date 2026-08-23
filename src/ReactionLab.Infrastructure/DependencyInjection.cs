using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Infrastructure.Persistence;
using ReactionLab.Infrastructure.Persistence.Interceptors;
using ReactionLab.Infrastructure.Persistence.Seeding;

namespace ReactionLab.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan SlowQueryThreshold = TimeSpan.FromMilliseconds(200);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ISaveChangesInterceptor, AuditInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, SearchProjectionInterceptor>();

        services.AddSingleton(sp => new SlowQueryInterceptor(
            sp.GetRequiredService<ILogger<SlowQueryInterceptor>>(),
            SlowQueryThreshold));

        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString, NpgsqlSetup.Configure)
            .AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>())
            .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        services.AddScoped<ICatalogSource, JsonCatalogSource>();
        services.AddScoped<CatalogSeeder>();

        services.AddSingleton<ICatalogSearch, TrigramCatalogSearch>();

        return services;
    }
}
