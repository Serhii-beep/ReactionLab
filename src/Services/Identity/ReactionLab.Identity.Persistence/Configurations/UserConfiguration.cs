using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table => table.HasCheckConstraint(
            "ck_users_organization_username",
            "(organization_id IS NULL) = (organization_username IS NULL)"));

        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.HasIndex(user => user.NormalizedUserName)
            .HasDatabaseName("ix_users_normalized_user_name")
            .IsUnique();

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("ix_users_normalized_email")
            .IsUnique();

        builder.HasIndex(user => user.NormalizedEmail, "ix_users_regular_account_normalized_email")
            .HasDatabaseName("ix_users_regular_account_normalized_email")
            .IsUnique()
            .HasFilter("organization_id IS NULL");

        builder.HasIndex(user => new { user.OrganizationId, user.NormalizedEmail })
            .HasDatabaseName("ix_users_organization_id_normalized_email")
            .IsUnique()
            .HasFilter("organization_id IS NOT NULL AND normalized_email IS NOT NULL");

        builder.HasIndex(user => new { user.OrganizationId, user.OrganizationUsername })
            .HasDatabaseName("ix_users_organization_id_organization_username")
            .IsUnique()
            .HasFilter("organization_id IS NOT NULL");

        builder.Property(user => user.DisplayName)
            .HasMaxLength(User.MaximumDisplayNameLength)
            .IsRequired();

        builder.Property(user => user.PreferredLocale)
            .HasMaxLength(User.MaximumLocaleLength)
            .IsRequired();

        builder.Property(user => user.BirthYear);
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.OrganizationUsername).HasMaxLength(User.MaximumUsernameLength);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(user => user.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
