using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public sealed class RegularAccountUserStore(AppDbContext context, IdentityErrorDescriber? describer = null)
    : UserOnlyStore<User, AppDbContext, Guid>(context, describer)
{
    public override Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return Users.SingleOrDefaultAsync(
            user => user.NormalizedEmail == normalizedEmail && user.OrganizationId == null,
            cancellationToken);
    }

    public override async Task<User?> FindByLoginAsync(
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken = default) =>
        await base.FindByLoginAsync(loginProvider, providerKey, cancellationToken) is { OrganizationId: null } user
            ? user
            : null;
}
