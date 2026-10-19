using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReactionLab.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndexSessionsBySubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_openiddict_authorizations_valid;",
                suppressTransaction: true);

            migrationBuilder.CreateIndex(
                name: "ix_openiddict_authorizations_valid",
                table: "openiddict_authorizations",
                columns: new[] { "subject", "id" },
                filter: "status = 'valid'")
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_openiddict_authorizations_valid",
                table: "openiddict_authorizations");
        }
    }
}
