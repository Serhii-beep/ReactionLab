using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Maintenance;

public static class MaintenanceRunStatements
{
    public static async Task<DateTimeOffset?> TryLeaseMaintenanceRunAsync(
        this AppDbContext context,
        string name,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        var leasedUntil = await context.Database
            .SqlQuery<DateTimeOffset>(
                $"""
                INSERT INTO maintenance_runs AS run (name, due_at, leased_until)
                VALUES ({name}, now(), now() + {lease})
                ON CONFLICT (name) DO UPDATE SET leased_until = excluded.leased_until
                WHERE run.due_at <= now() AND (run.leased_until IS NULL OR run.leased_until < now())
                RETURNING run.leased_until AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return leasedUntil.Count == 1 ? leasedUntil[0] : null;
    }

    public static Task<int> ReleaseMaintenanceRunAsync(
        this AppDbContext context,
        string name,
        DateTimeOffset leasedUntil,
        TimeSpan nextRunIn) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE maintenance_runs SET due_at = now() + {nextRunIn}, leased_until = NULL
            WHERE name = {name} AND leased_until = {leasedUntil}
            """,
            CancellationToken.None);
}
