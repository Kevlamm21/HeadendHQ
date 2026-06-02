using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class TitleRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SportingEvents");

            migrationBuilder.CreateTable(
                name: "Titles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamingService = table.Column<int>(type: "INTEGER", nullable: false),
                    EventUrl = table.Column<string>(type: "TEXT", nullable: true),
                    AdbCommand = table.Column<string>(type: "TEXT", nullable: true),
                    DummyVideoPath = table.Column<string>(type: "TEXT", nullable: true),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata_Plot = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Tagline = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Year = table.Column<int>(type: "INTEGER", nullable: true),
                    Metadata_Studio = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Genre = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Rating = table.Column<float>(type: "REAL", nullable: true),
                    Metadata_ContentRating = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_UniqueId = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Season = table.Column<int>(type: "INTEGER", nullable: true),
                    Metadata_Episode = table.Column<int>(type: "INTEGER", nullable: true),
                    Metadata_ShowTitle = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_HomeTeam = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_AwayTeam = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Venue = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_League = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLive = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Titles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Titles");

            migrationBuilder.CreateTable(
                name: "SportingEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdbCommand = table.Column<string>(type: "TEXT", nullable: true),
                    DummyVideoPath = table.Column<string>(type: "TEXT", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EventUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    Sport = table.Column<int>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StreamingService = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportingEvents", x => x.Id);
                });
        }
    }
}
