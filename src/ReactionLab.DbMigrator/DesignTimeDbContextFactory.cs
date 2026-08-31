using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ReactionLab.Infrastructure.Persistence;

namespace ReactionLab.DbMigrator;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DesignTimePlaceholder =
        "Host=localhost;Database=reactionlab;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DesignTimePlaceholder;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, NpgsqlSetup.Configure)
            .Options;

        return new AppDbContext(options);
    }
}
