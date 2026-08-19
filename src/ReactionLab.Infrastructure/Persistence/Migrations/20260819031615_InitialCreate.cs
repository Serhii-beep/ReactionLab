using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "elements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    atomic_number = table.Column<int>(type: "integer", nullable: false),
                    symbol = table.Column<string>(type: "citext", maxLength: 3, nullable: false),
                    mass = table.Column<decimal>(type: "numeric(10,5)", precision: 10, scale: 5, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    state_at_room_temperature = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    electronegativity = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    melting_point = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    boiling_point = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    electron_configuration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    translations = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    search_text = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    periodic_group = table.Column<int>(type: "integer", nullable: true),
                    period = table.Column<int>(type: "integer", nullable: false),
                    covalent_radius_pm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    van_der_waals_radius_pm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_elements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    is_reversible = table.Column<bool>(type: "boolean", nullable: false),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    translations = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reactant_signature = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    search_text = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    catalyst = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    pressure_kpa = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    temperature_k = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    activation_energy_kj_per_mol = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    enthalpy_kj_per_mol = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    animation_duration_ms = table.Column<int>(type: "integer", nullable: true),
                    effect_preset_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "substances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_organic = table.Column<bool>(type: "boolean", nullable: false),
                    state_at_room_temperature = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(12,5)", precision: 12, scale: 5, nullable: true),
                    structure = table.Column<string>(type: "jsonb", nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    translations = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    search_text = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_substances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reaction_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    substance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    coefficient = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    reaction_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reaction_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_reaction_participants_reactions_reaction_id",
                        column: x => x.reaction_id,
                        principalTable: "reactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_elements_atomic_number",
                table: "elements",
                column: "atomic_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_elements_search_text",
                table: "elements",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_elements_symbol",
                table: "elements",
                column: "symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reaction_participants_reaction_id",
                table: "reaction_participants",
                column: "reaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_reaction_participants_substance_id",
                table: "reaction_participants",
                column: "substance_id");

            migrationBuilder.CreateIndex(
                name: "ix_reactions_reactant_signature",
                table: "reactions",
                column: "reactant_signature")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_reactions_search_text",
                table: "reactions",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_reactions_type",
                table: "reactions",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_substances_category",
                table: "substances",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_substances_formula",
                table: "substances",
                column: "formula");

            migrationBuilder.CreateIndex(
                name: "ix_substances_search_text",
                table: "substances",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "elements");

            migrationBuilder.DropTable(
                name: "reaction_participants");

            migrationBuilder.DropTable(
                name: "substances");

            migrationBuilder.DropTable(
                name: "reactions");
        }
    }
}
