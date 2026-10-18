using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    private const int MaximumKeyLength = 128;

    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
    {
        builder.ToTable("user_logins");
        builder.Property(login => login.LoginProvider).HasMaxLength(MaximumKeyLength);
        builder.Property(login => login.ProviderKey).HasMaxLength(MaximumKeyLength);
    }
}
