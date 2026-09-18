using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class SelectableLogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "TeamLogos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "LeagueLogos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "BroadcasterLogos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            foreach (var (table, owner) in new[]
                     {
                         ("TeamLogos", "TeamId"),
                         ("LeagueLogos", "LeagueId"),
                         ("BroadcasterLogos", "BroadcasterId"),
                     })
            {
                migrationBuilder.Sql($"""
                    UPDATE "{table}" SET "IsSelected" = 1
                    WHERE "Id" IN (
                        SELECT MAX("Id") FROM "{table}" WHERE "Origin" = 1 GROUP BY "{owner}", "Variant");
                    """);

                migrationBuilder.Sql($"""
                    UPDATE "{table}" SET "Label" = NULL WHERE "Origin" = 1;
                    """);
            }

            migrationBuilder.Sql("""
                UPDATE "TeamLogos" SET "IsSelected" = 1
                WHERE "Origin" = 0
                  AND "Label" = (
                      SELECT t."PreferredLogoRel" FROM "Teams" t
                      WHERE t."Id" = "TeamLogos"."TeamId"
                        AND t."PreferredLogoRel" <> 'primary_logo_on_secondary_color')
                  AND NOT EXISTS (
                      SELECT 1 FROM "TeamLogos" m
                      WHERE m."TeamId" = "TeamLogos"."TeamId" AND m."Variant" = "TeamLogos"."Variant"
                        AND m."IsSelected" = 1);
                """);

            migrationBuilder.DropColumn(
                name: "PreferredLogoRel",
                table: "Teams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "TeamLogos");

            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "LeagueLogos");

            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "BroadcasterLogos");

            migrationBuilder.AddColumn<string>(
                name: "PreferredLogoRel",
                table: "Teams",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
