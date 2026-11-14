using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Notifications.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrioritizeLinkMail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.AddColumn<bool>(
                name: "urgent",
                table: "mail_jobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_mail_jobs_urgent_due",
                table: "mail_jobs",
                column: "available_at",
                filter: "failed_at IS NULL AND urgent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.DropIndex(
                name: "ix_mail_jobs_urgent_due",
                table: "mail_jobs");

            migrationBuilder.DropColumn(
                name: "urgent",
                table: "mail_jobs");
        }
    }
}
