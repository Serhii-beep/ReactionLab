using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.CreateTable(
                name: "account_setups",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: true),
                    issued_in_session = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_setups", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_account_setups_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_in_session = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_changes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    district_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sign_in_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                    table.CheckConstraint("ck_organizations_district_id", "kind = 'school' OR district_id IS NULL");
                    table.CheckConstraint("ck_organizations_kind", "kind IN ('district', 'school')");
                    table.ForeignKey(
                        name: "fk_organizations_organizations_district_id",
                        column: x => x.district_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_school_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_school_roles", x => new { x.user_id, x.organization_id, x.role });
                    table.CheckConstraint(
                        "ck_user_school_roles_role",
                        "role IN ('school-admin', 'teacher', 'student', 'district-admin')");
                    table.ForeignKey(
                        name: "fk_user_school_roles_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_school_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_setups_token_hash",
                table: "account_setups",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_changes_organization_id_id",
                table: "organization_changes",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_organizations_district_id_id",
                table: "organizations",
                columns: new[] { "district_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_organizations_sign_in_code",
                table: "organizations",
                column: "sign_in_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_school_roles_organization_id_role",
                table: "user_school_roles",
                columns: new[] { "organization_id", "role" });

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organization_username",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE users
                    ADD CONSTRAINT fk_users_organizations_organization_id
                        FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE RESTRICT NOT VALID,
                    ADD CONSTRAINT ck_users_organization_username
                        CHECK ((organization_id IS NULL) = (organization_username IS NULL)) NOT VALID;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_organizations_organization_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "account_setups");

            migrationBuilder.DropTable(
                name: "organization_changes");

            migrationBuilder.DropTable(
                name: "user_school_roles");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_organization_username",
                table: "users");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "organization_username",
                table: "users");
        }
    }
}
