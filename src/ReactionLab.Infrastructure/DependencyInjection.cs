using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactionLab.Application.Interfaces;
using ReactionLab.Domain.Interfaces;
using ReactionLab.Infrastructure.Caching;
using ReactionLab.Infrastructure.Persistence;
using ReactionLab.Infrastructure.Persistence.Repositories;
using ReactionLab.Infrastructure.Persistence.Seeding;
using StackExchange.Redis;

namespace ReactionLab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. Set it with: " +
                "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<value>\" --project src/ReactionLab.API");

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException(
                "Connection string 'Redis' is not configured. Set it with: " +
                "dotnet user-secrets set \"ConnectionStrings:Redis\" \"<value>\" --project src/ReactionLab.API");

        services.AddDbContext<ReactionLabDbContext>(options =>
            options.UseNpgsql(
                postgresConnectionString,
                b => b.MigrationsAssembly(typeof(ReactionLabDbContext).Assembly.FullName)));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IElementRepository, ElementRepository>();
        services.AddScoped<IMoleculeRepository, MoleculeRepository>();
        services.AddScoped<IReactionRepository, ReactionRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IDataSeeder, ElementSeeder>();
        services.AddScoped<IDataSeeder, MoleculeSeeder>();
        services.AddScoped<IDataSeeder, ReactionSeeder>();
        services.AddScoped<DatabaseSeeder>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
