using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.Messaging.Outbox;

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
                issued_in_session = excluded.issued_in_session,
                request_id = NULL,
                requested_at = NULL,
                email = NULL,
                purpose = NULL,
                first_requested_at = NULL,
                request_count = 0
            """,
            CancellationToken.None);

    public static Task<int> RequestMailedAccountSetupAsync(
        this AppDbContext context,
        Guid organizationId,
        Guid accountId,
        Guid requestId,
        string purpose,
        Guid? issuedBy,
        Guid? issuedInSession,
        OutboxMessage requested) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH requester AS (
                SELECT EXISTS (
                    SELECT 1 FROM users WHERE id = {issuedBy}::uuid AND organization_id IS NULL) AS from_platform),
            counted AS (
                UPDATE users
                SET mailed_setups_since = CASE
                        WHEN users.mailed_setups_since IS NULL
                            OR users.mailed_setups_since <= now() - {AccountSetup.MailedSetupWindow}
                        THEN now()
                        ELSE users.mailed_setups_since
                    END,
                    mailed_setup_count = CASE
                        WHEN users.mailed_setups_since IS NULL
                            OR users.mailed_setups_since <= now() - {AccountSetup.MailedSetupWindow}
                        THEN 1
                        ELSE users.mailed_setup_count + 1
                    END
                FROM organizations, requester
                WHERE users.id = {accountId} AND users.organization_id = {organizationId}
                    AND users.email IS NOT NULL AND organizations.id = users.organization_id
                    AND (users.mailed_setups_since IS NULL
                        OR users.mailed_setups_since <= now() - {AccountSetup.MailedSetupWindow}
                        OR users.mailed_setup_count < {AccountSetup.MaximumMailedSetupsPerWindow})
                    AND (requester.from_platform
                        OR organizations.mailed_setups_since IS NULL
                        OR organizations.mailed_setups_since <= now() - {AccountSetup.MailedSetupWindow}
                        OR organizations.mailed_setup_count < {Organization.MaximumMailedSetupsPerWindow})
                RETURNING users.id, users.email),
            recorded AS (
                INSERT INTO account_setups (
                    user_id, token_hash, issued_at, expires_at, issued_by, issued_in_session, request_id,
                    requested_at, email, purpose, first_requested_at, request_count)
                SELECT id, NULL, now(), now() + {AccountSetup.Lifetime}, {issuedBy}::uuid, {issuedInSession}::uuid,
                    {requestId}, now(), email, {purpose}, NULL, 0
                FROM counted
                ON CONFLICT (user_id) DO UPDATE
                SET token_hash = NULL,
                    issued_at = excluded.issued_at,
                    expires_at = excluded.expires_at,
                    issued_by = excluded.issued_by,
                    issued_in_session = excluded.issued_in_session,
                    request_id = excluded.request_id,
                    requested_at = excluded.requested_at,
                    email = excluded.email,
                    purpose = excluded.purpose
                RETURNING user_id),
            school_counted AS (
                UPDATE organizations
                SET mailed_setups_since = CASE
                        WHEN mailed_setups_since IS NULL
                            OR mailed_setups_since <= now() - {AccountSetup.MailedSetupWindow}
                        THEN now()
                        ELSE mailed_setups_since
                    END,
                    mailed_setup_count = CASE
                        WHEN mailed_setups_since IS NULL
                            OR mailed_setups_since <= now() - {AccountSetup.MailedSetupWindow}
                        THEN 1
                        ELSE mailed_setup_count + 1
                    END
                FROM requester
                WHERE id = {organizationId} AND NOT requester.from_platform AND EXISTS (SELECT 1 FROM recorded))
            INSERT INTO outbox_messages (id, topic, subject, type, payload, trace_parent, created_at)
            SELECT {requested.Id}, {requested.Topic}, {requested.Subject}, {requested.Type}, {requested.Payload},
                {requested.TraceParent}, {requested.CreatedAt}
            FROM recorded
            """,
            CancellationToken.None);

    public static Task<int> RequestSelfResetAsync(
        this AppDbContext context,
        Guid accountId,
        Guid requestId,
        OutboxMessage requested) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH recorded AS (
                INSERT INTO account_setups (
                    user_id, token_hash, issued_at, expires_at, issued_by, issued_in_session, request_id,
                    requested_at, email, purpose, first_requested_at, request_count)
                SELECT users.id, NULL, now(), now() + {AccountSetup.SelfResetLifetime}, users.id, NULL,
                    {requestId}, now(), users.email, {AccountSetupPurposes.SelfReset}, now(), 1
                FROM users
                JOIN organizations ON organizations.id = users.organization_id
                LEFT JOIN organizations AS districts ON districts.id = organizations.district_id
                WHERE users.id = {accountId} AND users.email IS NOT NULL AND users.password_hash IS NOT NULL
                    AND users.disabled_at IS NULL AND organizations.accounts_may_reset_own_passwords
                    AND organizations.suspended_at IS NULL AND districts.suspended_at IS NULL
                FOR NO KEY UPDATE OF users
                ON CONFLICT (user_id) DO UPDATE
                SET token_hash = NULL,
                    issued_at = excluded.issued_at,
                    expires_at = excluded.expires_at,
                    issued_by = excluded.issued_by,
                    issued_in_session = NULL,
                    request_id = excluded.request_id,
                    requested_at = excluded.requested_at,
                    email = excluded.email,
                    purpose = excluded.purpose,
                    first_requested_at = CASE
                        WHEN account_setups.first_requested_at IS NULL
                            OR account_setups.first_requested_at
                                <= excluded.requested_at - {AccountSetup.SelfResetWindow}
                        THEN excluded.requested_at
                        ELSE account_setups.first_requested_at
                    END,
                    request_count = CASE
                        WHEN account_setups.first_requested_at IS NULL
                            OR account_setups.first_requested_at
                                <= excluded.requested_at - {AccountSetup.SelfResetWindow}
                        THEN 1
                        ELSE account_setups.request_count + 1
                    END
                WHERE (account_setups.purpose = {AccountSetupPurposes.SelfReset}
                        OR account_setups.expires_at <= excluded.requested_at)
                    AND (account_setups.requested_at IS NULL
                        OR account_setups.requested_at
                            <= excluded.requested_at - {AccountSetup.MinimumIntervalBetweenSelfResets})
                    AND (account_setups.first_requested_at IS NULL
                        OR account_setups.first_requested_at <= excluded.requested_at - {AccountSetup.SelfResetWindow}
                        OR account_setups.request_count < {AccountSetup.MaximumSelfResetsPerWindow})
                RETURNING user_id)
            INSERT INTO outbox_messages (id, topic, subject, type, payload, trace_parent, created_at)
            SELECT {requested.Id}, {requested.Topic}, {requested.Subject}, {requested.Type}, {requested.Payload},
                {requested.TraceParent}, {requested.CreatedAt}
            FROM recorded
            """,
            CancellationToken.None);

    public static async Task<DateTimeOffset?> IssueMailedAccountSetupLinkAsync(
        this AppDbContext context,
        Guid accountId,
        Guid requestId,
        byte[] tokenHash)
    {
        var expiries = await context.Database.SqlQuery<DateTimeOffset>(
            $"""
            UPDATE account_setups
            SET token_hash = {tokenHash},
                issued_at = now()
            FROM users
            JOIN organizations ON organizations.id = users.organization_id
            LEFT JOIN organizations AS districts ON districts.id = organizations.district_id
            WHERE account_setups.user_id = {accountId} AND account_setups.request_id = {requestId}
                AND account_setups.expires_at > now()
                AND users.id = account_setups.user_id AND users.email = account_setups.email
                AND users.disabled_at IS NULL
                AND organizations.suspended_at IS NULL AND districts.suspended_at IS NULL
                AND (account_setups.purpose <> {AccountSetupPurposes.SelfReset}
                    OR (organizations.accounts_may_reset_own_passwords AND users.password_hash IS NOT NULL))
            RETURNING account_setups.expires_at AS "Value"
            """)
            .ToListAsync(CancellationToken.None);

        return expiries.Count == 0 ? null : expiries[0];
    }

    public static async Task<CompletedAccountSetup?> CompleteAccountSetupAsync(
        this AppDbContext context,
        Guid accountId,
        byte[] tokenHash,
        string? username,
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
                    AND EXISTS (
                        SELECT 1 FROM users
                        JOIN organizations ON organizations.id = users.organization_id
                        LEFT JOIN organizations AS districts ON districts.id = organizations.district_id
                        WHERE users.id = {accountId} AND users.disabled_at IS NULL
                            AND (users.organization_username IS NOT NULL OR {username}::text IS NOT NULL)
                            AND organizations.suspended_at IS NULL AND districts.suspended_at IS NULL)
                RETURNING user_id)
            UPDATE users
            SET password_hash = {passwordHash},
                organization_username = coalesce(users.organization_username, {username}::text),
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
