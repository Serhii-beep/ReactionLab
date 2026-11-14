using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.AccountSetups;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class AccountSetupConfiguration : IEntityTypeConfiguration<AccountSetup>
{
    public void Configure(EntityTypeBuilder<AccountSetup> builder)
    {
        builder.ToTable("account_setups");
        builder.HasKey(setup => setup.UserId);
        builder.HasIndex(setup => setup.TokenHash).IsUnique();
        builder.Property(setup => setup.Email).HasMaxLength(User.MaximumEmailLength);
        builder.Property(setup => setup.Purpose).HasMaxLength(AccountSetup.MaximumPurposeLength);
        builder.Property(setup => setup.RequestCount).HasDefaultValue(0);

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<AccountSetup>(setup => setup.UserId)
            .HasConstraintName("fk_account_setups_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
