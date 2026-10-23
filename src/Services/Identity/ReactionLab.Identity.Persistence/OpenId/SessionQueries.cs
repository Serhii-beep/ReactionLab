using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.OpenId;

public static class SessionQueries
{
    public static async Task<IReadOnlyList<LiveSession>> ListLiveSessionsAsync(
        this AppDbContext context,
        string subject,
        Guid before,
        int count,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await context.Database.SqlQuery<LiveSession>(
            $"""
            SELECT session.id,
                session.properties::jsonb ->> {SessionProperties.Device} AS device,
                session.properties::jsonb ->> {SessionProperties.Address} AS address,
                session.creation_date AS signed_in_at,
                refresh_token.creation_date AS last_active_at,
                refresh_token.expiration_date AS expires_at
            FROM openiddict_authorizations AS session
            CROSS JOIN LATERAL (
                SELECT creation_date, expiration_date FROM openiddict_tokens
                WHERE authorization_id = session.id AND status = 'valid'
                    AND type = 'urn:ietf:params:oauth:token-type:refresh_token' AND expiration_date > {now}
                ORDER BY creation_date DESC
                LIMIT 1) AS refresh_token
            WHERE session.subject = {subject} AND session.status = 'valid' AND session.id < {before}
            ORDER BY session.id DESC
            LIMIT {count}
            """)
            .ToListAsync(cancellationToken);

    public static async Task<ReauthenticationState?> FindReauthenticationStateAsync(
        this AppDbContext context,
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        (await context.Database.SqlQuery<ReauthenticationState>(
            $"""
            SELECT account.password_hash IS NOT NULL AS account_has_password,
                account.password_probation_ends_at,
                session.id IS NOT NULL AS session_valid,
                to_timestamp((session.properties::jsonb ->> {SessionProperties.PasswordVerifiedAt})::bigint)
                    AS password_verified_at
            FROM users AS account
            LEFT JOIN openiddict_authorizations AS session
                ON session.id = {sessionId} AND session.subject = account.id::text AND session.status = 'valid'
            WHERE account.id = {userId}
            """)
            .ToListAsync(cancellationToken))
            .SingleOrDefault();
}
