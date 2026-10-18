using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.HasIndex(user => user.NormalizedUserName)
            .HasDatabaseName("ix_users_normalized_user_name")
            .IsUnique();

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("ix_users_normalized_email")
            .IsUnique();

        builder.Property(user => user.DisplayName)
            .HasMaxLength(User.MaximumDisplayNameLength)
            .IsRequired();

        builder.Property(user => user.PreferredLocale)
            .HasMaxLength(User.MaximumLocaleLength)
            .IsRequired();

        builder.Property(user => user.BirthYear);
        builder.Property(user => user.CreatedAt).IsRequired();
    }
}
