using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class AccountTakeoverStatements
{
    public static async Task<bool> TakeOverUnconfirmedAccountAsync(
        this AppDbContext context,
        Guid userId,
        string normalizedEmail,
        string securityStamp)
    {
        await context.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM users WHERE id = {userId} FOR NO KEY UPDATE",
            CancellationToken.None);

        var taken = await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET password_hash = NULL,
                password_probation_ends_at = NULL,
                security_stamp = {securityStamp},
                email_confirmed = true,
                access_failed_count = 0,
                lockout_end = NULL,
                current_password_failures = 0,
                current_password_lockout_end = NULL,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId} AND organization_id IS NULL AND normalized_email = {normalizedEmail}
                AND password_hash IS NOT NULL AND NOT email_confirmed
            """,
            CancellationToken.None);

        return taken == 1;
    }
}
