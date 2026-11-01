using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "users",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    topic = table.Column<string>(type: "character varying(249)", maxLength: 249, nullable: false),
                    subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    payload = table.Column<string>(type: "text", nullable: true),
                    trace_parent = table.Column<string>(type: "character varying(55)", maxLength: 55, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_messages_tombstone", "(type IS NULL) = (payload IS NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_subject",
                table: "outbox_messages",
                column: "subject");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_topic_position",
                table: "outbox_messages",
                columns: new[] { "topic", "position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET LOCAL lock_timeout = '3s';");
            migrationBuilder.Sql("LOCK TABLE outbox_messages IN ACCESS EXCLUSIVE MODE;");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM outbox_messages) THEN
                        RAISE EXCEPTION 'The outbox still holds events; let the relay publish them first.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "version",
                table: "users");
        }
    }
}
