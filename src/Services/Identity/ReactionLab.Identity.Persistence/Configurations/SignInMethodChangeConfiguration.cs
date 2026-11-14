using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class SignInMethodChangeConfiguration : IEntityTypeConfiguration<SignInMethodChange>
{
    public void Configure(EntityTypeBuilder<SignInMethodChange> builder)
    {
        builder.ToTable("sign_in_method_changes");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedNever();
        builder.Property(change => change.Change).HasMaxLength(SignInMethodChange.MaximumChangeLength);
        builder.Property(change => change.Provider).HasMaxLength(User.MaximumLoginKeyLength);
        builder.Property(change => change.ChangedAt).HasDefaultValueSql("now()");
        builder.Property(change => change.UserAgent).HasMaxLength(PasswordChange.MaximumUserAgentLength);

        builder.HasIndex(change => change.ChangedAt).HasDatabaseName("ix_sign_in_method_changes_changed_at");
        builder.HasIndex(change => change.UserId).HasDatabaseName("ix_sign_in_method_changes_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(change => change.UserId)
            .HasConstraintName("fk_sign_in_method_changes_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
