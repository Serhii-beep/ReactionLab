using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmErasures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.CreateTable(
                name: "account_erasures",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    escalated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_erasures", x => x.user_id);
                });

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
                name: "maintenance_runs",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    leased_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_runs", x => x.name);
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

            migrationBuilder.CreateTable(
                name: "erasure_confirmations",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_erasure_confirmations", x => new { x.user_id, x.service });
                    table.ForeignKey(
                        name: "fk_erasure_confirmations_account_erasures_user_id",
                        column: x => x.user_id,
                        principalTable: "account_erasures",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_erasures_completed_at",
                table: "account_erasures",
                column: "completed_at",
                filter: "completed_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_account_erasures_incomplete_requested_at",
                table: "account_erasures",
                column: "requested_at",
                filter: "completed_at IS NULL");

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
                name: "ix_processed_events_processed_at",
                table: "processed_events",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.DropTable(
                name: "dead_letters");

            migrationBuilder.DropTable(
                name: "erasure_confirmations");

            migrationBuilder.DropTable(
                name: "erasure_markers");

            migrationBuilder.DropTable(
                name: "maintenance_runs");

            migrationBuilder.DropTable(
                name: "processed_events");

            migrationBuilder.DropTable(
                name: "account_erasures");
        }
    }
}
