using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class ProfileQueries
{
    public static Task<AccountProfile?> FindProfileAsync(
        this AppDbContext context,
        Guid userId,
        CancellationToken cancellationToken) =>
        context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AccountProfile(
                user.Email,
                user.DisplayName,
                user.PreferredLocale,
                user.PasswordHash != null))
            .SingleOrDefaultAsync(cancellationToken);
}
