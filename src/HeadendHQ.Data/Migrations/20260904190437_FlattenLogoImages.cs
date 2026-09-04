using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <summary>
    /// Collapses the owned <c>ImageRef</c> on every logo table into a plain image id, and moves the
    /// address and validators onto <c>Images</c>, which already held the address for headshots.
    /// <para>
    /// The tables are rebuilt rather than altered, because the columns do not line up: a team's
    /// <c>Rel</c> becomes its <c>Label</c> (not its <c>Variant</c>), a broadcaster's misnamed
    /// <c>Variant</c> was always a rel and becomes <c>Label</c> too, and <c>Origin</c> has to be read
    /// off the image each row points at rather than defaulted.
    /// </para>
    /// <para>
    /// Rows with no <c>Image_ImageId</c> are dropped. They were addresses nobody had ever downloaded
    /// — the bulk of the league catalogue — and a logo row now means bytes on disk. Follows and
    /// nightly refreshes put back the handful that are actually wanted.
    /// </para>
    /// </summary>
    public partial class FlattenLogoImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- Images takes over the validators ----------------------------------------------
            migrationBuilder.AddColumn<string>(
                name: "ETag", table: "Images", type: "TEXT", nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedUtc", table: "Images", type: "TEXT", nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FetchedAtUtc", table: "Images", type: "TEXT", nullable: true);

            // Purpose joins the key: it decides normalization, so the same address fetched as a
            // headshot and as a team logo is two different sets of pixels and two different rows.
            migrationBuilder.DropIndex(name: "IX_Images_SourceUrl", table: "Images");

            migrationBuilder.CreateIndex(
                name: "IX_Images_SourceUrl_Purpose",
                table: "Images",
                columns: new[] { "SourceUrl", "Purpose" },
                unique: true,
                filter: "\"SourceUrl\" IS NOT NULL");

            // Best effort, and only where it is unambiguous: a URL held by exactly one logo row can
            // hand its validators over, so that image is revalidated rather than re-downloaded on the
            // next refresh. Everything else simply costs one unconditional fetch, once.
            foreach (var table in new[] { "TeamLogos", "LeagueLogos", "BroadcasterLogos" })
                migrationBuilder.Sql($"""
                    UPDATE Images
                    SET ETag = (SELECT l.Image_ETag FROM {table} l
                                WHERE l.Image_SourceUrl = Images.SourceUrl AND l.Image_ETag IS NOT NULL),
                        LastModifiedUtc = (SELECT l.Image_LastModifiedUtc FROM {table} l
                                WHERE l.Image_SourceUrl = Images.SourceUrl AND l.Image_ETag IS NOT NULL)
                    WHERE Images.ETag IS NULL
                      AND Images.SourceUrl IS NOT NULL
                      AND (SELECT COUNT(*) FROM {table} l
                           WHERE l.Image_SourceUrl = Images.SourceUrl AND l.Image_ETag IS NOT NULL) = 1;
                    """);

            // ---- The four logo tables ----------------------------------------------------------
            // Origin is read off the image rather than guessed, so a hand upload stays Manual and is
            // never overwritten by a later refresh.
            Rebuild(migrationBuilder,
                table: "TeamLogos", owner: "Teams", ownerKey: "TeamId",
                selectVariant: "'Default'", selectLabel: "old.Rel");

            Rebuild(migrationBuilder,
                table: "LeagueLogos", owner: "Leagues", ownerKey: "LeagueId",
                selectVariant: "old.Variant", selectLabel: "old.Rel");

            // The old column named Variant only ever held 'Default' or 'dark' — rel tokens filed
            // under the wrong axis. They move to Label, normalized to the LogoRels spelling.
            Rebuild(migrationBuilder,
                table: "BroadcasterLogos", owner: "Broadcasters", ownerKey: "BroadcasterId",
                selectVariant: "'Default'",
                selectLabel: "CASE WHEN old.Variant = 'dark' THEN 'dark' ELSE 'default' END");

            // Upload-only, so no label and no origin to record.
            migrationBuilder.Sql("""
                CREATE TABLE "LeagueWordmarks_new" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_LeagueWordmarks" PRIMARY KEY AUTOINCREMENT,
                    "LeagueId" INTEGER NOT NULL,
                    "Variant" TEXT NOT NULL,
                    "ImageId" INTEGER NOT NULL,
                    CONSTRAINT "FK_LeagueWordmarks_Leagues_LeagueId" FOREIGN KEY ("LeagueId")
                        REFERENCES "Leagues" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "LeagueWordmarks_new" ("Id", "LeagueId", "Variant", "ImageId")
                SELECT old."Id", old."LeagueId", old."Variant", old."Image_ImageId"
                FROM "LeagueWordmarks" old
                WHERE old."Image_ImageId" IS NOT NULL
                  AND EXISTS (SELECT 1 FROM "Images" i WHERE i."Id" = old."Image_ImageId");
                """);

            migrationBuilder.Sql("""DROP TABLE "LeagueWordmarks";""");
            migrationBuilder.Sql("""ALTER TABLE "LeagueWordmarks_new" RENAME TO "LeagueWordmarks";""");
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_LeagueWordmarks_LeagueId_Variant"
                ON "LeagueWordmarks" ("LeagueId", "Variant");
                """);
        }

        /// <summary>
        /// Rebuilds one logo table into the flattened shape. Deduplicated because the three differ
        /// only in which owner they hang off and where their variant and label come from.
        /// </summary>
        private static void Rebuild(
            MigrationBuilder migrationBuilder,
            string table, string owner, string ownerKey, string selectVariant, string selectLabel)
        {
            migrationBuilder.Sql($"""
                CREATE TABLE "{table}_new" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_{table}" PRIMARY KEY AUTOINCREMENT,
                    "{ownerKey}" INTEGER NOT NULL,
                    "Variant" TEXT NOT NULL,
                    "Label" TEXT NULL,
                    "Origin" INTEGER NOT NULL,
                    "ImageId" INTEGER NOT NULL,
                    CONSTRAINT "FK_{table}_{owner}_{ownerKey}" FOREIGN KEY ("{ownerKey}")
                        REFERENCES "{owner}" ("Id") ON DELETE CASCADE
                );
                """);

            // GROUP BY collapses the rows that the old, looser key allowed to collide under the new
            // one — a team holding both 'dark' and 'dark|scoreboard' of the same image, say.
            migrationBuilder.Sql($"""
                INSERT INTO "{table}_new" ("Id", "{ownerKey}", "Variant", "Label", "Origin", "ImageId")
                SELECT MIN(old."Id"), old."{ownerKey}", {selectVariant}, {selectLabel},
                       COALESCE((SELECT i."Origin" FROM "Images" i WHERE i."Id" = old."Image_ImageId"), 0),
                       old."Image_ImageId"
                FROM "{table}" old
                WHERE old."Image_ImageId" IS NOT NULL
                  AND EXISTS (SELECT 1 FROM "Images" i WHERE i."Id" = old."Image_ImageId")
                GROUP BY old."{ownerKey}", {selectVariant}, {selectLabel};
                """);

            migrationBuilder.Sql($"""DROP TABLE "{table}";""");
            migrationBuilder.Sql($"""ALTER TABLE "{table}_new" RENAME TO "{table}";""");
            migrationBuilder.Sql($"""
                CREATE UNIQUE INDEX "IX_{table}_{ownerKey}_Variant_Label"
                ON "{table}" ("{ownerKey}", "Variant", "Label");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The lazy shape comes back empty of upstream state: the addresses lived on these rows
            // and are gone. Every logo re-resolves from the source on the next sync.
            Unbuild(migrationBuilder, "TeamLogos", "Teams", "TeamId", hasRel: true, hasVariant: false);
            Unbuild(migrationBuilder, "LeagueLogos", "Leagues", "LeagueId", hasRel: true, hasVariant: true);
            Unbuild(migrationBuilder, "BroadcasterLogos", "Broadcasters", "BroadcasterId", hasRel: false, hasVariant: true);
            Unbuild(migrationBuilder, "LeagueWordmarks", "Leagues", "LeagueId", hasRel: false, hasVariant: true);

            migrationBuilder.DropIndex(name: "IX_Images_SourceUrl_Purpose", table: "Images");

            migrationBuilder.CreateIndex(
                name: "IX_Images_SourceUrl",
                table: "Images",
                column: "SourceUrl",
                unique: true,
                filter: "\"SourceUrl\" IS NOT NULL");

            migrationBuilder.DropColumn(name: "FetchedAtUtc", table: "Images");
            migrationBuilder.DropColumn(name: "LastModifiedUtc", table: "Images");
            migrationBuilder.DropColumn(name: "ETag", table: "Images");
        }

        private static void Unbuild(
            MigrationBuilder migrationBuilder,
            string table, string owner, string ownerKey, bool hasRel, bool hasVariant)
        {
            var variantColumn = hasVariant ? "\"Variant\" TEXT NOT NULL," : string.Empty;
            var relColumn = hasRel ? "\"Rel\" TEXT NOT NULL," : string.Empty;
            var variantInsert = hasVariant ? "\"Variant\"," : string.Empty;
            var relInsert = hasRel ? "\"Rel\"," : string.Empty;
            var variantSelect = hasVariant ? "old.\"Variant\"," : string.Empty;
            var relSelect = hasRel ? "COALESCE(old.\"Label\", 'default')," : string.Empty;

            migrationBuilder.Sql($"""
                CREATE TABLE "{table}_old" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_{table}" PRIMARY KEY AUTOINCREMENT,
                    "{ownerKey}" INTEGER NOT NULL,
                    {variantColumn}
                    {relColumn}
                    "Image_SourceKey" TEXT NULL,
                    "Image_SourceUrl" TEXT NULL,
                    "Image_ETag" TEXT NULL,
                    "Image_LastModifiedUtc" TEXT NULL,
                    "Image_SourceUpdatedAtUtc" TEXT NULL,
                    "Image_FetchedAtUtc" TEXT NULL,
                    "Image_ImageId" INTEGER NULL,
                    CONSTRAINT "FK_{table}_{owner}_{ownerKey}" FOREIGN KEY ("{ownerKey}")
                        REFERENCES "{owner}" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "{table}_old" ("Id", "{ownerKey}", {variantInsert} {relInsert} "Image_ImageId")
                SELECT old."Id", old."{ownerKey}", {variantSelect} {relSelect} old."ImageId"
                FROM "{table}" old;
                """);

            migrationBuilder.Sql($"""DROP TABLE "{table}";""");
            migrationBuilder.Sql($"""ALTER TABLE "{table}_old" RENAME TO "{table}";""");

            var key = (hasVariant, hasRel) switch
            {
                (true, true) => "\"Variant\", \"Rel\"",
                (true, false) => "\"Variant\"",
                _ => "\"Rel\"",
            };

            var name = (hasVariant, hasRel) switch
            {
                (true, true) => $"IX_{table}_{ownerKey}_Variant_Rel",
                (true, false) => $"IX_{table}_{ownerKey}_Variant",
                _ => $"IX_{table}_{ownerKey}_Rel",
            };

            migrationBuilder.Sql($"""
                CREATE UNIQUE INDEX "{name}" ON "{table}" ("{ownerKey}", {key});
                """);
        }
    }
}
