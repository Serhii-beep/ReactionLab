using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class PasswordQueries
{
    public static Task<bool> HoldsPasswordHashAsync(this AppDbContext context, Guid userId, string passwordHash) =>
        context.Users.AnyAsync(
            user => user.Id == userId && user.PasswordHash == passwordHash,
            CancellationToken.None);
}
