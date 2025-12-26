using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjectsWebApp.DataAccsess.Migrations
{
    /// <inheritdoc />
    public partial class AddLernpfad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LernFlows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OwnerId = table.Column<string>(type: "text", nullable: true),
                    PublicId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EditKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OwnerTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LernFlows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LernSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    LernFlowId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LernSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LernSteps_LernFlows_LernFlowId",
                        column: x => x.LernFlowId,
                        principalTable: "LernFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LernFlows_PublicId",
                table: "LernFlows",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LernSteps_LernFlowId",
                table: "LernSteps",
                column: "LernFlowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LernSteps");

            migrationBuilder.DropTable(
                name: "LernFlows");
        }
    }
}
