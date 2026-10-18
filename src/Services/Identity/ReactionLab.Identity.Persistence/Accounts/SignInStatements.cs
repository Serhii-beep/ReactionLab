using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class SignInStatements
{
    public static async Task<bool> ClaimSignInAttemptAsync(
        this AppDbContext context,
        Guid userId,
        LockoutSchedule schedule,
        DateTimeOffset now)
    {
        var claimed = await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET access_failed_count = access_failed_count + 1,
                lockout_end = CASE
                    WHEN NOT lockout_enabled THEN lockout_end
                    WHEN access_failed_count + 1 >= {schedule.FailuresBeforeDisabling} THEN 'infinity'
                    WHEN (access_failed_count + 1) % {schedule.FailuresPerLockout} = 0
                        THEN {now} + {schedule.FirstLockout} * power(2, (access_failed_count + 1) / {schedule.FailuresPerLockout} - 1)
                    ELSE lockout_end
                END,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId} AND (NOT lockout_enabled OR lockout_end IS NULL OR lockout_end <= {now})
            """,
            CancellationToken.None);

        return claimed == 1;
    }

    public static Task ResetSignInAttemptsAsync(this AppDbContext context, Guid userId) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET access_failed_count = 0,
                lockout_end = NULL,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId}
            """,
            CancellationToken.None);

    public static Task ReplacePasswordHashAsync(
        this AppDbContext context,
        Guid userId,
        string previousHash,
        string replacementHash) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET password_hash = {replacementHash},
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {userId} AND password_hash = {previousHash}
            """,
            CancellationToken.None);
}
