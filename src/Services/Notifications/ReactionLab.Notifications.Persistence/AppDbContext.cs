using Microsoft.EntityFrameworkCore;
using ReactionLab.Messaging.Consuming;

namespace ReactionLab.Notifications.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new ProcessedEventConfiguration());
        modelBuilder.ApplyConfiguration(new ErasureMarkerConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterConfiguration());
    }
}
