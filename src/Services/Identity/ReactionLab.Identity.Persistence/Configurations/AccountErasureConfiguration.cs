using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Erasures;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class AccountErasureConfiguration : IEntityTypeConfiguration<AccountErasure>
{
    public void Configure(EntityTypeBuilder<AccountErasure> builder)
    {
        builder.ToTable("account_erasures");
        builder.HasKey(erasure => erasure.UserId);
        builder.Property(erasure => erasure.UserId).ValueGeneratedNever();
        builder.Property(erasure => erasure.Reason).HasMaxLength(AccountErasure.MaximumReasonLength);

        builder.HasIndex(erasure => erasure.RequestedAt)
            .HasDatabaseName("ix_account_erasures_incomplete_requested_at")
            .HasFilter("completed_at IS NULL");
        builder.HasIndex(erasure => erasure.CompletedAt)
            .HasDatabaseName("ix_account_erasures_completed_at")
            .HasFilter("completed_at IS NOT NULL");

        builder.HasMany<ErasureConfirmation>()
            .WithOne()
            .HasForeignKey(confirmation => confirmation.UserId)
            .HasConstraintName("fk_erasure_confirmations_account_erasures_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
