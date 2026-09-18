using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class TitleSourceAndFlatMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A title now links to its source entity by SourceId and carries the NFO fields flat:
            // scalars as columns, the two string lists and the cast as JSON columns, plus the four
            // artwork image ids. The old Metadata JSON blob, the composer-input logo/colour columns
            // and the separate TitleCast table all go. Existing produced titles have no SourceId and
            // won't re-compose (their event already has a TitleId) — run DELETE /titles once for a
            // clean rebuild, or let them age out on retention.
            migrationBuilder.DropTable(name: "TitleCast");

            migrationBuilder.DropColumn(name: "Metadata", table: "Titles");
            migrationBuilder.DropColumn(name: "Artwork_PrimaryLogoImageId", table: "Titles");
            migrationBuilder.DropColumn(name: "Artwork_SecondaryLogoImageId", table: "Titles");
            migrationBuilder.DropColumn(name: "Artwork_PrimaryColorHex", table: "Titles");
            migrationBuilder.DropColumn(name: "Artwork_SecondaryColorHex", table: "Titles");
            migrationBuilder.DropColumn(name: "Artwork_BadgeImageId", table: "Titles");
            migrationBuilder.DropColumn(name: "Artwork_ProviderLogoImageId", table: "Titles");
            migrationBuilder.DropColumn(name: "Artwork_WordmarkImageId", table: "Titles");

            migrationBuilder.AddColumn<Guid>(name: "SourceId", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Plot", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Tagline", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Studio", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ContentRating", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "UniqueId", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Genres", table: "Titles", type: "TEXT", nullable: false, defaultValue: "[]");
            migrationBuilder.AddColumn<string>(name: "Sets", table: "Titles", type: "TEXT", nullable: false, defaultValue: "[]");
            migrationBuilder.AddColumn<string>(name: "Cast", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<int>(name: "PosterImageId", table: "Titles", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "BackgroundImageId", table: "Titles", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "ThumbnailImageId", table: "Titles", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "ClearLogoImageId", table: "Titles", type: "INTEGER", nullable: true);

            migrationBuilder.CreateIndex(name: "IX_Titles_SourceId", table: "Titles", column: "SourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Titles_SourceId", table: "Titles");

            migrationBuilder.DropColumn(name: "SourceId", table: "Titles");
            migrationBuilder.DropColumn(name: "Plot", table: "Titles");
            migrationBuilder.DropColumn(name: "Tagline", table: "Titles");
            migrationBuilder.DropColumn(name: "Studio", table: "Titles");
            migrationBuilder.DropColumn(name: "ContentRating", table: "Titles");
            migrationBuilder.DropColumn(name: "UniqueId", table: "Titles");
            migrationBuilder.DropColumn(name: "Genres", table: "Titles");
            migrationBuilder.DropColumn(name: "Sets", table: "Titles");
            migrationBuilder.DropColumn(name: "Cast", table: "Titles");
            migrationBuilder.DropColumn(name: "PosterImageId", table: "Titles");
            migrationBuilder.DropColumn(name: "BackgroundImageId", table: "Titles");
            migrationBuilder.DropColumn(name: "ThumbnailImageId", table: "Titles");
            migrationBuilder.DropColumn(name: "ClearLogoImageId", table: "Titles");

            migrationBuilder.AddColumn<string>(name: "Metadata", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Artwork_PrimaryLogoImageId", table: "Titles", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Artwork_SecondaryLogoImageId", table: "Titles", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Artwork_PrimaryColorHex", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Artwork_SecondaryColorHex", table: "Titles", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Artwork_BadgeImageId", table: "Titles", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Artwork_ProviderLogoImageId", table: "Titles", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Artwork_WordmarkImageId", table: "Titles", type: "INTEGER", nullable: true);

            migrationBuilder.CreateTable(
                name: "TitleCast",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HeadshotImageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: true),
                    TitleId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleCast", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TitleCast_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TitleCast_TitleId_Order",
                table: "TitleCast",
                columns: new[] { "TitleId", "Order" });
        }
    }
}
