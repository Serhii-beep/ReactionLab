using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class PasswordChangeStatements
{
    public const int PruningBatchSize = 1000;

    public static Task<int> RecordPasswordChangeAsync(
        this AppDbContext context,
        Guid changeId,
        Guid userId,
        string? userAgent) =>
        context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO password_changes (id, user_id, changed_at, user_agent)
            VALUES ({changeId}, {userId}, now(), {userAgent})
            ON CONFLICT (id) DO NOTHING
            """,
            CancellationToken.None);

    public static Task<int> DeleteExpiredPasswordChangesAsync(
        this AppDbContext context,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM password_changes
            WHERE id IN (
                SELECT id FROM password_changes
                WHERE changed_at < now() - {PasswordChange.Retention}
                ORDER BY changed_at
                LIMIT {PruningBatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);
}
