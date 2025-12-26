using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectsWebApp.DataAccsess.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkerReflextion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reflexion",
                table: "Markers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reflexion",
                table: "Markers");
        }
    }
}
