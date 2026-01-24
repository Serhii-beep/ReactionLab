using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public class UserProgressConfiguration : IEntityTypeConfiguration<UserProgress>
{
    public void Configure(EntityTypeBuilder<UserProgress> builder)
    {
        builder.ToTable("UserProgress");

        builder.HasKey(up => up.Id);

        builder.HasIndex(up => new { up.UserId, up.ReactionId })
            .IsUnique();

        builder.Property(up => up.Completed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(up => up.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(up => up.User)
            .WithMany(u => u.Progress)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Reaction)
            .WithMany()
            .HasForeignKey(up => up.ReactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}