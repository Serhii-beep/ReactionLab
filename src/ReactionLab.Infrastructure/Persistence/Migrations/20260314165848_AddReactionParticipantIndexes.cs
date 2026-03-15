using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReactionParticipantIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReactionParticipants_ElementId",
                table: "ReactionParticipants");

            migrationBuilder.DropIndex(
                name: "IX_ReactionParticipants_MoleculeId",
                table: "ReactionParticipants");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionParticipants_ElementId_Role",
                table: "ReactionParticipants",
                columns: new[] { "ElementId", "Role" },
                filter: "\"Role\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionParticipants_MoleculeId_Role",
                table: "ReactionParticipants",
                columns: new[] { "MoleculeId", "Role" },
                filter: "\"Role\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReactionParticipants_ElementId_Role",
                table: "ReactionParticipants");

            migrationBuilder.DropIndex(
                name: "IX_ReactionParticipants_MoleculeId_Role",
                table: "ReactionParticipants");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionParticipants_ElementId",
                table: "ReactionParticipants",
                column: "ElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionParticipants_MoleculeId",
                table: "ReactionParticipants",
                column: "MoleculeId");
        }
    }
}
