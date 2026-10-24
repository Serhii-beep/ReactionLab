using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_role_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    granted = table.Column<bool>(type: "boolean", nullable: false),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_in_session = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_role_changes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_platform_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_platform_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_platform_roles_platform_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "platform_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_platform_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "platform_roles",
                columns: new[] { "id", "concurrency_stamp", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("0199f0c8-6a35-7b52-9c1e-3f4a8d2b6e01"), "0199f0c8-6a35-7b52-9c1e-3f4a8d2b6e01", "admin", "ADMIN" },
                    { new Guid("0199f0c8-6a35-7b52-9c1e-3f4a8d2b6e02"), "0199f0c8-6a35-7b52-9c1e-3f4a8d2b6e02", "moderator", "MODERATOR" },
                    { new Guid("0199f0c8-6a35-7b52-9c1e-3f4a8d2b6e03"), "0199f0c8-6a35-7b52-9c1e-3f4a8d2b6e03", "catalog-editor", "CATALOG-EDITOR" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_platform_role_changes_account_id_id",
                table: "platform_role_changes",
                columns: new[] { "account_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_platform_roles_normalized_name",
                table: "platform_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_platform_roles_role_id",
                table: "user_platform_roles",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_role_changes");

            migrationBuilder.DropTable(
                name: "user_platform_roles");

            migrationBuilder.DropTable(
                name: "platform_roles");
        }
    }
}
