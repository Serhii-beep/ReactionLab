using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class ContactQueries
{
    public static Task<AccountContact?> FindContactAsync(
        this AppDbContext context,
        Guid userId,
        CancellationToken cancellationToken) =>
        context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AccountContact(
                user.Email,
                user.EmailConfirmed,
                user.DisplayName,
                user.PreferredLocale,
                user.TimeZone))
            .SingleOrDefaultAsync(cancellationToken);
}
