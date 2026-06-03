using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorXmltvCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceAuth",
                table: "HdHomerunDevices");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "HdHomerunDevices");

            migrationBuilder.DropColumn(
                name: "XmltvUrl",
                table: "HdHomerunDevices");

            migrationBuilder.RenameTable(
                name: "HdHomerunDevices",
                newName: "XmltvCache");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "XmltvCache",
                newName: "HdHomerunDevices");

            migrationBuilder.AddColumn<string>(
                name: "DeviceAuth",
                table: "HdHomerunDevices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "HdHomerunDevices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XmltvUrl",
                table: "HdHomerunDevices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
