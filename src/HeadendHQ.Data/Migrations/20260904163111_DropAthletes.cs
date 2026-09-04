using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropAthletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM SportingEventCast;");
            migrationBuilder.Sql(
                "UPDATE SportingEvents SET DetailsFetchedAtUtc = NULL WHERE StartUtc > CURRENT_TIMESTAMP;");

            migrationBuilder.Sql(
                """
                DELETE FROM Images
                WHERE Id IN (SELECT Headshot_ImageId FROM Athletes WHERE Headshot_ImageId IS NOT NULL);
                """);

            migrationBuilder.DropTable(
                name: "AthleteExternalRefs");

            migrationBuilder.DropTable(
                name: "Athletes");

            migrationBuilder.DropIndex(
                name: "IX_SportingEventCast_SportingEventId_AthleteId",
                table: "SportingEventCast");

            migrationBuilder.DropColumn(
                name: "RosterRefreshedAtUtc",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "AthleteId",
                table: "SportingEventCast");

            migrationBuilder.DropColumn(
                name: "RosterTtlDays",
                table: "SourceSettings");

            migrationBuilder.AddColumn<int>(
                name: "HeadshotImageId",
                table: "SportingEventCast",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SportingEventCast",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "SportingEventCast",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "Images",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "Images",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Images",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SportingEventCast_SportingEventId_Order",
                table: "SportingEventCast",
                columns: new[] { "SportingEventId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Images_Purpose_LeagueId",
                table: "Images",
                columns: new[] { "Purpose", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_Images_SourceUrl",
                table: "Images",
                column: "SourceUrl",
                unique: true,
                filter: "\"SourceUrl\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SportingEventCast_SportingEventId_Order",
                table: "SportingEventCast");

            migrationBuilder.DropIndex(
                name: "IX_Images_Purpose_LeagueId",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_SourceUrl",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "HeadshotImageId",
                table: "SportingEventCast");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SportingEventCast");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "SportingEventCast");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Images");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RosterRefreshedAtUtc",
                table: "Teams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AthleteId",
                table: "SportingEventCast",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RosterTtlDays",
                table: "SourceSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Athletes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    ExperienceYears = table.Column<int>(type: "INTEGER", nullable: true),
                    Jersey = table.Column<string>(type: "TEXT", nullable: true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<string>(type: "TEXT", nullable: true),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    Headshot_ETag = table.Column<string>(type: "TEXT", nullable: true),
                    Headshot_FetchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Headshot_ImageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Headshot_LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Headshot_SourceKey = table.Column<string>(type: "TEXT", nullable: true),
                    Headshot_SourceUpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Headshot_SourceUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Athletes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AthleteExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteExternalRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AthleteExternalRefs_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SportingEventCast_SportingEventId_AthleteId",
                table: "SportingEventCast",
                columns: new[] { "SportingEventId", "AthleteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AthleteExternalRefs_AthleteId_SourceKey",
                table: "AthleteExternalRefs",
                columns: new[] { "AthleteId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AthleteExternalRefs_SourceKey_ExternalId",
                table: "AthleteExternalRefs",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_TeamId",
                table: "Athletes",
                column: "TeamId");
        }
    }
}
