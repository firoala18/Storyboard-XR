using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectsWebApp.DataAccsess.Migrations
{
    /// <inheritdoc />
    public partial class AddImagesToPfadAndSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "LernSteps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "LernFlows",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "LernSteps");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "LernFlows");
        }
    }
}
