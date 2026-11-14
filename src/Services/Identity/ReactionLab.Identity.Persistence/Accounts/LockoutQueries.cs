using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class LockoutQueries
{
    public static Task<AccountLockout?> FindAccountLockoutAsync(
        this AppDbContext context,
        Guid userId,
        CancellationToken cancellationToken) =>
        context.Users.AsNoTracking()
            .Where(user => user.Id == userId && user.OrganizationId == null)
            .Select(user => new AccountLockout(
                user.LockoutEnabled,
                user.LockoutEnd,
                user.AccessFailedCount,
                user.CurrentPasswordLockoutEnd,
                user.CurrentPasswordFailures))
            .SingleOrDefaultAsync(cancellationToken);
}
