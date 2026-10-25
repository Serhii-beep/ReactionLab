using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndexSchoolAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE users VALIDATE CONSTRAINT fk_users_organizations_organization_id;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "ALTER TABLE users VALIDATE CONSTRAINT ck_users_organization_username;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_users_regular_account_normalized_email'
                            AND NOT pg_index.indisvalid) THEN
                        PERFORM set_config('lock_timeout', '3s', true);
                        DROP INDEX ix_users_regular_account_normalized_email;
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS ix_users_regular_account_normalized_email
                    ON users (normalized_email)
                    WHERE organization_id IS NULL;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_users_organization_id_normalized_email'
                            AND NOT pg_index.indisvalid) THEN
                        PERFORM set_config('lock_timeout', '3s', true);
                        DROP INDEX ix_users_organization_id_normalized_email;
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS ix_users_organization_id_normalized_email
                    ON users (organization_id, normalized_email)
                    WHERE organization_id IS NOT NULL AND normalized_email IS NOT NULL;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_users_organization_id_organization_username'
                            AND NOT pg_index.indisvalid) THEN
                        PERFORM set_config('lock_timeout', '3s', true);
                        DROP INDEX ix_users_organization_id_organization_username;
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS ix_users_organization_id_organization_username
                    ON users (organization_id, organization_username)
                    WHERE organization_id IS NOT NULL;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF (SELECT count(*) FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname IN ('ix_users_regular_account_normalized_email',
                                'ix_users_organization_id_normalized_email',
                                'ix_users_organization_id_organization_username')
                            AND pg_index.indisvalid AND pg_index.indisready) <> 3 THEN
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
                "DROP INDEX CONCURRENTLY IF EXISTS ix_users_organization_id_organization_username;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_users_organization_id_normalized_email;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_users_regular_account_normalized_email;",
                suppressTransaction: true);
        }
    }
}
