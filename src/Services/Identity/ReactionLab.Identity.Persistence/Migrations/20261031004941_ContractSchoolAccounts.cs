using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContractSchoolAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH revoked AS (
                    DELETE FROM user_platform_roles USING users, platform_roles
                    WHERE users.id = user_platform_roles.user_id AND users.organization_id IS NOT NULL
                        AND platform_roles.id = user_platform_roles.role_id
                    RETURNING user_platform_roles.user_id, platform_roles.name)
                INSERT INTO platform_role_changes
                    (id, account_id, role, granted, source, changed_by, changed_in_session, changed_at)
                SELECT uuidv7(), user_id, name, false, 'school-account', NULL, NULL, clock_timestamp()
                FROM revoked;
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
                WITH ended AS (
                    UPDATE openiddict_authorizations SET status = 'revoked'
                    FROM users
                    WHERE openiddict_authorizations.subject = users.id::text AND users.organization_id IS NOT NULL
                        AND openiddict_authorizations.status = 'valid'
                    RETURNING openiddict_authorizations.id)
                UPDATE openiddict_tokens SET status = 'revoked'
                WHERE authorization_id = ANY (ARRAY(SELECT id FROM ended)) AND status = 'valid';
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_users_normalized_email;",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM users WHERE normalized_email IS NOT NULL
                        GROUP BY normalized_email HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'Two accounts share an address, so the old unique index can''t be rebuilt.';
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_users_normalized_email' AND NOT pg_index.indisvalid) THEN
                        PERFORM set_config('lock_timeout', '3s', true);
                        DROP INDEX ix_users_normalized_email;
                    END IF;
                END $$;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS ix_users_normalized_email
                    ON users (normalized_email);
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_index JOIN pg_class ON pg_class.oid = pg_index.indexrelid
                        WHERE pg_class.relname = 'ix_users_normalized_email'
                            AND pg_index.indisvalid AND pg_index.indisready) THEN
                        RAISE EXCEPTION 'An index on users was left unfinished. Run the migrator again to rebuild it.';
                    END IF;
                END $$;
                """,
                suppressTransaction: true);
        }
    }
}
