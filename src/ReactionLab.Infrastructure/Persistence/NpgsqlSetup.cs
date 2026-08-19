using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace ReactionLab.Infrastructure.Persistence;

public static class NpgsqlSetup
{
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    public static void Configure(NpgsqlDbContextOptionsBuilder npgsql)
    {
        npgsql.MigrationsHistoryTable(MigrationsHistoryTable);
        npgsql.EnableRetryOnFailure();
    }
}
