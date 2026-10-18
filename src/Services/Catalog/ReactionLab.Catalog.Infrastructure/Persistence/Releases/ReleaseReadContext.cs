using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Releases.Snapshots;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.Catalog.Infrastructure.Persistence.Configurations;
using ReactionLab.Chemistry.Constitution;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Releases;

public sealed class ReleaseReadContext(DbContextOptions<ReleaseReadContext> options)
    : DbContext(options), IReleaseReadContext
{
    public const string Substances = "release_substances";

    public const string SubstanceNames = "release_substance_names";

    public const string PrefixTops = "release_prefix_tops";

    public const string Words = "release_words";

    public const string SubstanceElements = "release_substance_elements";

    public const string Reactions = "release_reactions";

    public const string Participants = "release_participants";

    public static IReadOnlyList<string> Tables { get; } =
        [Substances, SubstanceNames, PrefixTops, Words, SubstanceElements, Reactions, Participants];

    public Guid ReleaseId { get; private set; }

    IQueryable<Substance> IReleaseReadContext.Substances => Set<Substance>();

    IQueryable<Reaction> IReleaseReadContext.Reactions => Set<Reaction>();

    IQueryable<ReleaseSubstanceName> IReleaseReadContext.SubstanceNames => Set<ReleaseSubstanceName>();

    IQueryable<ReleasePrefixTop> IReleaseReadContext.PrefixTops => Set<ReleasePrefixTop>();

    IQueryable<ReleaseWord> IReleaseReadContext.Words => Set<ReleaseWord>();

    IQueryable<ReleaseSubstanceElement> IReleaseReadContext.SubstanceElements => Set<ReleaseSubstanceElement>();

    public void UseRelease(Guid releaseId) => ReleaseId = releaseId;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder
            .UseSnakeCaseNamingConvention()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.ApplyDomainConversions();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Substance>(builder =>
        {
            MapSubstance(builder);
            builder.HasQueryFilter(substance => EF.Property<Guid>(substance, ReleaseColumns.ReleaseId) == ReleaseId);
        });

        modelBuilder.Entity<Reaction>(builder =>
        {
            MapReaction(builder);
            builder.HasQueryFilter(reaction => EF.Property<Guid>(reaction, ReleaseColumns.ReleaseId) == ReleaseId);
        });

        modelBuilder.Entity<ReleaseSubstanceName>(builder =>
        {
            builder.ToTable(SubstanceNames, table => table.ExcludeFromMigrations());
            builder.HasNoKey();
            builder.Property(name => name.NameKey).UseCollation("C");
            builder.HasQueryFilter(name => name.ReleaseId == ReleaseId);
        });

        modelBuilder.Entity<ReleasePrefixTop>(builder =>
        {
            builder.ToTable(PrefixTops, table => table.ExcludeFromMigrations());
            builder.HasNoKey();
            builder.Property(top => top.Prefix).UseCollation("C");
            builder.HasQueryFilter(top => top.ReleaseId == ReleaseId);
        });

        modelBuilder.Entity<ReleaseWord>(builder =>
        {
            builder.ToTable(Words, table => table.ExcludeFromMigrations());
            builder.HasNoKey();
            builder.Property(word => word.Word).UseCollation("C");
            builder.HasQueryFilter(word => word.ReleaseId == ReleaseId);
        });

        modelBuilder.Entity<ReleaseSubstanceElement>(builder =>
        {
            builder.ToTable(SubstanceElements, table => table.ExcludeFromMigrations());
            builder.HasNoKey();
            builder.HasQueryFilter(element => element.ReleaseId == ReleaseId);
        });
    }

    private static void MapSubstance(EntityTypeBuilder<Substance> builder)
    {
        builder.ToTable(Substances, table => table.ExcludeFromMigrations());
        builder.Property<Guid>(ReleaseColumns.ReleaseId);
        builder.HasKey(ReleaseColumns.ReleaseId, nameof(Substance.Id));

        SubstanceMapping.MapContent(builder);

        builder.Property<int>(ReleaseColumns.Prominence);
        builder.Property<string?>(ReleaseColumns.StructureKey).HasMaxLength(ConstitutionKey.Length);
    }

    private static void MapReaction(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable(Reactions, table => table.ExcludeFromMigrations());
        builder.Property<Guid>(ReleaseColumns.ReleaseId);
        builder.HasKey(ReleaseColumns.ReleaseId, nameof(Reaction.Id));

        ReactionMapping.MapContent(builder);

        builder.Property<Guid[]>(ReleaseColumns.ReactantIds);
        builder.Property<int>(ReleaseColumns.Rank);

        builder.OwnsMany(reaction => reaction.Participants, participant =>
        {
            participant.ToTable(Participants, table => table.ExcludeFromMigrations());
            participant.Property<Guid>(ReleaseColumns.ReleaseId);
            participant.WithOwner().HasForeignKey(ReleaseColumns.ReleaseId, "ReactionId");
            participant.HasKey(ReleaseColumns.ReleaseId, nameof(ReactionParticipant.Id));
            ReactionMapping.MapParticipantContent(participant);
        });
    }
}
