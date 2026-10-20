using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class ProfileStatements
{
    public static async Task<AccountProfile?> UpdateProfileAsync(
        this AppDbContext context,
        Guid userId,
        string? displayName,
        string? preferredLocale) =>
        (await context.Database.SqlQuery<AccountProfile>(
            $"""
            UPDATE users
            SET display_name = coalesce({displayName}, display_name),
                preferred_locale = coalesce({preferredLocale}, preferred_locale),
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId}
            RETURNING email, display_name, preferred_locale
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();
}
