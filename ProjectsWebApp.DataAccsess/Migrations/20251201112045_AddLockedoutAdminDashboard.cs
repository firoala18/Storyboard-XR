using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjectsWebApp.DataAccsess.Migrations
{
    /// <inheritdoc />
    public partial class AddLockedoutAdminDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSecurityStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LockoutCount = table.Column<int>(type: "integer", nullable: false),
                    IsManuallyLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LastLockoutUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurityStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityStates_UserId",
                table: "UserSecurityStates",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSecurityStates");
        }
    }
}
