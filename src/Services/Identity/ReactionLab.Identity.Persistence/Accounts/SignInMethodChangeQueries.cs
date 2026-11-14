using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class SignInMethodChangeQueries
{
    public static Task<SignInMethodChange?> FindSignInMethodChangeAsync(
        this AppDbContext context,
        Guid userId,
        Guid changeId,
        CancellationToken cancellationToken) =>
        context.Set<SignInMethodChange>().AsNoTracking()
            .SingleOrDefaultAsync(change => change.Id == changeId && change.UserId == userId, cancellationToken);
}
