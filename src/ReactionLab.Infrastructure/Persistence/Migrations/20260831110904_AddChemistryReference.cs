using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChemistryReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chemistry_reference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chemistry_reference", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chemistry_reference_key",
                table: "chemistry_reference",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chemistry_reference");
        }
    }
}
