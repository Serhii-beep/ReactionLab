using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MailSetupsAndNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.AddColumn<bool>(
                name: "accounts_may_reset_own_passwords",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "mailed_setup_count",
                table: "organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "mailed_setups_since",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "batch_id",
                table: "organization_changes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "token_hash",
                table: "account_setups",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "account_setups",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "first_requested_at",
                table: "account_setups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purpose",
                table: "account_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "request_count",
                table: "account_setups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "request_id",
                table: "account_setups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_at",
                table: "account_setups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sign_in_method_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    user_agent = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sign_in_method_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_sign_in_method_changes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_organization_username",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "mailed_setup_count",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "mailed_setups_since",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                "ALTER TABLE users ADD CONSTRAINT ck_users_organization_username CHECK ("
                + "(organization_id IS NULL AND organization_username IS NULL) OR (organization_id IS NOT NULL "
                + "AND (organization_username IS NOT NULL OR password_hash IS NULL))) NOT VALID;");

            migrationBuilder.CreateIndex(
                name: "ix_sign_in_method_changes_changed_at",
                table: "sign_in_method_changes",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "ix_sign_in_method_changes_user_id",
                table: "sign_in_method_changes",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.Sql("DELETE FROM account_setups WHERE token_hash IS NULL;");

            migrationBuilder.DropColumn(
                name: "accounts_may_reset_own_passwords",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "mailed_setup_count",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "mailed_setups_since",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "batch_id",
                table: "organization_changes");

            migrationBuilder.DropColumn(
                name: "email",
                table: "account_setups");

            migrationBuilder.DropColumn(
                name: "first_requested_at",
                table: "account_setups");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "account_setups");

            migrationBuilder.DropColumn(
                name: "request_count",
                table: "account_setups");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "account_setups");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "account_setups");

            migrationBuilder.AlterColumn<byte[]>(
                name: "token_hash",
                table: "account_setups",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "sign_in_method_changes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_organization_username",
                table: "users");

            migrationBuilder.DropColumn(
                name: "mailed_setup_count",
                table: "users");

            migrationBuilder.DropColumn(
                name: "mailed_setups_since",
                table: "users");

            migrationBuilder.Sql(
                "UPDATE users SET organization_username = 'invited-' || replace(id::text, '-', '') "
                + "WHERE organization_id IS NOT NULL AND organization_username IS NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_organization_username",
                table: "users",
                sql: "(organization_id IS NULL) = (organization_username IS NULL)");
        }
    }
}
