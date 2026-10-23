using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.OpenId;

public static class SessionStatements
{
    public static async Task<RevokeSessionOutcome> RevokeSessionAsync(
        this AppDbContext context,
        string subject,
        Guid actingSessionId,
        Guid sessionId) =>
        (await context.Database.SqlQuery<RevokeSessionOutcome>(
            $"""
            WITH locked AS MATERIALIZED (
                SELECT id, status FROM openiddict_authorizations
                WHERE id IN ({actingSessionId}, {sessionId}) AND subject = {subject}
                ORDER BY id
                FOR NO KEY UPDATE),
            acting AS (
                SELECT 1 FROM locked WHERE id = {actingSessionId} AND status = 'valid'),
            ended AS (
                UPDATE openiddict_authorizations SET status = 'revoked'
                WHERE id = {sessionId} AND EXISTS (SELECT 1 FROM acting)
                    AND EXISTS (SELECT 1 FROM locked WHERE id = {sessionId} AND status = 'valid')
                RETURNING id),
            ended_tokens AS (
                UPDATE openiddict_tokens SET status = 'revoked'
                WHERE authorization_id = ANY (ARRAY(SELECT id FROM ended)) AND status = 'valid')
            SELECT EXISTS (SELECT 1 FROM acting) AS acting_session_valid,
                EXISTS (SELECT 1 FROM locked WHERE id = {sessionId}) AS session_found,
                EXISTS (SELECT 1 FROM ended) AS session_ended
            """)
            .ToListAsync(CancellationToken.None))
            .Single();

    public static async Task<RevokeOtherSessionsOutcome> RevokeOtherSessionsAsync(
        this AppDbContext context,
        string subject,
        Guid actingSessionId) =>
        (await context.Database.SqlQuery<RevokeOtherSessionsOutcome>(
            $"""
            WITH locked AS MATERIALIZED (
                SELECT id FROM openiddict_authorizations
                WHERE subject = {subject} AND status = 'valid'
                ORDER BY id
                FOR NO KEY UPDATE),
            acting AS (
                SELECT 1 FROM locked WHERE id = {actingSessionId}),
            ended AS (
                UPDATE openiddict_authorizations SET status = 'revoked'
                WHERE id IN (SELECT id FROM locked) AND id <> {actingSessionId} AND EXISTS (SELECT 1 FROM acting)
                RETURNING id),
            ended_tokens AS (
                UPDATE openiddict_tokens SET status = 'revoked'
                WHERE authorization_id = ANY (ARRAY(SELECT id FROM ended)) AND status = 'valid')
            SELECT EXISTS (SELECT 1 FROM acting) AS acting_session_valid,
                (SELECT count(*)::int FROM ended) AS ended_session_count
            """)
            .ToListAsync(CancellationToken.None))
            .Single();

    public static async Task<int> RevokeOldestSessionsAsync(
        this AppDbContext context,
        string subject,
        int keptSessionCount) =>
        (await context.Database.SqlQuery<int>(
            $"""
            WITH excess AS (
                SELECT id FROM openiddict_authorizations
                WHERE subject = {subject} AND status = 'valid'
                ORDER BY properties::jsonb -> {SessionProperties.SignedInAt} DESC NULLS LAST, id
                OFFSET {keptSessionCount}),
            locked AS MATERIALIZED (
                SELECT id FROM openiddict_authorizations
                WHERE id IN (SELECT id FROM excess) AND status = 'valid'
                ORDER BY id
                FOR NO KEY UPDATE),
            ended AS (
                UPDATE openiddict_authorizations SET status = 'revoked'
                WHERE id IN (SELECT id FROM locked)
                RETURNING id),
            ended_tokens AS (
                UPDATE openiddict_tokens SET status = 'revoked'
                WHERE authorization_id = ANY (ARRAY(SELECT id FROM ended)) AND status = 'valid')
            SELECT count(*)::int AS "Value" FROM ended
            """)
            .ToListAsync(CancellationToken.None))
            .Single();

    public static Task<int> KeepSessionThroughStampChangeAsync(
        this AppDbContext context,
        string subject,
        Guid sessionId,
        string previousStampHash,
        string securityStampHash) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE openiddict_authorizations
            SET properties = ((properties::jsonb - {SessionProperties.StampCarriedOverBy}) || jsonb_build_object(
                {SessionProperties.SecurityStampHash}, {securityStampHash},
                {SessionProperties.StampCarryOver}, {securityStampHash}))::text
            WHERE id = {sessionId} AND subject = {subject} AND status = 'valid'
                AND properties::jsonb ->> {SessionProperties.SecurityStampHash} = {previousStampHash}
            """,
            CancellationToken.None);

    public static async Task<string?> CarrySessionOverStampChangeAsync(
        this AppDbContext context,
        Guid userId,
        Guid sessionId,
        string tokenId) =>
        (await context.Database.SqlQuery<string>(
            $"""
            WITH account AS MATERIALIZED (
                SELECT security_stamp FROM users WHERE id = {userId} FOR SHARE)
            UPDATE openiddict_authorizations
            SET properties = (properties::jsonb
                || jsonb_build_object({SessionProperties.StampCarriedOverBy}, {tokenId}))::text
            FROM account
            WHERE openiddict_authorizations.id = {sessionId} AND openiddict_authorizations.status = 'valid'
                AND properties::jsonb ->> {SessionProperties.StampCarryOver}
                    = upper(encode(sha256(convert_to(account.security_stamp, 'UTF8')), 'hex'))
                AND coalesce(properties::jsonb ->> {SessionProperties.StampCarriedOverBy}, {tokenId}) = {tokenId}
            RETURNING account.security_stamp AS "Value"
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();

    public static async Task<int> ClaimPasswordCheckAsync(
        this AppDbContext context,
        string subject,
        Guid sessionId,
        int maximumChecks) =>
        (await context.Database.SqlQuery<int>(
            $"""
            UPDATE openiddict_authorizations
            SET properties = (properties::jsonb || jsonb_build_object(
                {SessionProperties.PasswordChecks},
                coalesce((properties::jsonb ->> {SessionProperties.PasswordChecks})::int, 0) + 1))::text
            WHERE id = {sessionId} AND subject = {subject} AND status = 'valid'
                AND coalesce((properties::jsonb ->> {SessionProperties.PasswordChecks})::int, 0) < {maximumChecks}
            RETURNING (properties::jsonb ->> {SessionProperties.PasswordChecks})::int AS "Value"
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();

    public static Task<int> MarkPasswordVerifiedAsync(
        this AppDbContext context,
        string subject,
        Guid sessionId,
        DateTimeOffset verifiedAt) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE openiddict_authorizations
            SET properties = ((properties::jsonb - {SessionProperties.PasswordChecks}) || jsonb_build_object(
                {SessionProperties.PasswordVerifiedAt}, {verifiedAt.ToUnixTimeSeconds()}))::text
            WHERE id = {sessionId} AND subject = {subject} AND status = 'valid'
            """,
            CancellationToken.None);

    public static async Task<bool> LockValidSessionAsync(this AppDbContext context, string subject, Guid sessionId) =>
        (await context.Database.SqlQuery<bool>(
            $"""
            SELECT true AS "Value" FROM openiddict_authorizations
            WHERE id = {sessionId} AND subject = {subject} AND status = 'valid'
            FOR SHARE
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();
}
