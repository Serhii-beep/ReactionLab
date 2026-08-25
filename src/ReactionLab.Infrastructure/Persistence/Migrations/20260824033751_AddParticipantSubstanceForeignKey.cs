using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantSubstanceForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "fk_reaction_participants_substances_substance_id",
                table: "reaction_participants",
                column: "substance_id",
                principalTable: "substances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reaction_participants_substances_substance_id",
                table: "reaction_participants");
        }
    }
}
