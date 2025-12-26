using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectsWebApp.DataAccsess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactEmail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactEmail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatenschutzContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    SectionType = table.Column<string>(type: "text", nullable: false),
                    ContentHtml = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatenschutzContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImpressumContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    SectionType = table.Column<string>(type: "text", nullable: false),
                    ContentHtml = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpressumContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KontaktCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Funktion = table.Column<string>(type: "text", nullable: true),
                    KontaktDatenHtml = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KontaktCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeichteSpracheContent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContentHtml = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeichteSpracheContent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MakerSpaceDescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    SubTitle = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MakerSpaceDescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MakerSpaceProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    ProjectUrl = table.Column<string>(type: "text", nullable: false),
                    Top = table.Column<bool>(type: "boolean", nullable: false),
                    Forschung = table.Column<bool>(type: "boolean", nullable: false),
                    download = table.Column<bool>(type: "boolean", nullable: false),
                    tutorial = table.Column<bool>(type: "boolean", nullable: false),
                    events = table.Column<bool>(type: "boolean", nullable: false),
                    netzwerk = table.Column<bool>(type: "boolean", nullable: false),
                    lesezeichen = table.Column<bool>(type: "boolean", nullable: false),
                    ITRecht = table.Column<bool>(type: "boolean", nullable: false),
                    Beitraege = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MakerSpaceProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MitmachenContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SectionType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitmachenContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortalCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortalVideo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VideoPath = table.Column<string>(type: "text", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ImagePath = table.Column<string>(type: "text", nullable: true),
                    ShowImageInsteadOfVideo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalVideo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SliderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsForVirtuellesKlassenzimmer = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SliderItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Storyboards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: true),
                    Zielgruppe = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Beschreibung = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Lernziel = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Farbpalette = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: true),
                    PublicId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EditKeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Readonly = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AccessTokenView = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AccessTokenEdit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OwnerTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Storyboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UebersichtContent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContentHtml = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UebersichtContent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UrheberechtContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    SectionType = table.Column<string>(type: "text", nullable: false),
                    ContentHtml = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrheberechtContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryboardId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ImagePath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scenes_Storyboards_StoryboardId",
                        column: x => x.StoryboardId,
                        principalTable: "Storyboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Markers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    X = table.Column<double>(type: "double precision", precision: 6, scale: 4, nullable: false),
                    Y = table.Column<double>(type: "double precision", precision: 6, scale: 4, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Ziel = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Datenablage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PromptIdee = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SceneId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Markers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Markers_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ContactEmail",
                columns: new[] { "Id", "Email" },
                values: new object[] { 1, "h.seehagen-marx@uni-wuppertal.de" });

            migrationBuilder.InsertData(
                table: "DatenschutzContents",
                columns: new[] { "Id", "ContentHtml", "DisplayOrder", "SectionType", "Title" },
                values: new object[,]
                {
                    { 1, "Dies ist der Einleitungstext für das Impressum.", 1, "Text", "Datenschutz Einleitung" },
                    { 2, "Name und Anschrift des Verantwortlichen...", 2, "Accordion", "Verantwortlich" }
                });

            migrationBuilder.InsertData(
                table: "ImpressumContents",
                columns: new[] { "Id", "ContentHtml", "DisplayOrder", "SectionType", "Title" },
                values: new object[,]
                {
                    { 1, "Dies ist der Einleitungstext für das Impressum.", 1, "Text", "Impressum Einleitung" },
                    { 2, "Name und Anschrift des Verantwortlichen...", 2, "Accordion", "Verantwortlich" }
                });

            migrationBuilder.InsertData(
                table: "KontaktCards",
                columns: new[] { "Id", "DisplayOrder", "Funktion", "ImageUrl", "KontaktDatenHtml", "Name" },
                values: new object[] { 1, 1, "Leitung MediaLab", "/images/Kontakt/Heike_Seehagen-Marx.jpg", "Bergische Universität Wuppertal, Zentrum für Informations- und Medienverarbeitung (ZIM), Medienlabor (Leitung)", "Dr. Heike Seehagen-Marx" });

            migrationBuilder.InsertData(
                table: "LeichteSpracheContent",
                columns: new[] { "Id", "ContentHtml" },
                values: new object[] { 1, "@{\r\n    ViewData[\"Title\"] = \"Leichtesprache\";\r\n}\r\n\r\n<div class=\"container mt-5\">\r\n    <div class=\"row justify-content-center\">\r\n        <div class=\"col-md-10\">\r\n            <!-- Title -->\r\n            <h2 class=\"mb-4 \" style=\"color:#90bc14\">Projekte im MediaLab (MediaLab-Projekte)</h2>\r\n\r\n            <!-- Introduction -->\r\n            <p class=\"lead\">\r\n                Die Webseite \"MediaLab-Projekte\" präsentiert die Ergebnisse der gemeinsamen Projekte im MediaLab in einer übersichtlichen Projektbibliothek.\r\n            </p>\r\n\r\n            <p class=\"lead\">\r\n                Das MediaLab an der Bergischen Universität Wuppertal ist ein kreativer Raum, in dem Studierende, Lehrende und Forschende ihre Ideen umsetzen, neue Technologien testen und Prototypen entwickeln können.\r\n            </p>\r\n\r\n            <!-- Section: Lehrende -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Für Lehrende – Ihre Chancen im MediaLab</h2>\r\n            <ul class=\"list-group list-group-flush mb-4\">\r\n                <li class=\"list-group-item\"><strong>Ideen umsetzen:</strong> Bringen Sie Ihre Ideen ins MediaLab und setzen Sie sie gemeinsam mit anderen um.</li>\r\n                <li class=\"list-group-item\"><strong>Prototypen entwickeln:</strong> Machen Sie aus Ihren Ideen Modelle, die Ihre Konzepte zeigen und weiterentwickeln.</li>\r\n                <li class=\"list-group-item\"><strong>Neue Lösungen testen:</strong> Probieren Sie neue Technologien und Methoden aus und testen Sie, ob sie gut funktionieren.</li>\r\n                <li class=\"list-group-item\"><strong>Zusammenarbeiten:</strong> Arbeiten Sie mit Studierenden und Kolleg*innen an innovativen Lösungen.</li>\r\n            </ul>\r\n\r\n            <!-- Section: Studierende -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Für Studierende – Ihre Chancen im MediaLab</h2>\r\n            <ul class=\"list-group list-group-flush mb-4\">\r\n                <li class=\"list-group-item\"><strong>Praktika und Abschlussarbeiten:</strong> Nutzen Sie das MediaLab für spannende Themen, die Theorie und Praxis verbinden.</li>\r\n                <li class=\"list-group-item\"><strong>Seminararbeiten:</strong> Arbeiten Sie an praktischen Aufgaben und bringen Sie kreative Ideen ein.</li>\r\n                <li class=\"list-group-item\"><strong>Hilfskraftstellen:</strong> Engagieren Sie sich im MediaLab und sammeln Sie wertvolle Praxiserfahrung.</li>\r\n                <li class=\"list-group-item\"><strong>Eigene Ideen umsetzen:</strong> Haben Sie eine Idee? Nutzen Sie das MediaLab, um Ihre Projekte umzusetzen.</li>\r\n            </ul>\r\n\r\n            <!-- Section: Forschende -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Für Forschende – Ihre Möglichkeiten im MediaLab</h2>\r\n            <ul class=\"list-group list-group-flush mb-4\">\r\n                <li class=\"list-group-item\"><strong>Projekte starten:</strong> Starten Sie eigene Forschung und arbeiten Sie mit anderen Disziplinen zusammen.</li>\r\n                <li class=\"list-group-item\"><strong>Neue Technologien testen:</strong> Nutzen Sie die Ausstattung im MediaLab, um neue Ideen und Technologien auszuprobieren.</li>\r\n                <li class=\"list-group-item\"><strong>Förderprojekte umsetzen:</strong> Holen Sie sich Unterstützung für Projekte mit Fördermitteln.</li>\r\n                <li class=\"list-group-item\"><strong>Forschung teilen:</strong> Stellen Sie Ihre Forschungsergebnisse auf der Webseite vor.</li>\r\n            </ul>\r\n\r\n            <!-- Section: Mitmachen -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Wie können Sie mitmachen?</h2>\r\n            <ul class=\"list-group list-group-flush mb-4\">\r\n                <li class=\"list-group-item\"><strong>Eigene Projekte einreichen:</strong> Haben Sie eine Idee? Reichen Sie sie ein und arbeiten Sie mit uns zusammen.</li>\r\n                <li class=\"list-group-item\"><strong>Bestehende Projekte unterstützen:</strong> Schließen Sie sich laufenden Projekten an und bringen Sie Ihre Stärken ein.</li>\r\n                <li class=\"list-group-item\"><strong>Angebote nutzen:</strong> Bewerben Sie sich auf Praktika, Hilfskraftstellen oder nutzen Sie das MediaLab für Ihre Abschlussarbeit.</li>\r\n                <li class=\"list-group-item\"><strong>Jetzt aktiv werden:</strong> Das MediaLab freut sich auf Ihre Ideen und Ihr Engagement! Kontaktieren Sie uns!</li>\r\n            </ul>\r\n\r\n            <!-- Contact Section -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Kontakt</h2>\r\n            <p class=\"mb-0\">\r\n                <strong>Dr. Heike Seehagen-Marx</strong><br>\r\n                <a href=\"mailto:h.seehagen-marx@uni-wuppertal.de\" class=\"text-decoration-none text-primary\">h.seehagen-marx@uni-wuppertal.de</a>\r\n            </p>\r\n        </div>\r\n    </div>\r\n</div>\r\n" });

            migrationBuilder.InsertData(
                table: "MakerSpaceDescriptions",
                columns: new[] { "Id", "Content", "SubTitle", "Title" },
                values: new object[] { 1, " In unserem digitalen Makerspace findest du kuratierte Links, inspirierende Impulse und praxisnahe Ressourcen rund um die Entwicklung von Extended Reality (XR). Ob du gerade erst einsteigst oder bereits eigene Projekte realisierst – hier bekommst du Zugang zu Tools, Tutorials, Frameworks und Ideen, die dich bei der Umsetzung deiner XR-Vision unterstützen.\r\n\r\n                    Tauche ein in die Welt von Virtual Reality (VR), Augmented Reality (AR) und Mixed Reality (MR) – von ersten Prototypen bis hin zu fortgeschrittenen Anwendungen. Der Makerspace ist dein Startpunkt für Experimente, Austausch und technologische Kreativität.", "Kuratierte Links, Impulse und Ressourcen für die Entwicklung von XR-Lehr- und Lernmedien", "Willkommen im ToolBar" });

            migrationBuilder.InsertData(
                table: "MakerSpaceProjects",
                columns: new[] { "Id", "Beitraege", "Description", "DisplayOrder", "Forschung", "ITRecht", "ImageUrl", "ProjectUrl", "Tags", "Title", "Top", "download", "events", "lesezeichen", "netzwerk", "tutorial" },
                values: new object[,]
                {
                    { 1, false, "A low-cost 3D printed prosthetic hand designed for children. Fully open-source and customizable.", 0, false, false, null, "https://example.com/prosthetic-hand", "3D Printing, Prosthetics, Open Source", "3D Printed Prosthetic Hand", false, false, false, false, false, false },
                    { 2, false, "An automatic watering system for plants using soil moisture sensors and Arduino.", 0, false, false, null, "https://example.com/smart-watering", "IoT, Arduino, Sensors, Plants", "Smart Plant Watering System", false, false, false, false, false, false },
                    { 3, false, "Build your own CNC milling machine using affordable components and open hardware designs.", 0, false, false, null, "https://example.com/desktop-cnc", "CNC, DIY, Fabrication, Open Hardware", "DIY Desktop CNC Machine", false, false, false, false, false, false },
                    { 4, false, "A smart assistant using Raspberry Pi and voice recognition libraries to execute custom commands.", 0, false, false, null, "https://example.com/voice-pi", "Raspberry Pi, Voice Recognition, Python, AI", "Voice-Controlled Assistant with Raspberry Pi", false, false, false, false, false, false }
                });

            migrationBuilder.InsertData(
                table: "MitmachenContents",
                columns: new[] { "Id", "Content", "DisplayOrder", "SectionType", "Title" },
                values: new object[,]
                {
                    { 2, "Das MediaLab der Bergischen Universität Wuppertal ist ein inspirierender Makerspace, die kreativen Köpfe aus unterschiedlichen Disziplinen zusammenbringt. Hier haben Studierende, Lehrende und Forschende die Möglichkeit, ihre Ideen in die Praxis umzusetzen, Prototypen zu entwickeln und neue Technologien auszuprobieren. Mit einer gut ausgestatteten Infrastruktur und einem interdisziplinären Ansatz bietet das MediaLab einen idealen Raum, um an zukunftsorientierten Projekten zu arbeiten und gemeinsam innovative Lösungen zu entwickeln.", 0, "Card", "Von der Idee zur Umsetzung: Projekte im MediaLab" },
                    { 3, "Ihre Ideen realisieren: Nutzen Sie das MediaLab, um technologische, kreative oder didaktische Ansätze in Form von Prototypen zu verwirklichen. Von der Skizze zum Modell: Entwickeln Sie konkrete Prototypen, die Ihre Konzepte anschaulich machen und eine Weiterentwicklung erleichtern. Innovative Lösungen testen: Experimentieren Sie mit neuen Ansätzen und Technologien, um ihre Praxistauglichkeit zu evaluieren. Förderprojekte mit Prototypen stärken: Prototypen als Grundlage: Entwickeln Sie funktionale Modelle, die Förderprojekten eine klare und überzeugende Basis bieten. Lehrmethoden modellieren: Erstellen Sie Prototypen für digitale Tools, didaktische Konzepte oder virtuelle Formate. Testen und optimieren: Evaluieren Sie die Wirksamkeit Ihrer Ideen in einer experimentellen Umgebung, bevor sie in der Lehre angewendet werden. Didaktische Konzepte visualisieren: Praxisnah und anschaulich: Entwickeln Sie Prototypen, die komplexe didaktische Ansätze verständlich machen. Gemeinsam gestalten: Arbeiten Sie mit Kolleg*innen, Forschenden und Studierenden zusammen, um Konzepte zu entwickeln, die den Anforderungen der digitalen Transformation gerecht werden.", 0, "Accordion", "Für Lehrende – Ihre Chancen im MediaLab" },
                    { 4, "Praktika und Abschlussarbeiten: Nutzen Sie das MediaLab als Ausgangspunkt für spannende Themen, die Praxis und Wissenschaft verbinden. Entwickeln Sie innovative Lösungen und lassen Sie Ihre Abschlussarbeit Teil eines realen Projekts werden. Seminararbeiten: Arbeiten Sie im Rahmen Ihrer Seminare an praxisnahen Aufgaben, die interdisziplinäre Ansätze fördern und kreative Technologien nutzen. Ihre Arbeit kann Impulse für zukünftige Projekte setzen. Hilfskraftstellen: Engagieren Sie sich als studentische Hilfskraft im MediaLab und unterstützen Sie spannende Projekte. Dabei erweitern Sie Ihre Kenntnisse in einem inspirierenden Umfeld und sammeln wertvolle Praxiserfahrung. Einfach aus Interesse: Haben Sie eine eigene Idee oder möchten Sie Teil eines kreativen Teams sein? Das MediaLab bietet Ihnen Raum, Unterstützung und eine Community, um Ihrer Leidenschaft nachzugehen – auch unabhängig von Ihrem Studium.", 0, "Accordion", "Für Studierende – Ihre Chancen im MediaLab" },
                    { 5, "Interdisziplinäre Projekte initiieren: Starten Sie eigene Forschungsvorhaben, die verschiedene Disziplinen miteinander verbinden. Das MediaLab fördert die Zusammenarbeit zwischen Fachbereichen und schafft Synergien für zukunftsweisende Lösungen. Technologische Innovationen erkunden: Nutzen Sie die Ausstattung und Expertise des MediaLabs, um mit vielfältigen Technologien zu experimentieren und innovative Forschungsansätze zu entwickeln. Förderprojekte realisieren: Das MediaLab bietet Unterstützung bei der Konzeption und Umsetzung von Drittmittelprojekten. Von der Antragstellung bis zur Durchführung – wir begleiten Sie bei jedem Schritt. Forschung sichtbar machen: Präsentieren Sie Ihre Ergebnisse auf dem Projekte-Portal und teilen Sie Ihre Arbeit mit einer breiten Community. Ihre Forschung wird Teil eines Netzwerks, das Innovation und Wissenstransfer fördert. Praxisorientierte Lösungen entwickeln: Arbeiten Sie an anwendungsorientierten Konzepten, die nicht nur in der Wissenschaft, sondern auch in Gesellschaft und Wirtschaft einen Unterschied machen.", 0, "Accordion", "Für Forschende – Ihre Möglichkeiten im MediaLab" },
                    { 6, "Eigene Projekte einreichen: Haben Sie eine Idee? Reichen Sie diese ein und setzen Sie sie gemeinsam mit dem MediaLab-Team um. Bestehende Projekte unterstützen: Schließen Sie sich laufenden Projekten an und bringen Sie Ihre Stärken ein. Angebote nutzen: Bewerben Sie sich auf Praktika, Hilfskraftstellen oder nutzen Sie das MediaLab als Basis für Ihre Abschlussarbeit. Jetzt aktiv werden und die digitale Zukunft mitgestalten: Auf Kontakt Verweisen Das MediaLab freut sich auf Ihre Ideen, Ihr Engagement und Ihre Neugier!", 0, "Accordion", "Wie können Sie mitmachen?" }
                });

            migrationBuilder.InsertData(
                table: "PortalCards",
                columns: new[] { "Id", "Content", "DisplayOrder", "Title" },
                values: new object[,]
                {
                    { 1, "Das MediaLab des ZIM an der Bergischen Universität Wuppertal (BUW) ist ein zentraler Makerspace, der interdisziplinären Lehre und Forschung fördert. Mit unserem neuen Projekte-Portal möchten wir die Vielfalt und den Impact unserer Arbeit sichtbar machen. Das Portal bietet einen umfassenden Überblick über unsere interdisziplinären Projekte und zeigt unser Engagement für Transparenz, Nachhaltigkeit und die aktive Mitgestaltung der digitalen Transformation. Hier erhalten Sie Einblicke in die innovativen Projekte, die durch die Zusammenarbeit von Studierenden, Forschenden und Lehrenden entstehen.", 0, "Projekte und Ideen sichtbar machen" },
                    { 2, "Das MediaLab ist eine zentrale Anlaufstelle für Akteur*innen aus verschiedenen Disziplinen. Das Portal bietet eine sichtbare Plattform, die Kooperationspartner, Projektergebnisse und interdisziplinären Austausch in den Fokus stellt. So entstehen neue Impulse für Zusammenarbeit und Innovation.", 0, "Sichtbarkeit der Netzwerker" },
                    { 3, "Die Projekte im Portal repräsentieren nachhaltige Konzepte und praxisorientierte Innovationen, die über Disziplinen hinauswirken. Sie werden langfristig zugänglich gemacht, weiterentwickelt und in neue Kontexte übertragen, was den Wissenstransfer und die Förderung interdisziplinärer Expertise unterstützten.", 0, "Nachhaltige Konzepte für die Zukunft" },
                    { 4, "Von Open Educational Resources (OER)-Initiativen bis zu aktuellen Projekten wie „Kollaborativ Biodiversität entdecken“ zeigt das Portal die Vielfalt der MediaLab-Projekte. Diese verdeutlichen, wie innovative Technologien und interdisziplinäre Zusammenarbeit Lehre und Forschung bereichern.", 0, "Projektförderungen im Fokus" },
                    { 5, "Das Projekte-Portal ist eine zentrale Plattform, die Transparenz, Nachhaltigkeit und die Sichtbarkeit von Netzwerken fördert. Es zeigt, wie das MediaLab als Raum für Co-Creation und kreative Zusammenarbeit die digitale Zukunft der BUW aktiv mitgestaltet. Alle Universitätsangehörigen sind eingeladen, die Vielfalt der Projekte zu entdecken und gemeinsam an neuen Lösungen zu arbeiten.\r\n", 0, "Gemeinsam Zukunft gestalten" }
                });

            migrationBuilder.InsertData(
                table: "SliderItems",
                columns: new[] { "Id", "Description", "DisplayOrder", "ImageUrl", "IsForVirtuellesKlassenzimmer", "Title" },
                values: new object[,]
                {
                    { 1, "Globale Innovation trifft auf nachhaltiges Lernen <br /> erleben Sie immersive digitale Erlebnisse, die den Campus der Zukunft gestalten.", 0, "/images/FirstSlider1.png", false, "Virtueller Campus" },
                    { 2, "Fördern Sie internationale Zusammenarbeit und gestalten Sie die Zukunft  <br /> des digitalen Forschens in einem virtuellen Raum.", 0, "/images/FirstSlider2.png", false, "Virtueller Campus" },
                    { 3, "Tauchen Sie ein in die Welt der BUW  <br /> entdecken Sie die Universität interaktiv in einer virtuellen Rundreise.", 0, "/images/FirstSlider3.png", false, "360° BUW Tour" },
                    { 4, "Erkunden Sie die 'Gallery of Walk'  <br /> eine interaktive Reise durch nachhaltige Visualisierungen von Lehre und Forschung.", 0, "/images/FirstSlider4.png", false, "360°-Rundgang – GSA Konvent" },
                    { 5, "Erleben Sie innovative Posterpräsentationen  <br /> ein interaktiver 360°-Rundgang durch die neuesten Forschungsergebnisse.", 0, "/images/FirstSlider5.png", false, "360°-Rundgang – GSA Konvent" },
                    { 6, "Schützen Sie Salamander in einem innovativen virtuellen Game  <br /> ein interaktives Labor zur Rettung gefährdeter Amphibienarten.", 0, "/images/FirstSlider6.png", false, "Amphibienschutz in virtuellen 3D-Räumen" },
                    { 7, "Erforschen Sie den Schutz gefährdeter Amphibien durch forschendes Lernen im virtuellen Labor  <br /> ein einzigartiger Ansatz für Artenschutz.", 0, "/images/FirstSlider7.png", false, "Amphibienschutz in virtuellen 3D-Räumen" },
                    { 8, "Bildung für nachhaltige Entwicklung durch Open Educational Resources  <br /> Wissen für die Zukunft zugänglich und nachhaltig vermitteln.", 0, "/images/FirstSlider8.png", false, "BNE OER" },
                    { 9, "Testen Sie innovative Unterrichtsszenarien in virtuellen Lernräumen  <br /> die Mathematik der Zukunft erleben.", 0, "/images/FirstSlidera9.png", false, "Virtuelle Mathematik" },
                    { 10, "Erproben Sie didaktische Szenarien  <br /> entwickeln Sie hybride Lehr-Lernszenarien und erforschen Sie die Verbindung zwischen virtuellen und physischen Lernwelten.", 0, "/images/FirstSlider10.png", false, "Virtuelle Räume für die Bildung und Forschung" },
                    { 11, "Mit KI zur präzisen Identifikation von Salamandern  <br /> nachhaltig die Forschung und den Schutz gefährdeter Arten vorantreiben.", 0, "/images/FirstSlider11.png", false, "Salamander-KI-Mustererkennungssoftware" },
                    { 12, "Globale Innovation trifft auf nachhaltiges Lernen <br /> erleben Sie immersive digitale Erlebnisse, die den Campus der Zukunft gestalten.", 0, "/images/FirstSlider1.png", true, "Virtueller Campus" }
                });

            migrationBuilder.InsertData(
                table: "UebersichtContent",
                columns: new[] { "Id", "ContentHtml" },
                values: new object[] { 1, "\r\n@{\r\n    ViewData[\"Title\"] = \"Übersicht\";\r\n}\r\n\r\n<div class=\"container mt-5\">\r\n    <div class=\"row justify-content-center\">\r\n        <div class=\"col-md-10\">\r\n\r\n            <!-- Title -->\r\n            <h2 class=\"mb-4\" style=\"color:#90bc14\">Übersicht der Projekte</h2>\r\n\r\n            <!-- Introduction -->\r\n            <p class=\"lead\">\r\n                Hier finden Sie eine Übersicht über die verschiedenen Projekte im MediaLab und BioVersum.\r\n            </p>\r\n\r\n            <p>\r\n                Jedes Projekt ist einzigartig und bietet spannende Einblicke in aktuelle Entwicklungen, Forschungsthemen und kreative Lösungen.\r\n            </p>\r\n\r\n            <!-- Sections -->\r\n            <h2 class=\"mt-5\" style=\"color:#90bc14\">Projektkategorien</h2>\r\n            <ul class=\"list-group list-group-flush mb-4\">\r\n                <li class=\"list-group-item\"><strong>Lehre:</strong> Projekte, die zur Unterstützung von Lehrveranstaltungen entwickelt wurden.</li>\r\n                <li class=\"list-group-item\"><strong>Forschung:</strong> Forschungsbasierte Projekte zur Entwicklung neuer Erkenntnisse und Methoden.</li>\r\n                <li class=\"list-group-item\"><strong>Studentische Arbeiten:</strong> Abschlussarbeiten, Seminarprojekte und Praktika.</li>\r\n                <li class=\"list-group-item\"><strong>Offene Projekte:</strong> Projekte, an denen Studierende und Lehrende gemeinsam arbeiten können.</li>\r\n            </ul>\r\n\r\n            <!-- Call to action -->\r\n            <h2 class=\"mt-5\" style=\"color:#90bc14\">Mitmachen und mehr erfahren</h2>\r\n            <p>\r\n                Wenn Sie mehr über ein Projekt erfahren oder sich beteiligen möchten, wenden Sie sich bitte an das MediaLab-Team.\r\n            </p>\r\n\r\n            <!-- Contact Section -->\r\n            <h2 class=\"mt-5\" style=\"color:#90bc14\">Kontakt</h2>\r\n            <p>\r\n                <strong>Dr. Heike Seehagen-Marx</strong><br>\r\n                <a href=\"mailto:h.seehagen-marx@uni-wuppertal.de\" class=\"text-decoration-none text-primary\">h.seehagen-marx@uni-wuppertal.de</a>\r\n            </p>\r\n\r\n        </div>\r\n    </div>\r\n</div>\r\n" });

            migrationBuilder.InsertData(
                table: "UrheberechtContents",
                columns: new[] { "Id", "ContentHtml", "DisplayOrder", "SectionType", "Title" },
                values: new object[,]
                {
                    { 1, "Dies ist der Einleitungstext für das Impressum.", 1, "Text", "Datenschutz Einleitung" },
                    { 2, "Name und Anschrift des Verantwortlichen...", 2, "Accordion", "Verantwortlich" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Markers_SceneId",
                table: "Markers",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_StoryboardId",
                table: "Scenes",
                column: "StoryboardId");

            migrationBuilder.CreateIndex(
                name: "IX_Storyboards_PublicId",
                table: "Storyboards",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ContactEmail");

            migrationBuilder.DropTable(
                name: "DatenschutzContents");

            migrationBuilder.DropTable(
                name: "ImpressumContents");

            migrationBuilder.DropTable(
                name: "KontaktCards");

            migrationBuilder.DropTable(
                name: "LeichteSpracheContent");

            migrationBuilder.DropTable(
                name: "MakerSpaceDescriptions");

            migrationBuilder.DropTable(
                name: "MakerSpaceProjects");

            migrationBuilder.DropTable(
                name: "Markers");

            migrationBuilder.DropTable(
                name: "MitmachenContents");

            migrationBuilder.DropTable(
                name: "PortalCards");

            migrationBuilder.DropTable(
                name: "PortalVideo");

            migrationBuilder.DropTable(
                name: "RegistrationCodes");

            migrationBuilder.DropTable(
                name: "SliderItems");

            migrationBuilder.DropTable(
                name: "UebersichtContent");

            migrationBuilder.DropTable(
                name: "UrheberechtContents");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Scenes");

            migrationBuilder.DropTable(
                name: "Storyboards");
        }
    }
}
