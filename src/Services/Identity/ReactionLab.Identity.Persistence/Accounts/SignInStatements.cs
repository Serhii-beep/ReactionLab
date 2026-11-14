using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class SignInStatements
{
    public static async Task<bool> ClaimSignInAttemptAsync(
        this AppDbContext context,
        Guid userId,
        LockoutSchedule schedule,
        DateTimeOffset now,
        LockAnnouncement announcement)
    {
        var (locked, disabled) = announcement;
        var claimed = await context.Database.SqlQuery<int>(
            $"""
            WITH claimed AS (
                UPDATE users
                SET access_failed_count = access_failed_count + 1,
                    lockout_end = CASE
                        WHEN NOT lockout_enabled THEN lockout_end
                        WHEN access_failed_count + 1 >= {schedule.FailuresBeforeDisabling} THEN 'infinity'
                        WHEN (access_failed_count + 1) % {schedule.FailuresPerLockout} = 0
                            THEN {now} + {schedule.FirstLockout}
                                * power(2, (access_failed_count + 1) / {schedule.FailuresPerLockout} - 1)
                        ELSE lockout_end
                    END,
                    concurrency_stamp = gen_random_uuid()::text
                WHERE id = {userId} AND (NOT lockout_enabled OR lockout_end IS NULL OR lockout_end <= {now})
                RETURNING organization_id, lockout_enabled, access_failed_count),
            locked AS (
                INSERT INTO outbox_messages (id, topic, subject, type, payload, trace_parent, created_at)
                SELECT {locked.Id}, {locked.Topic}, {locked.Subject}, {locked.Type}, {locked.Payload},
                    {locked.TraceParent}, {locked.CreatedAt}
                FROM claimed
                WHERE claimed.organization_id IS NULL AND claimed.lockout_enabled
                    AND claimed.access_failed_count % {schedule.FailuresPerLockout} = 0
                    AND claimed.access_failed_count < {schedule.FailuresBeforeDisabling}
                RETURNING id),
            disabled AS (
                INSERT INTO outbox_messages (id, topic, subject, type, payload, trace_parent, created_at)
                SELECT {disabled.Id}, {disabled.Topic}, {disabled.Subject}, {disabled.Type}, {disabled.Payload},
                    {disabled.TraceParent}, {disabled.CreatedAt}
                FROM claimed
                WHERE claimed.organization_id IS NULL AND claimed.lockout_enabled
                    AND claimed.access_failed_count = {schedule.FailuresBeforeDisabling}
                RETURNING id)
            SELECT count(*)::int AS "Value" FROM claimed
            """)
            .ToListAsync(CancellationToken.None);

        return claimed[0] == 1;
    }

    public static async Task<bool> ClaimCurrentPasswordCheckAsync(
        this AppDbContext context,
        Guid userId,
        LockoutSchedule schedule,
        DateTimeOffset now,
        LockAnnouncement announcement)
    {
        var (locked, disabled) = announcement;
        var claimed = await context.Database.SqlQuery<int>(
            $"""
            WITH claimed AS (
                UPDATE users
                SET current_password_failures = current_password_failures + 1,
                    current_password_lockout_end = CASE
                        WHEN current_password_failures + 1 >= {schedule.FailuresBeforeDisabling} THEN 'infinity'
                        WHEN (current_password_failures + 1) % {schedule.FailuresPerLockout} = 0
                            THEN {now} + {schedule.FirstLockout}
                                * power(2, (current_password_failures + 1) / {schedule.FailuresPerLockout} - 1)
                        ELSE current_password_lockout_end
                    END,
                    concurrency_stamp = gen_random_uuid()::text
                WHERE id = {userId}
                    AND (current_password_lockout_end IS NULL OR current_password_lockout_end <= {now})
                RETURNING organization_id, current_password_failures),
            locked AS (
                INSERT INTO outbox_messages (id, topic, subject, type, payload, trace_parent, created_at)
                SELECT {locked.Id}, {locked.Topic}, {locked.Subject}, {locked.Type}, {locked.Payload},
                    {locked.TraceParent}, {locked.CreatedAt}
                FROM claimed
                WHERE claimed.organization_id IS NULL
                    AND claimed.current_password_failures % {schedule.FailuresPerLockout} = 0
                    AND claimed.current_password_failures < {schedule.FailuresBeforeDisabling}
                RETURNING id),
            disabled AS (
                INSERT INTO outbox_messages (id, topic, subject, type, payload, trace_parent, created_at)
                SELECT {disabled.Id}, {disabled.Topic}, {disabled.Subject}, {disabled.Type}, {disabled.Payload},
                    {disabled.TraceParent}, {disabled.CreatedAt}
                FROM claimed
                WHERE claimed.organization_id IS NULL
                    AND claimed.current_password_failures = {schedule.FailuresBeforeDisabling}
                RETURNING id)
            SELECT count(*)::int AS "Value" FROM claimed
            """)
            .ToListAsync(CancellationToken.None);

        return claimed[0] == 1;
    }

    public static Task ResetSignInAttemptsAsync(this AppDbContext context, Guid userId) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET access_failed_count = 0,
                lockout_end = CASE WHEN lockout_end = 'infinity' THEN lockout_end END,
                current_password_failures = 0,
                current_password_lockout_end = CASE
                    WHEN current_password_lockout_end = 'infinity' THEN current_password_lockout_end
                END,
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
