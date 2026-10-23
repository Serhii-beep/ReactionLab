using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class SecurityStampStatements
{
    public static async Task<string?> ReplaceSecurityStampAsync(
        this AppDbContext context,
        Guid userId,
        string securityStamp) =>
        (await context.Database.SqlQuery<string>(
            $"""
            WITH previous AS MATERIALIZED (
                SELECT id, security_stamp FROM users WHERE id = {userId} FOR NO KEY UPDATE)
            UPDATE users
            SET security_stamp = {securityStamp},
                concurrency_stamp = gen_random_uuid()::text
            FROM previous
            WHERE users.id = previous.id
            RETURNING previous.security_stamp AS "Value"
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();

    public static async Task<string?> ReplacePasswordAndSecurityStampAsync(
        this AppDbContext context,
        Guid userId,
        string previousHash,
        string replacementHash,
        string securityStamp) =>
        (await context.Database.SqlQuery<string>(
            $"""
            WITH previous AS MATERIALIZED (
                SELECT id, security_stamp FROM users WHERE id = {userId} FOR NO KEY UPDATE)
            UPDATE users
            SET password_hash = {replacementHash},
                security_stamp = {securityStamp},
                concurrency_stamp = gen_random_uuid()::text
            FROM previous
            WHERE users.id = previous.id AND users.password_hash IN ({previousHash}, {replacementHash})
            RETURNING previous.security_stamp AS "Value"
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();

    public static async Task<string?> RemoveLoginAndReplaceSecurityStampAsync(
        this AppDbContext context,
        Guid userId,
        string provider,
        string[] usableProviders,
        DateTimeOffset now,
        string securityStamp)
    {
        if (await LockSecurityStampAsync(context, userId) is not { } previousStamp)
        {
            return null;
        }

        var removed = await context.Database.ExecuteSqlAsync(
            $"""
            WITH removed AS (
                DELETE FROM user_logins
                WHERE user_id = {userId} AND login_provider = {provider}
                    AND (EXISTS (SELECT 1 FROM users
                            WHERE id = {userId} AND password_hash IS NOT NULL AND normalized_email IS NOT NULL
                                AND lockout_end IS DISTINCT FROM 'infinity'
                                AND (password_probation_ends_at IS NULL OR password_probation_ends_at <= {now}))
                        OR EXISTS (SELECT 1 FROM user_logins AS other
                            WHERE other.user_id = {userId} AND other.login_provider <> {provider}
                                AND other.login_provider = ANY ({usableProviders})))
                RETURNING user_id)
            UPDATE users
            SET security_stamp = {securityStamp},
                concurrency_stamp = gen_random_uuid()::text
            WHERE id IN (SELECT user_id FROM removed)
            """,
            CancellationToken.None);

        return removed == 1 ? previousStamp : null;
    }

    public static async Task<string?> RemovePasswordAndReplaceSecurityStampAsync(
        this AppDbContext context,
        Guid userId,
        string[] usableProviders,
        string securityStamp)
    {
        if (await LockSecurityStampAsync(context, userId) is not { } previousStamp)
        {
            return null;
        }

        var removed = await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET password_hash = NULL,
                password_probation_ends_at = NULL,
                security_stamp = {securityStamp},
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId} AND password_hash IS NOT NULL
                AND EXISTS (SELECT 1 FROM user_logins
                    WHERE user_id = {userId} AND login_provider = ANY ({usableProviders}))
            """,
            CancellationToken.None);

        return removed == 1 ? previousStamp : null;
    }

    private static async Task<string?> LockSecurityStampAsync(AppDbContext context, Guid userId) =>
        (await context.Database.SqlQuery<string>(
            $"""
            SELECT security_stamp AS "Value" FROM users WHERE id = {userId} FOR NO KEY UPDATE
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();
}
