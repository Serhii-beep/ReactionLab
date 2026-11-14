using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class PasswordChangeQueries
{
    public static Task<PasswordChange?> FindPasswordChangeAsync(
        this AppDbContext context,
        Guid userId,
        Guid changeId,
        CancellationToken cancellationToken) =>
        context.Set<PasswordChange>().AsNoTracking()
            .SingleOrDefaultAsync(change => change.Id == changeId && change.UserId == userId, cancellationToken);
}
