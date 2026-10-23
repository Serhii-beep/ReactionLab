using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
    {
        builder.ToTable("user_logins");
        builder.Property(login => login.LoginProvider).HasMaxLength(User.MaximumLoginKeyLength);
        builder.Property(login => login.ProviderKey).HasMaxLength(User.MaximumLoginKeyLength);
        builder.Property<DateTimeOffset?>(UserLoginProperties.ConnectedAt).HasDefaultValueSql("now()");
        builder.HasIndex(login => new { login.UserId, login.LoginProvider }).IsUnique().IsCreatedConcurrently();
    }
}
