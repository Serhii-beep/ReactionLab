using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public class UserExperimentConfiguration : IEntityTypeConfiguration<UserExperiment>
{
    public void Configure(EntityTypeBuilder<UserExperiment> builder)
    {
        builder.ToTable("UserExperiments");

        builder.HasKey(ue => ue.Id);

        builder.Property(ue => ue.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ue => ue.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(ue => ue.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(ue => ue.User)
            .WithMany(u => u.Experiments)
            .HasForeignKey(ue => ue.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
