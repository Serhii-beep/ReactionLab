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

        builder.Metadata.RemoveIndex([builder.Property(user => user.NormalizedEmail).Metadata]);

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

        builder.HasIndex(user => new { user.OrganizationId, user.Id })
            .HasDatabaseName("ix_users_organization_id_id")
            .HasFilter("organization_id IS NOT NULL");

        builder.HasIndex(user => user.CreatedAt)
            .HasDatabaseName("ix_users_unconfirmed_created_at")
            .HasFilter("organization_id IS NULL AND email IS NOT NULL AND NOT email_confirmed"
                + " AND NOT email_confirmation_exempt");

        builder.Property(user => user.DisplayName)
            .HasMaxLength(User.MaximumDisplayNameLength)
            .IsRequired();

        builder.Property(user => user.PreferredLocale)
            .HasMaxLength(User.MaximumLocaleLength)
            .IsRequired();

        builder.Property(user => user.TimeZone).HasMaxLength(User.MaximumTimeZoneLength);

        builder.Property(user => user.BirthYear);
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.EmailConfirmationExempt);
        builder.Property(user => user.OrganizationUsername).HasMaxLength(User.MaximumUsernameLength);
        builder.Property(user => user.Version).HasDefaultValue(1L);
        builder.Ignore(user => user.DomainEvents);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(user => user.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
