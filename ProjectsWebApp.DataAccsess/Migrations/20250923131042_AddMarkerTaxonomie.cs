using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectsWebApp.DataAccsess.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkerTaxonomie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Taxonomie",
                table: "Markers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Taxonomie",
                table: "Markers");
        }
    }
}
