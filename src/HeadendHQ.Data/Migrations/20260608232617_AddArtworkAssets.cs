using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PosterPath",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeagueAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    League = table.Column<int>(type: "INTEGER", nullable: false),
                    Variant = table.Column<string>(type: "TEXT", nullable: false),
                    LogoData = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StreamingServiceAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Service = table.Column<int>(type: "INTEGER", nullable: false),
                    LogoData = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingServiceAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamName = table.Column<string>(type: "TEXT", nullable: false),
                    League = table.Column<int>(type: "INTEGER", nullable: false),
                    LogoData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    PrimaryColorHex = table.Column<string>(type: "TEXT", nullable: true),
                    SecondaryColorHex = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WordMarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    League = table.Column<int>(type: "INTEGER", nullable: false),
                    Variant = table.Column<string>(type: "TEXT", nullable: false),
                    LogoData = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordMarks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueAssets_League_Variant",
                table: "LeagueAssets",
                columns: new[] { "League", "Variant" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StreamingServiceAssets_Service",
                table: "StreamingServiceAssets",
                column: "Service",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamAssets_TeamName_League",
                table: "TeamAssets",
                columns: new[] { "TeamName", "League" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordMarks_League_Variant",
                table: "WordMarks",
                columns: new[] { "League", "Variant" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeagueAssets");

            migrationBuilder.DropTable(
                name: "StreamingServiceAssets");

            migrationBuilder.DropTable(
                name: "TeamAssets");

            migrationBuilder.DropTable(
                name: "WordMarks");

            migrationBuilder.DropColumn(
                name: "PosterPath",
                table: "Titles");
        }
    }
}
