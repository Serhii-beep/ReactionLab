using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdministerSchools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "disabled_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locale",
                table: "organizations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<bool>(
                name: "teachers_may_reset_student_passwords",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "time_zone",
                table: "organizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Etc/UTC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "locale",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "teachers_may_reset_student_passwords",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "time_zone",
                table: "organizations");
        }
    }
}
