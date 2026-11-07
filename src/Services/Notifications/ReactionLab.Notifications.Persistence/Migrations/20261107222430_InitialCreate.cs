using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Notifications.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dead_letters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic = table.Column<string>(type: "character varying(249)", maxLength: 249, nullable: false),
                    partition = table.Column<int>(type: "integer", nullable: false),
                    offset = table.Column<long>(type: "bigint", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payload = table.Column<byte[]>(type: "bytea", nullable: true),
                    trace_parent = table.Column<string>(type: "character varying(55)", maxLength: 55, nullable: true),
                    failure = table.Column<string>(type: "text", nullable: false),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dead_letters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "erasure_markers",
                columns: table => new
                {
                    subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    erased_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_erasure_markers", x => x.subject);
                });

            migrationBuilder.CreateTable(
                name: "mail_jobs",
                columns: table => new
                {
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mail_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trace_parent = table.Column<string>(type: "character varying(55)", maxLength: 55, nullable: true),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mail_jobs", x => new { x.recipient_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "notice_throttles",
                columns: table => new
                {
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mail_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    last_queued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notice_throttles", x => new { x.recipient_id, x.mail_type });
                });

            migrationBuilder.CreateTable(
                name: "processed_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_events", x => x.event_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dead_letters_failed_at",
                table: "dead_letters",
                column: "failed_at");

            migrationBuilder.CreateIndex(
                name: "ix_dead_letters_subject",
                table: "dead_letters",
                column: "subject");

            migrationBuilder.CreateIndex(
                name: "ix_erasure_markers_erased_at",
                table: "erasure_markers",
                column: "erased_at");

            migrationBuilder.CreateIndex(
                name: "ix_notice_throttles_last_queued_at",
                table: "notice_throttles",
                column: "last_queued_at");

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_processed_at",
                table: "processed_events",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letters");

            migrationBuilder.DropTable(
                name: "erasure_markers");

            migrationBuilder.DropTable(
                name: "mail_jobs");

            migrationBuilder.DropTable(
                name: "notice_throttles");

            migrationBuilder.DropTable(
                name: "processed_events");
        }
    }
}
