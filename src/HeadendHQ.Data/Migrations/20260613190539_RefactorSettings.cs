using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DummyVideoSettings");

            migrationBuilder.DropTable(
                name: "ScheduleScraperSettings");

            migrationBuilder.DropColumn(
                name: "DummyVideoPath",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "CronSchedule",
                table: "HdHomerunSettings");

            migrationBuilder.DropColumn(
                name: "RunOnStartup",
                table: "HdHomerunSettings");

            migrationBuilder.RenameColumn(
                name: "PosterPath",
                table: "Titles",
                newName: "VodLauncherPath");

            migrationBuilder.AddColumn<bool>(
                name: "ArtworkCreated",
                table: "Titles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnabledStreamingServices = table.Column<string>(type: "TEXT", nullable: false),
                    TitleRetentionDays = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleScrapingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScrapeWindowDays = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleScrapingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VodLauncherSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LibraryPaths = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VodLauncherSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "ScheduleScrapingSettings");

            migrationBuilder.DropTable(
                name: "VodLauncherSettings");

            migrationBuilder.DropColumn(
                name: "ArtworkCreated",
                table: "Titles");

            migrationBuilder.RenameColumn(
                name: "VodLauncherPath",
                table: "Titles",
                newName: "PosterPath");

            migrationBuilder.AddColumn<string>(
                name: "DummyVideoPath",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CronSchedule",
                table: "HdHomerunSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RunOnStartup",
                table: "HdHomerunSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DummyVideoSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CronSchedule = table.Column<string>(type: "TEXT", nullable: false),
                    LibraryPaths = table.Column<string>(type: "TEXT", nullable: false),
                    RunOnStartup = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DummyVideoSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleScraperSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CleanupRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    CronSchedule = table.Column<string>(type: "TEXT", nullable: false),
                    EnabledStreamingServices = table.Column<string>(type: "TEXT", nullable: false),
                    RunOnStartup = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScrapeWindowDays = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleScraperSettings", x => x.Id);
                });
        }
    }
}
