using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LimitCurrentPasswordChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "current_password_failures",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "current_password_lockout_end",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_password_failures",
                table: "users");

            migrationBuilder.DropColumn(
                name: "current_password_lockout_end",
                table: "users");
        }
    }
}
