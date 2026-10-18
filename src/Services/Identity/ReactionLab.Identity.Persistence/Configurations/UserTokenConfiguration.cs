using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    private const int MaximumKeyLength = 128;

    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
    {
        builder.ToTable("user_tokens");
        builder.Property(token => token.LoginProvider).HasMaxLength(MaximumKeyLength);
        builder.Property(token => token.Name).HasMaxLength(MaximumKeyLength);
    }
}
