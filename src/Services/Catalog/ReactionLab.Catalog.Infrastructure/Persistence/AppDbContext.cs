using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Domain.Reference;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Element> Elements => Set<Element>();

    public DbSet<Substance> Substances => Set<Substance>();

    public DbSet<Reaction> Reactions => Set<Reaction>();

    public DbSet<ChemistryReference> ChemistryReferences => Set<ChemistryReference>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSnakeCaseNamingConvention();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.ApplyDomainConversions();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
