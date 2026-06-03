using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleScraperSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnabledStreamingServices = table.Column<string>(type: "TEXT", nullable: false),
                    ScrapeWindowDays = table.Column<int>(type: "INTEGER", nullable: false),
                    CleanupRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    CronSchedule = table.Column<string>(type: "TEXT", nullable: false),
                    RunOnStartup = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ScheduleScraperSettings", x => x.Id));

            migrationBuilder.CreateTable(
                name: "DummyVideoSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LibraryPaths = table.Column<string>(type: "TEXT", nullable: false),
                    CronSchedule = table.Column<string>(type: "TEXT", nullable: false),
                    RunOnStartup = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DummyVideoSettings", x => x.Id));

            migrationBuilder.CreateTable(
                name: "HdHomerunSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    CronSchedule = table.Column<string>(type: "TEXT", nullable: false),
                    RunOnStartup = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_HdHomerunSettings", x => x.Id));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ScheduleScraperSettings");
            migrationBuilder.DropTable(name: "DummyVideoSettings");
            migrationBuilder.DropTable(name: "HdHomerunSettings");
        }
    }
}
