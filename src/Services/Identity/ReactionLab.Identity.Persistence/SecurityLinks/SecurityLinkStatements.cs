using Microsoft.EntityFrameworkCore;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Persistence.SecurityLinks;

public static class SecurityLinkStatements
{
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
                expires_at = now() + {SecurityLink.EmailConfirmationLifetime}
            FROM users
            WHERE security_links.user_id = {userId}
                AND security_links.purpose = {SecurityLinkPurposes.EmailConfirmation}
                AND security_links.request_id = {requestedId}
                AND security_links.requested_at > now() - {SecurityLink.RequestLifetime}
                AND users.id = security_links.user_id
                AND users.email = security_links.email
                AND NOT users.email_confirmed
            RETURNING security_links.expires_at AS "Value"
            """)
            .ToListAsync(CancellationToken.None);

        return expires.Count == 0 ? null : expires[0];
    }

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
