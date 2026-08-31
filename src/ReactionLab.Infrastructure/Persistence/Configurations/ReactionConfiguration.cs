using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Reactions;
using ReactionLab.Domain.Substances;
using ReactionLab.Infrastructure.Persistence.Converters;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

internal sealed class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("reactions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Type).IsRequired();
        builder.Property(r => r.Difficulty).IsRequired();
        builder.Property(r => r.IsReversible).IsRequired();
        builder.HasIndex(r => r.Type);

        builder.ComplexProperty(r => r.Energetics, e =>
        {
            e.Property(x => x.EnthalpyChange).HasColumnName("enthalpy_kj_per_mol");
            e.Property(x => x.ActivationEnergyKilojoulesPerMole)
                .HasColumnName("activation_energy_kj_per_mol").HasPrecision(12, 4);
        });

        builder.ComplexProperty(r => r.Conditions, c =>
        {
            c.Property(x => x.Temperature).HasColumnName("temperature_k");
            c.Property(x => x.Pressure).HasColumnName("pressure_kpa");
            c.Property(x => x.Catalyst)
                .HasColumnName("catalyst")
                .HasMaxLength(ReactionConditions.MaximumCatalystLength);
        });

        builder.ComplexProperty(r => r.Visualization, v =>
        {
            v.Property(x => x.PresetKey)
                .HasColumnName("effect_preset_key")
                .HasMaxLength(VisualizationHint.MaximumPresetKeyLength);
            v.Property(x => x.DurationMilliseconds).HasColumnName("animation_duration_ms");
        });

        builder.ComplexProperty(r => r.Provenance, p =>
        {
            p.Property(x => x.Rule)
                .HasColumnName("rule")
                .HasMaxLength(ReactionProvenance.MaximumRuleLength);

            p.Property(x => x.Confidence)
                .HasColumnName("confidence")
                .HasPrecision(4, 3);
        });

        builder.Property<List<string>>("_tags")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("tags")
            .IsRequired();

        builder.Property(r => r.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("translations")
            .HasColumnType("jsonb")
            .HasConversion<ReactionTranslationsConverter>()
            .IsRequired();

        builder.Property<Guid[]>(PersistenceColumns.ReactantSignature).IsRequired();
        builder.HasIndex(PersistenceColumns.ReactantSignature).HasMethod("gin");

        builder.OwnsMany(r => r.Participants, p =>
        {
            p.ToTable("reaction_participants");
            p.WithOwner().HasForeignKey("ReactionId");
            p.HasKey(p => p.Id);
            p.Property(x => x.Id).ValueGeneratedNever();
            p.Property(x => x.SubstanceId).IsRequired();
            p.Property(x => x.Role).IsRequired();
            p.Property(x => x.Coefficient).IsRequired();
            p.HasIndex(x => x.SubstanceId);
            p.HasOne<Substance>()
                .WithMany()
                .HasForeignKey(x => x.SubstanceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(r => r.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasSearchText();
        builder.HasAuditTimestamps();

        builder.Ignore(r => r.Reactants);
        builder.Ignore(r => r.Products);
        builder.Ignore(r => r.ReactantSignature);
        builder.Ignore(r => r.Tags);
        builder.Ignore(r => r.Locales);
        builder.Ignore(r => r.DomainEvents);
    }
}
