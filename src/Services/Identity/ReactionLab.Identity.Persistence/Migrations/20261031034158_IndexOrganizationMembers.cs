using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndexOrganizationMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_users_organization_id_id' AND NOT pg_index.indisvalid) THEN
                        PERFORM set_config('lock_timeout', '3s', true);
                        DROP INDEX ix_users_organization_id_id;
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_users_organization_id_id
                    ON users (organization_id, id)
                    WHERE organization_id IS NOT NULL;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                WITH removed AS (
                    DELETE FROM user_logins USING users
                    WHERE users.id = user_logins.user_id AND users.organization_id IS NOT NULL
                    RETURNING users.organization_id, user_logins.user_id)
                INSERT INTO organization_changes
                    (id, organization_id, change, account_id, role, changed_by, changed_in_session, changed_at)
                SELECT uuidv7(), organization_id, 'login-removed', user_id, NULL, NULL, NULL, clock_timestamp()
                FROM removed;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_users_organization_id_id'
                            AND pg_index.indisvalid AND pg_index.indisready) THEN
                        RAISE EXCEPTION 'An index on users was left unfinished. Run the migrator again to rebuild it.';
                    END IF;
                END $$;
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_users_organization_id_id;",
                suppressTransaction: true);
        }
    }
}
