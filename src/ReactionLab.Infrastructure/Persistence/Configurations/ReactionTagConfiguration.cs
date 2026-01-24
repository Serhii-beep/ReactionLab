using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public class ReactionTagConfiguration : IEntityTypeConfiguration<ReactionTag>
{
    public void Configure(EntityTypeBuilder<ReactionTag> builder)
    {
        builder.ToTable("ReactionTags");

        builder.HasKey(rt => new { rt.ReactionId, rt.TagId });

        builder.HasOne(rt => rt.Reaction)
            .WithMany(r => r.ReactionTags)
            .HasForeignKey(rt => rt.ReactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rt => rt.Tag)
            .WithMany(t => t.ReactionTags)
            .HasForeignKey(rt => rt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}