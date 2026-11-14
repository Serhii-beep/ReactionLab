using Microsoft.EntityFrameworkCore;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Persistence.SecurityLinks;

public static class SecurityLinkStatements
{
    public const int PruningBatchSize = 1000;

    public static Task<int> RequestEmailConfirmationAsync(
        this AppDbContext context,
        Guid userId,
        Guid requestId,
        OutboxMessage requested) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH renewed AS (
                INSERT INTO security_links (
                    user_id, purpose, request_id, email, requested_at, first_requested_at, request_count)
                SELECT users.id, {SecurityLinkPurposes.EmailConfirmation}, {requestId}, users.email, now(), now(), 1
                FROM users
                WHERE users.id = {userId} AND users.organization_id IS NULL AND users.email IS NOT NULL
                    AND users.password_hash IS NOT NULL AND NOT users.email_confirmed
                    AND NOT users.email_confirmation_exempt
                    AND users.created_at > now() - {SecurityLink.EmailConfirmationPeriod}
                FOR NO KEY UPDATE
                ON CONFLICT (user_id, purpose) DO UPDATE
                SET request_id = excluded.request_id,
                    email = excluded.email,
                    token_hash = CASE
                        WHEN security_links.email IS DISTINCT FROM excluded.email THEN NULL
                        ELSE security_links.token_hash
                    END,
                    issued_at = CASE
                        WHEN security_links.email IS DISTINCT FROM excluded.email THEN NULL
                        ELSE security_links.issued_at
                    END,
                    expires_at = CASE
                        WHEN security_links.email IS DISTINCT FROM excluded.email THEN NULL
                        ELSE security_links.expires_at
                    END,
                    requested_at = excluded.requested_at,
                    first_requested_at = CASE
                        WHEN security_links.first_requested_at <= excluded.requested_at - {SecurityLink.RequestWindow}
                        THEN excluded.requested_at
                        ELSE security_links.first_requested_at
                    END,
                    request_count = CASE
                        WHEN security_links.first_requested_at <= excluded.requested_at - {SecurityLink.RequestWindow}
                        THEN 1
                        ELSE security_links.request_count + 1
                    END
                WHERE security_links.requested_at
                        <= excluded.requested_at - {SecurityLink.MinimumIntervalBetweenRequests}
                    AND (security_links.first_requested_at <= excluded.requested_at - {SecurityLink.RequestWindow}
                        OR security_links.request_count < {SecurityLink.MaximumRequestsPerWindow})
                RETURNING user_id)
            INSERT INTO outbox_messages (id, topic, subject, type, payload, trace_parent, created_at)
            SELECT {requested.Id}, {requested.Topic}, {requested.Subject}, {requested.Type}, {requested.Payload},
                {requested.TraceParent}, {requested.CreatedAt}
            FROM renewed
            """,
            CancellationToken.None);

    public static async Task<DateTimeOffset?> IssueEmailConfirmationLinkAsync(
        this AppDbContext context,
        Guid userId,
        Guid requestedId,
        byte[] tokenHash)
    {
        var expires = await context.Database.SqlQuery<DateTimeOffset>(
            $"""
            UPDATE security_links
            SET token_hash = {tokenHash},
                issued_at = now(),
                expires_at = least(
                    now() + {SecurityLink.EmailConfirmationLifetime},
                    users.created_at + {SecurityLink.EmailConfirmationPeriod})
            FROM users
            WHERE security_links.user_id = {userId}
                AND security_links.purpose = {SecurityLinkPurposes.EmailConfirmation}
                AND security_links.request_id = {requestedId}
                AND security_links.requested_at > now() - {SecurityLink.RequestLifetime}
                AND users.id = security_links.user_id
                AND users.email = security_links.email
                AND NOT users.email_confirmed
                AND users.created_at + {SecurityLink.EmailConfirmationPeriod}
                    > now() + {SecurityLink.ShortestEmailConfirmationLifetime}
            RETURNING security_links.expires_at AS "Value"
            """)
            .ToListAsync(CancellationToken.None);

        return expires.Count == 0 ? null : expires[0];
    }

    public static Task<int> RequestPasswordResetAsync(
        this AppDbContext context,
        Guid userId,
        Guid requestId,
        OutboxMessage requested) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH renewed AS (
                INSERT INTO security_links (
                    user_id, purpose, request_id, email, requested_at, first_requested_at, request_count)
                SELECT users.id, {SecurityLinkPurposes.PasswordReset}, {requestId}, users.email, now(), now(), 1
                FROM users
                WHERE users.id = {userId} AND users.organization_id IS NULL AND users.email IS NOT NULL
                FOR NO KEY UPDATE
                ON CONFLICT (user_id, purpose) DO UPDATE
                SET request_id = excluded.request_id,
                    email = excluded.email,
                    token_hash = CASE
                        WHEN security_links.email IS DISTINCT FROM excluded.email THEN NULL
                        ELSE security_links.token_hash
                    END,
                    issued_at = CASE
                        WHEN security_links.email IS DISTINCT FROM excluded.email THEN NULL
                        ELSE security_links.issued_at
                    END,
                    expires_at = CASE
                        WHEN security_links.email IS DISTINCT FROM excluded.email THEN NULL
                        ELSE security_links.expires_at
                    END,
                    requested_at = excluded.requested_at,
                    first_requested_at = CASE
                        WHEN security_links.first_requested_at <= excluded.requested_at - {SecurityLink.RequestWindow}
                        THEN excluded.requested_at
                        ELSE security_links.first_requested_at
                    END,
                    request_count = CASE
                        WHEN security_links.first_requested_at <= excluded.requested_at - {SecurityLink.RequestWindow}
                        THEN 1
                        ELSE security_links.request_count + 1
                    END
                WHERE security_links.requested_at
                        <= excluded.requested_at - {SecurityLink.MinimumIntervalBetweenRequests}
                    AND (security_links.first_requested_at <= excluded.requested_at - {SecurityLink.RequestWindow}
                        OR security_links.request_count < {SecurityLink.MaximumRequestsPerWindow})
                RETURNING user_id)
            INSERT INTO outbox_messages (id, topic, subject, type, payload, trace_parent, created_at)
            SELECT {requested.Id}, {requested.Topic}, {requested.Subject}, {requested.Type}, {requested.Payload},
                {requested.TraceParent}, {requested.CreatedAt}
            FROM renewed
            """,
            CancellationToken.None);

    public static async Task<DateTimeOffset?> IssuePasswordResetLinkAsync(
        this AppDbContext context,
        Guid userId,
        Guid requestId,
        byte[] tokenHash)
    {
        var expiries = await context.Database.SqlQuery<DateTimeOffset>(
            $"""
            UPDATE security_links
            SET token_hash = {tokenHash},
                issued_at = now(),
                expires_at = now() + {SecurityLink.PasswordResetLifetime}
            FROM users
            WHERE security_links.user_id = {userId}
                AND security_links.purpose = {SecurityLinkPurposes.PasswordReset}
                AND security_links.request_id = {requestId}
                AND security_links.requested_at > now() - {SecurityLink.PasswordResetRequestLifetime}
                AND users.id = security_links.user_id
                AND users.email = security_links.email
                AND users.organization_id IS NULL
            RETURNING security_links.expires_at AS "Value"
            """)
            .ToListAsync(CancellationToken.None);

        return expiries.Count == 0 ? null : expiries[0];
    }

    public static async Task<CompletedPasswordReset?> ResetPasswordWithLinkAsync(
        this AppDbContext context,
        Guid userId,
        byte[] tokenHash,
        string passwordHash,
        string securityStamp,
        TimeSpan probation)
    {
        await context.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM users WHERE id = {userId} FOR NO KEY UPDATE",
            CancellationToken.None);

        var previouslyConfirmed = await context.Database.SqlQuery<bool>(
            $"""
            WITH consumed AS (
                DELETE FROM security_links
                USING users
                WHERE security_links.user_id = {userId}
                    AND security_links.purpose = {SecurityLinkPurposes.PasswordReset}
                    AND security_links.token_hash = {tokenHash}
                    AND security_links.expires_at > now()
                    AND users.id = security_links.user_id
                    AND users.email = security_links.email
                    AND users.organization_id IS NULL
                RETURNING security_links.user_id)
            UPDATE users
            SET password_hash = {passwordHash},
                password_probation_ends_at = now() + {probation},
                security_stamp = {securityStamp},
                email_confirmed = true,
                access_failed_count = 0,
                lockout_end = NULL,
                current_password_failures = 0,
                current_password_lockout_end = NULL,
                concurrency_stamp = gen_random_uuid()::text
            FROM consumed
            WHERE users.id = consumed.user_id
            RETURNING old.email_confirmed AS "Value"
            """)
            .ToListAsync(CancellationToken.None);

        if (previouslyConfirmed.Count == 0)
        {
            return null;
        }

        await DeleteSecurityLinksAsync(context, userId);

        return new CompletedPasswordReset(previouslyConfirmed[0]);
    }

    public static Task<int> DeleteSpentPasswordResetRequestsAsync(
        this AppDbContext context,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM security_links
            WHERE (user_id, purpose) IN (
                SELECT user_id, purpose FROM security_links
                WHERE purpose = {SecurityLinkPurposes.PasswordReset}
                    AND first_requested_at <= now() - {SecurityLink.RequestWindow}
                    AND requested_at <= now() - {SecurityLink.RequestWindow}
                    AND (expires_at IS NULL OR expires_at <= now())
                LIMIT {PruningBatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);

    public static Task<int> DeleteSecurityLinksAsync(this AppDbContext context, Guid userId) =>
        context.Database.ExecuteSqlAsync(
            $"DELETE FROM security_links WHERE user_id = {userId}",
            CancellationToken.None);

    public static Task<int> ConfirmEmailWithLinkAsync(this AppDbContext context, Guid userId, byte[] tokenHash) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH confirmed AS (
                UPDATE users
                SET email_confirmed = true,
                    concurrency_stamp = gen_random_uuid()::text
                WHERE id = {userId} AND NOT email_confirmed
                    AND EXISTS (
                        SELECT 1 FROM security_links
                        WHERE security_links.user_id = users.id
                            AND security_links.purpose = {SecurityLinkPurposes.EmailConfirmation}
                            AND security_links.token_hash = {tokenHash}
                            AND security_links.expires_at > now()
                            AND security_links.email = users.email)
                RETURNING id)
            DELETE FROM security_links
            USING confirmed
            WHERE security_links.user_id = confirmed.id
                AND security_links.purpose = {SecurityLinkPurposes.EmailConfirmation}
            """,
            CancellationToken.None);

    public static Task<int> ConfirmEmailThroughProviderAsync(this AppDbContext context, Guid userId, string email) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH confirmed AS (
                UPDATE users
                SET email_confirmed = true,
                    concurrency_stamp = gen_random_uuid()::text
                WHERE id = {userId} AND email = {email} AND NOT email_confirmed
                RETURNING id)
            DELETE FROM security_links
            USING confirmed
            WHERE security_links.user_id = confirmed.id
                AND security_links.purpose = {SecurityLinkPurposes.EmailConfirmation}
            """,
            CancellationToken.None);
}
