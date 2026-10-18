using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Infrastructure.Persistence.Converters;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Configurations;

internal static class ReactionMapping
{
    public const string Tags = PersistedFields.ReactionTags;

    public static void MapContent(EntityTypeBuilder<Reaction> builder)
    {
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Type).IsRequired();
        builder.Property(r => r.Difficulty).IsRequired();
        builder.Property(r => r.IsReversible).IsRequired();
        builder.Property(r => r.RetiredAt);

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

        builder.Property<List<string>>(Tags)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("tags")
            .IsRequired();

        builder.Property(r => r.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("translations")
            .HasColumnType("jsonb")
            .HasConversion<ReactionTranslationsConverter>()
            .IsRequired();

        builder.Navigation(r => r.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.Reactants);
        builder.Ignore(r => r.Products);
        builder.Ignore(r => r.ReactantSignature);
        builder.Ignore(r => r.Tags);
        builder.Ignore(r => r.Locales);
        builder.Ignore(r => r.DomainEvents);
    }

    public static void MapParticipantContent(OwnedNavigationBuilder<Reaction, ReactionParticipant> participant)
    {
        participant.Property(x => x.Id).ValueGeneratedNever();
        participant.Property(x => x.SubstanceId).IsRequired();
        participant.Property(x => x.Role).IsRequired();
        participant.Property(x => x.Coefficient).IsRequired();
    }
}
