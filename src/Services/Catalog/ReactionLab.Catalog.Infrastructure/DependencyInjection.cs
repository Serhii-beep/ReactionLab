using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Releases;
using ReactionLab.Catalog.Infrastructure.Persistence;
using ReactionLab.Catalog.Infrastructure.Persistence.Interceptors;
using ReactionLab.Catalog.Infrastructure.Persistence.Releases;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;
using ReactionLab.Catalog.Infrastructure.Releases;
using ReactionLab.Catalog.Infrastructure.Releases.Matching;
using StackExchange.Redis;

namespace ReactionLab.Catalog.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan SlowQueryThreshold = TimeSpan.FromMilliseconds(200);

    private const int PreparedStatementsPerConnection = 64;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(
            configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured."))
        {
            MaxAutoPrepare = PreparedStatementsPerConnection,
            AutoPrepareMinUsages = 2
        }.ConnectionString;

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

        services.AddDbContext<ReleaseReadContext>((sp, options) => options
            .UseNpgsql(connectionString, NpgsqlSetup.Configure)
            .AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>()));

        services.AddScoped<IReleaseReadContext>(sp => sp.GetRequiredService<ReleaseReadContext>());

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        services.AddSingleton<ICatalogSearch, TrigramCatalogSearch>();
        services.AddSingleton<IReleaseWordSearch, TrigramReleaseWordSearch>();
        services.AddSingleton<IReactionMatching, ReactantSignatureMatching>();
        services.AddSingleton<ISubstanceFiltering, ElementSymbolFiltering>();

        var redis = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                var parsed = ConfigurationOptions.Parse(redis);

                parsed.AbortOnConnectFail = false;
                parsed.ConnectTimeout = 200;
                parsed.ConnectRetry = 1;
                parsed.SyncTimeout = 200;

                options.ConfigurationOptions = parsed;
            });
        }

        services.AddHybridCache();

        return services;
    }

    public static IServiceCollection AddCatalogReleaseReads(this IServiceCollection services)
    {
        services.AddSingleton<CatalogReleaseRegistry>();
        services.AddSingleton<ICatalogReleases>(sp => sp.GetRequiredService<CatalogReleaseRegistry>());
        services.AddHostedService<CatalogReleaseRefresher>();
        services.AddHealthChecks().AddCheck<CatalogReleaseHealthCheck>("catalog-releases");

        return services;
    }

    public static IServiceCollection AddCatalogSeeding(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(CatalogImportOptions.SectionName);

        services.AddSingleton(Options.Create(new CatalogImportOptions
        {
            Build = section[nameof(CatalogImportOptions.Build)] ?? CuratedCatalogBuild.BuildId,
            AllowMassRetirement = bool.TryParse(section[nameof(CatalogImportOptions.AllowMassRetirement)], out var allow) && allow
        }));

        services.AddScoped<ICatalogBuild>(sp =>
        {
            var build = sp.GetRequiredService<IOptions<CatalogImportOptions>>().Value.Build;

            return string.Equals(build, CuratedCatalogBuild.BuildId, StringComparison.Ordinal)
                ? new CuratedCatalogBuild()
                : new StoredCatalogBuild(sp.GetRequiredService<BlobServiceClient>(), build);
        });

        services.AddScoped<CatalogImporter>();
        services.AddScoped<ReferenceSeeder>();

        return services;
    }

    public static IServiceCollection AddCatalogPublishing(this IServiceCollection services)
    {
        services.AddScoped<CatalogDocumentBuilder>();
        services.AddSingleton<CatalogReleaseStore>();
        services.AddSingleton<ReleaseSnapshotBuilder>();
        services.AddScoped<CatalogReleasePublisher>();

        return services;
    }
}
