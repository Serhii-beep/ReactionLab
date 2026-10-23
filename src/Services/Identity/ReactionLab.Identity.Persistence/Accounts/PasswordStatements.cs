using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class PasswordStatements
{
    public static Task<int> SetPasswordAsync(
        this AppDbContext context,
        Guid userId,
        string passwordHash,
        DateTimeOffset probationEndsAt) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET password_hash = {passwordHash},
                password_probation_ends_at = {probationEndsAt},
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId} AND normalized_email IS NOT NULL AND lockout_end IS DISTINCT FROM 'infinity'
                AND (password_hash IS NULL OR password_hash = {passwordHash})
            """,
            CancellationToken.None);
}
