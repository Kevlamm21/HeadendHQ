using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropSourceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourceSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SourceSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscoverySportSlugs = table.Column<string>(type: "TEXT", nullable: false),
                    JitterMs = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxConcurrency = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxTeamLogoLookupsPerRun = table.Column<int>(type: "INTEGER", nullable: false),
                    MinDelayMs = table.Column<int>(type: "INTEGER", nullable: false),
                    PerRunRequestBudget = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestsPerMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceSettings", x => x.Id);
                });
        }
    }
}
