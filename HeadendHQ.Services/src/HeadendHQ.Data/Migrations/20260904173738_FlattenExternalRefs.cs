using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class FlattenExternalRefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "Sports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Sports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "Leagues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Leagues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "Teams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Teams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "Broadcasters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Broadcasters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Sports SET
                  SourceKey  = (SELECT r.SourceKey  FROM SportExternalRefs r
                                WHERE r.SportId = Sports.Id ORDER BY r.Id LIMIT 1),
                  ExternalId = (SELECT r.ExternalId FROM SportExternalRefs r
                                WHERE r.SportId = Sports.Id ORDER BY r.Id LIMIT 1);
                """);

            migrationBuilder.Sql("""
                UPDATE Leagues SET
                  SourceKey  = (SELECT r.SourceKey  FROM LeagueExternalRefs r
                                WHERE r.LeagueId = Leagues.Id ORDER BY r.Id LIMIT 1),
                  ExternalId = (SELECT r.ExternalId FROM LeagueExternalRefs r
                                WHERE r.LeagueId = Leagues.Id ORDER BY r.Id LIMIT 1);
                """);

            migrationBuilder.Sql("""
                UPDATE Teams SET
                  SourceKey  = (SELECT r.SourceKey  FROM TeamExternalRefs r
                                WHERE r.TeamId = Teams.Id ORDER BY r.Id LIMIT 1),
                  ExternalId = (SELECT r.ExternalId FROM TeamExternalRefs r
                                WHERE r.TeamId = Teams.Id ORDER BY r.Id LIMIT 1);
                """);

            migrationBuilder.Sql("""
                UPDATE Broadcasters SET
                  SourceKey  = (SELECT r.SourceKey  FROM BroadcasterExternalRefs r
                                WHERE r.BroadcasterId = Broadcasters.Id ORDER BY r.Id LIMIT 1),
                  ExternalId = (SELECT r.ExternalId FROM BroadcasterExternalRefs r
                                WHERE r.BroadcasterId = Broadcasters.Id ORDER BY r.Id LIMIT 1);
                """);

            migrationBuilder.DropTable(
                name: "SportExternalRefs");

            migrationBuilder.DropTable(
                name: "LeagueExternalRefs");

            migrationBuilder.DropTable(
                name: "TeamExternalRefs");

            migrationBuilder.DropTable(
                name: "BroadcasterExternalRefs");

            migrationBuilder.CreateIndex(
                name: "IX_Sports_SourceKey_ExternalId",
                table: "Sports",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_SourceKey_ExternalId",
                table: "Leagues",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SourceKey_ExternalId",
                table: "Teams",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Broadcasters_SourceKey_ExternalId",
                table: "Broadcasters",
                columns: new[] { "SourceKey", "ExternalId" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sports_SourceKey_ExternalId",
                table: "Sports");

            migrationBuilder.DropIndex(
                name: "IX_Leagues_SourceKey_ExternalId",
                table: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_Teams_SourceKey_ExternalId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Broadcasters_SourceKey_ExternalId",
                table: "Broadcasters");

            migrationBuilder.CreateTable(
                name: "SportExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SportId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportExternalRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SportExternalRefs_Sports_SportId",
                        column: x => x.SportId,
                        principalTable: "Sports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueExternalRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueExternalRefs_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamExternalRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamExternalRefs_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BroadcasterExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BroadcasterId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcasterExternalRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcasterExternalRefs_Broadcasters_BroadcasterId",
                        column: x => x.BroadcasterId,
                        principalTable: "Broadcasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Move the pairs back before the columns are dropped. An entity with no id contributes
            // no row, which is how it looked before this migration ran.
            migrationBuilder.Sql("""
                INSERT INTO SportExternalRefs (SportId, SourceKey, ExternalId)
                SELECT Id, SourceKey, ExternalId FROM Sports
                WHERE SourceKey IS NOT NULL AND ExternalId IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO LeagueExternalRefs (LeagueId, SourceKey, ExternalId)
                SELECT Id, SourceKey, ExternalId FROM Leagues
                WHERE SourceKey IS NOT NULL AND ExternalId IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO TeamExternalRefs (TeamId, SourceKey, ExternalId)
                SELECT Id, SourceKey, ExternalId FROM Teams
                WHERE SourceKey IS NOT NULL AND ExternalId IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO BroadcasterExternalRefs (BroadcasterId, SourceKey, ExternalId)
                SELECT Id, SourceKey, ExternalId FROM Broadcasters
                WHERE SourceKey IS NOT NULL AND ExternalId IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "Broadcasters");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Broadcasters");

            migrationBuilder.CreateIndex(
                name: "IX_SportExternalRefs_SportId_SourceKey",
                table: "SportExternalRefs",
                columns: new[] { "SportId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SportExternalRefs_SourceKey_ExternalId",
                table: "SportExternalRefs",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueExternalRefs_LeagueId_SourceKey",
                table: "LeagueExternalRefs",
                columns: new[] { "LeagueId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeagueExternalRefs_SourceKey_ExternalId",
                table: "LeagueExternalRefs",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamExternalRefs_TeamId_SourceKey",
                table: "TeamExternalRefs",
                columns: new[] { "TeamId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamExternalRefs_SourceKey_ExternalId",
                table: "TeamExternalRefs",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_BroadcasterExternalRefs_BroadcasterId_SourceKey",
                table: "BroadcasterExternalRefs",
                columns: new[] { "BroadcasterId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BroadcasterExternalRefs_SourceKey_ExternalId",
                table: "BroadcasterExternalRefs",
                columns: new[] { "SourceKey", "ExternalId" });

        }
    }
}
