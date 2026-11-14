using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.SecurityLinks;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class SecurityLinkConfiguration : IEntityTypeConfiguration<SecurityLink>
{
    public void Configure(EntityTypeBuilder<SecurityLink> builder)
    {
        builder.ToTable("security_links");
        builder.HasKey(link => new { link.UserId, link.Purpose });
        builder.Property(link => link.Purpose).HasMaxLength(SecurityLink.MaximumPurposeLength);
        builder.Property(link => link.Email).HasMaxLength(User.MaximumEmailLength).IsRequired();
        builder.Property(link => link.RequestedAt).HasDefaultValueSql("now()");
        builder.Property(link => link.FirstRequestedAt).HasDefaultValueSql("now()");

        builder.HasIndex(link => link.TokenHash)
            .HasDatabaseName("ix_security_links_token_hash")
            .IsUnique()
            .HasFilter("token_hash IS NOT NULL");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(link => link.UserId)
            .HasConstraintName("fk_security_links_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
