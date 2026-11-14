using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ValidateUsernamesAndIndexBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "SELECT pg_advisory_lock(hashtext('ix_organization_changes_batch_id'));",
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_organization_changes_batch_id' AND NOT pg_index.indisvalid) THEN
                        PERFORM set_config('lock_timeout', '3s', true);
                        DROP INDEX ix_organization_changes_batch_id;
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_organization_changes_batch_id
                    ON organization_changes (batch_id)
                    WHERE batch_id IS NOT NULL;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_organization_changes_batch_id' AND NOT pg_index.indisvalid) THEN
                        RAISE EXCEPTION 'ix_organization_changes_batch_id is invalid; run the migrator again.';
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    PERFORM set_config('lock_timeout', '3s', true);
                    ALTER TABLE users VALIDATE CONSTRAINT ck_users_organization_username;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                "SELECT pg_advisory_unlock(hashtext('ix_organization_changes_batch_id'));",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_organization_changes_batch_id;",
                suppressTransaction: true);
        }
    }
}
