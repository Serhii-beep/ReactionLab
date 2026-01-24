using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactionLab.Domain.Interfaces;
using ReactionLab.Infrastructure.Persistence;
using ReactionLab.Infrastructure.Persistence.Repositories;
using ReactionLab.Infrastructure.Persistence.Seeding;

namespace ReactionLab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReactionLabDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ReactionLabDbContext).Assembly.FullName)));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IElementRepository, ElementRepository>();
        services.AddScoped<IMoleculeRepository, MoleculeRepository>();
        services.AddScoped<IReactionRepository, ReactionRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IDataSeeder, ElementSeeder>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}