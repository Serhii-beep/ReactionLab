using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("Reactions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Equation)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.EquationBalanced)
            .HasMaxLength(500);

        builder.Property(r => r.Category)
            .HasMaxLength(100);

        builder.Property(r => r.RequiredTemperature)
            .HasPrecision(8, 2);

        builder.Property(r => r.RequiredPressure)
            .HasPrecision(10, 2);

        builder.Property(r => r.CatalystInfo)
            .HasMaxLength(200);

        builder.Property(r => r.EnthalpyChange)
            .HasPrecision(10, 2);

        builder.Property(r => r.ActivationEnergy)
            .HasPrecision(10, 2);

        builder.Property(r => r.AnimationType)
            .HasMaxLength(50);

        builder.Property(r => r.EffectPreset)
            .HasMaxLength(50);

        builder.Property(r => r.DifficultyLevel)
            .IsRequired()
            .HasDefaultValue(1);
    }
}