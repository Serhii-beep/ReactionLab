using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
    {
        builder.ToTable("user_claims");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(claim => claim.UserId)
            .HasConstraintName("fk_user_claims_users_user_id");
    }
}
