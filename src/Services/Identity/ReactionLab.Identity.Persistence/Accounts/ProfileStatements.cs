using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class ProfileStatements
{
    public static async Task<UpdatedProfile?> UpdateProfileAsync(
        this AppDbContext context,
        Guid userId,
        string? displayName,
        string? preferredLocale) =>
        (await context.Database.SqlQuery<UpdatedProfile>(
            $"""
            UPDATE users
            SET display_name = coalesce({displayName}, display_name),
                preferred_locale = coalesce({preferredLocale}, preferred_locale),
                version = version + CASE
                    WHEN coalesce({displayName}, display_name) IS DISTINCT FROM display_name
                        OR coalesce({preferredLocale}, preferred_locale) IS DISTINCT FROM preferred_locale
                    THEN 1 ELSE 0 END,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId}
            RETURNING email, display_name, preferred_locale, password_hash IS NOT NULL AS has_password, version,
                version <> old.version AS profile_changed
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();
}
