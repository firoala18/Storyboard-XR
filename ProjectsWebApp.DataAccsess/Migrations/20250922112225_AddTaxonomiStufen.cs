using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectsWebApp.DataAccsess.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxonomiStufen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Taxonomie",
                table: "Storyboards",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Taxonomie",
                table: "Storyboards");
        }
    }
}
