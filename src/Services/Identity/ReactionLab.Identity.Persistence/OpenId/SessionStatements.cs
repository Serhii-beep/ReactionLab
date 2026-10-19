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
}
