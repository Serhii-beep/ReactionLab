using Microsoft.EntityFrameworkCore;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence;

public class ReactionLabDbContext : DbContext
{
    public ReactionLabDbContext(DbContextOptions<ReactionLabDbContext> options)
        : base(options)
    {

    }

    public DbSet<Element> Elements => Set<Element>();

    public DbSet<Molecule> Molecules => Set<Molecule>();

    public DbSet<MoleculeElement> MoleculeElements => Set<MoleculeElement>();

    public DbSet<Bond> Bonds => Set<Bond>();

    public DbSet<Reaction> Reactions => Set<Reaction>();

    public DbSet<ReactionParticipant> ReactionParticipants => Set<ReactionParticipant>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<ReactionTag> ReactionTags => Set<ReactionTag>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserExperiment> UserExperiments => Set<UserExperiment>();

    public DbSet<UserProgress> UserProgresses => Set<UserProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReactionLabDbContext).Assembly);
    }
}