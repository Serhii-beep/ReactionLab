using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackSignInMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE users ADD COLUMN IF NOT EXISTS password_probation_ends_at timestamp with time zone;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "ALTER TABLE user_logins ADD COLUMN IF NOT EXISTS connected_at timestamp with time zone;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "ALTER TABLE user_logins ALTER COLUMN connected_at SET DEFAULT now();",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_user_logins_user_id_login_provider;",
                suppressTransaction: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id_login_provider",
                table: "user_logins",
                columns: new[] { "user_id", "login_provider" },
                unique: true)
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_user_logins_user_id;",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_logins_user_id_login_provider",
                table: "user_logins");

            migrationBuilder.DropColumn(
                name: "password_probation_ends_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "connected_at",
                table: "user_logins");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                table: "user_logins",
                column: "user_id");
        }
    }
}
