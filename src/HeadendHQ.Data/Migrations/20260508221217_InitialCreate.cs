using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HdHomerunDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceAuth = table.Column<string>(type: "TEXT", nullable: false),
                    XmltvUrl = table.Column<string>(type: "TEXT", nullable: false),
                    XmltvContent = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HdHomerunDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SportingEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Sport = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamingService = table.Column<int>(type: "INTEGER", nullable: false),
                    EventUrl = table.Column<string>(type: "TEXT", nullable: false),
                    AdbCommand = table.Column<string>(type: "TEXT", nullable: true),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportingEvents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HdHomerunDevices");

            migrationBuilder.DropTable(
                name: "SportingEvents");
        }
    }
}
