using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class ProfileStatements
{
    public static async Task<long> ReplaceRegisteredProfileAsync(
        this AppDbContext context,
        Guid userId,
        RegisteredProfile profile) =>
        (await context.Database.SqlQuery<long>(
            $"""
            UPDATE users
            SET display_name = {profile.DisplayName},
                preferred_locale = {profile.PreferredLocale},
                time_zone = {profile.TimeZone},
                birth_year = {profile.BirthYear},
                version = version + 1,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId} AND organization_id IS NULL
            RETURNING version AS "Value"
            """)
            .ToListAsync(CancellationToken.None))
            .Single();

    public static async Task<UpdatedProfile?> UpdateProfileAsync(
        this AppDbContext context,
        Guid userId,
        string? displayName,
        string? preferredLocale,
        string? timeZone) =>
        (await context.Database.SqlQuery<UpdatedProfile>(
            $"""
            UPDATE users
            SET display_name = coalesce({displayName}, display_name),
                preferred_locale = coalesce({preferredLocale}, preferred_locale),
                time_zone = coalesce({timeZone}, time_zone),
                version = version + CASE
                    WHEN coalesce({displayName}, display_name) IS DISTINCT FROM display_name
                        OR coalesce({preferredLocale}, preferred_locale) IS DISTINCT FROM preferred_locale
                    THEN 1 ELSE 0 END,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId}
            RETURNING email, display_name, preferred_locale, time_zone, password_hash IS NOT NULL AS has_password,
                version,
                version <> old.version AS profile_changed
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();
}
