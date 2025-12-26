using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectsWebApp.DataAccsess.Migrations
{
    /// <inheritdoc />
    public partial class AddLicceneToBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Zielgruppe",
                table: "Storyboards",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Storyboards",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Lernziel",
                table: "Storyboards",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Beschreibung",
                table: "Storyboards",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<string>(
                name: "Authors",
                table: "Storyboards",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "License",
                table: "Storyboards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseExtras",
                table: "Storyboards",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Authors",
                table: "Storyboards");

            migrationBuilder.DropColumn(
                name: "License",
                table: "Storyboards");

            migrationBuilder.DropColumn(
                name: "LicenseExtras",
                table: "Storyboards");

            migrationBuilder.AlterColumn<string>(
                name: "Zielgruppe",
                table: "Storyboards",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Storyboards",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Lernziel",
                table: "Storyboards",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Beschreibung",
                table: "Storyboards",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
