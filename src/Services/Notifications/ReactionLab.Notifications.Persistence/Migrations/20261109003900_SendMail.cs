using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Notifications.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SendMail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.AddColumn<int>(
                name: "attempts",
                table: "mail_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "mail_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_failure",
                table: "mail_jobs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mail_deliveries",
                columns: table => new
                {
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mail_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mail_deliveries", x => new { x.recipient_id, x.id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_mail_jobs_due",
                table: "mail_jobs",
                column: "available_at",
                filter: "failed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_mail_jobs_failed",
                table: "mail_jobs",
                column: "failed_at",
                filter: "failed_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_mail_deliveries_sent_at",
                table: "mail_deliveries",
                column: "sent_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.DropTable(
                name: "mail_deliveries");

            migrationBuilder.DropIndex(
                name: "ix_mail_jobs_due",
                table: "mail_jobs");

            migrationBuilder.DropIndex(
                name: "ix_mail_jobs_failed",
                table: "mail_jobs");

            migrationBuilder.DropColumn(
                name: "attempts",
                table: "mail_jobs");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "mail_jobs");

            migrationBuilder.DropColumn(
                name: "last_failure",
                table: "mail_jobs");
        }
    }
}
