using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndexPasswordResets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_users_unconfirmed_created_at' AND NOT pg_index.indisvalid) THEN
                        PERFORM set_config('lock_timeout', '3s', true);
                        DROP INDEX ix_users_unconfirmed_created_at;
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_users_unconfirmed_created_at
                    ON users (created_at)
                    WHERE organization_id IS NULL AND email IS NOT NULL AND NOT email_confirmed
                        AND NOT email_confirmation_exempt;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_security_links_token_hash' AND NOT pg_index.indisvalid) THEN
                        PERFORM set_config('lock_timeout', '3s', true);
                        DROP INDEX ix_security_links_token_hash;
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS ix_security_links_token_hash
                    ON security_links (token_hash)
                    WHERE token_hash IS NOT NULL;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF (SELECT count(*) FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname IN ('ix_users_unconfirmed_created_at', 'ix_security_links_token_hash')
                            AND pg_index.indisvalid AND pg_index.indisready) <> 2 THEN
                        RAISE EXCEPTION 'An index was left unfinished. Run the migrator again to rebuild it.';
                    END IF;
                END $$;
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_security_links_token_hash;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_users_unconfirmed_created_at;",
                suppressTransaction: true);
        }
    }
}
