using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class LoginStatements
{
    public static async Task<int> ConnectLoginAsync(
        this AppDbContext context,
        Guid userId,
        Guid sessionId,
        string provider,
        string providerKey,
        string providerDisplayName)
    {
        await context.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM users WHERE id = {userId} FOR SHARE",
            CancellationToken.None);

        return await context.Database.ExecuteSqlAsync(
            $"""
            WITH session AS MATERIALIZED (
                SELECT id FROM openiddict_authorizations
                WHERE id = {sessionId} AND subject = {userId.ToString()} AND status = 'valid'
                FOR SHARE)
            INSERT INTO user_logins (login_provider, provider_key, provider_display_name, user_id)
            SELECT {provider}, {providerKey}, {providerDisplayName}, {userId} FROM session
            ON CONFLICT DO NOTHING
            """,
            CancellationToken.None);
    }

    public static Task<int> AddLoginAsync(
        this AppDbContext context,
        Guid userId,
        string provider,
        string providerKey,
        string providerDisplayName) =>
        context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO user_logins (login_provider, provider_key, provider_display_name, user_id)
            VALUES ({provider}, {providerKey}, {providerDisplayName}, {userId})
            ON CONFLICT DO NOTHING
            """,
            CancellationToken.None);
}
