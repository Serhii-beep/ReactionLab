using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class SignInMethodChangeStatements
{
    public const int PruningBatchSize = 1000;

    public static Task<int> RecordSignInMethodChangeAsync(
        this AppDbContext context,
        Guid changeId,
        Guid userId,
        string change,
        string? provider,
        string? userAgent) =>
        context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO sign_in_method_changes (id, user_id, change, provider, changed_at, user_agent)
            VALUES ({changeId}, {userId}, {change}, {provider}, now(), {userAgent})
            ON CONFLICT (id) DO NOTHING
            """,
            CancellationToken.None);

    public static Task<int> DeleteExpiredSignInMethodChangesAsync(
        this AppDbContext context,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM sign_in_method_changes
            WHERE id IN (
                SELECT id FROM sign_in_method_changes
                WHERE changed_at < now() - {SignInMethodChange.Retention}
                ORDER BY changed_at
                LIMIT {PruningBatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);
}
