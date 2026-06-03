using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTitleMetadataToJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata_AwayTeam",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_ContentRating",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_Episode",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_Genre",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_HomeTeam",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_League",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_Plot",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_Rating",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_Season",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_ShowTitle",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_Studio",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_Tagline",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_UniqueId",
                table: "Titles");

            migrationBuilder.DropColumn(
                name: "Metadata_Year",
                table: "Titles");

            migrationBuilder.RenameColumn(
                name: "Metadata_Venue",
                table: "Titles",
                newName: "Metadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Titles",
                newName: "Metadata_Venue");

            migrationBuilder.AddColumn<string>(
                name: "Metadata_AwayTeam",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata_ContentRating",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Metadata_Episode",
                table: "Titles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata_Genre",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata_HomeTeam",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata_League",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata_Plot",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Metadata_Rating",
                table: "Titles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Metadata_Season",
                table: "Titles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata_ShowTitle",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata_Studio",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata_Tagline",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata_UniqueId",
                table: "Titles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Metadata_Year",
                table: "Titles",
                type: "INTEGER",
                nullable: true);
        }
    }
}
