using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.AccountSetups;

public static class AccountSetupStatements
{
    public static Task<int> LockSchoolAccountAsync(this AppDbContext context, Guid organizationId, Guid accountId) =>
        context.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM users WHERE id = {accountId} AND organization_id = {organizationId} FOR NO KEY UPDATE",
            CancellationToken.None);

    public static Task<int> IssueAccountSetupAsync(
        this AppDbContext context,
        Guid organizationId,
        Guid accountId,
        byte[] tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        Guid? issuedBy,
        Guid? issuedInSession) =>
        context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO account_setups (user_id, token_hash, issued_at, expires_at, issued_by, issued_in_session)
            SELECT users.id, {tokenHash}, {issuedAt}, {expiresAt}, {issuedBy}::uuid, {issuedInSession}::uuid
            FROM users
            WHERE users.id = {accountId} AND users.organization_id = {organizationId}
            ON CONFLICT (user_id) DO UPDATE
            SET token_hash = excluded.token_hash,
                issued_at = excluded.issued_at,
                expires_at = excluded.expires_at,
                issued_by = excluded.issued_by,
                issued_in_session = excluded.issued_in_session
            """,
            CancellationToken.None);

    public static async Task<CompletedAccountSetup?> CompleteAccountSetupAsync(
        this AppDbContext context,
        Guid accountId,
        byte[] tokenHash,
        string passwordHash,
        string securityStamp,
        DateTimeOffset now)
    {
        await context.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM users WHERE id = {accountId} FOR NO KEY UPDATE",
            CancellationToken.None);

        return (await context.Database.SqlQuery<CompletedAccountSetup>(
            $"""
            WITH consumed AS (
                DELETE FROM account_setups
                WHERE user_id = {accountId} AND token_hash = {tokenHash} AND expires_at > {now}
                RETURNING user_id)
            UPDATE users
            SET password_hash = {passwordHash},
                password_probation_ends_at = NULL,
                security_stamp = {securityStamp},
                access_failed_count = 0,
                lockout_end = NULL,
                current_password_failures = 0,
                current_password_lockout_end = NULL,
                concurrency_stamp = gen_random_uuid()::text
            FROM consumed
            WHERE users.id = consumed.user_id AND users.organization_id IS NOT NULL
            RETURNING users.id AS account_id, users.organization_id
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();
    }
}
