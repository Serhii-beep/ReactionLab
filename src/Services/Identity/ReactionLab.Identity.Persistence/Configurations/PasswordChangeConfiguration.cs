using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class PasswordChangeConfiguration : IEntityTypeConfiguration<PasswordChange>
{
    public void Configure(EntityTypeBuilder<PasswordChange> builder)
    {
        builder.ToTable("password_changes");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedNever();
        builder.Property(change => change.ChangedAt).HasDefaultValueSql("now()");
        builder.Property(change => change.UserAgent).HasMaxLength(PasswordChange.MaximumUserAgentLength);
        builder.HasIndex(change => change.ChangedAt).HasDatabaseName("ix_password_changes_changed_at");
        builder.HasIndex(change => change.UserId).HasDatabaseName("ix_password_changes_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(change => change.UserId)
            .HasConstraintName("fk_password_changes_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
