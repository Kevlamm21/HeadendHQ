using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class StreamingServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AndroidPackage",
                table: "Broadcasters");

            migrationBuilder.DropColumn(
                name: "IptvGuideNumber",
                table: "Broadcasters");

            migrationBuilder.DropColumn(
                name: "IsAffiliate",
                table: "Broadcasters");

            migrationBuilder.DropColumn(
                name: "MapsToBroadcasterId",
                table: "Broadcasters");

            // Subscriptions are now derived from streaming assignments, which start empty.
            migrationBuilder.Sql("UPDATE Broadcasters SET IsSubscribed = 0;");

            migrationBuilder.AddColumn<int>(
                name: "StreamingServiceId",
                table: "SportingEvents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StreamingServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: true),
                    GuideNumber = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LogoBroadcasterId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BroadcasterStreamingAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BroadcasterId = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamingServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    UseBroadcasterLogo = table.Column<bool>(type: "INTEGER", nullable: false),
                    LogoVariant = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcasterStreamingAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcasterStreamingAssignments_Broadcasters_BroadcasterId",
                        column: x => x.BroadcasterId,
                        principalTable: "Broadcasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BroadcasterStreamingAssignments_StreamingServices_StreamingServiceId",
                        column: x => x.StreamingServiceId,
                        principalTable: "StreamingServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StreamingServiceLogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StreamingServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Variant = table.Column<string>(type: "TEXT", nullable: false),
                    ImageId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingServiceLogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamingServiceLogos_StreamingServices_StreamingServiceId",
                        column: x => x.StreamingServiceId,
                        principalTable: "StreamingServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BroadcasterStreamingAssignments_BroadcasterId",
                table: "BroadcasterStreamingAssignments",
                column: "BroadcasterId");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcasterStreamingAssignments_StreamingServiceId",
                table: "BroadcasterStreamingAssignments",
                column: "StreamingServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingServiceLogos_StreamingServiceId_Variant",
                table: "StreamingServiceLogos",
                columns: new[] { "StreamingServiceId", "Variant" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StreamingServices_Key",
                table: "StreamingServices",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BroadcasterStreamingAssignments");

            migrationBuilder.DropTable(
                name: "StreamingServiceLogos");

            migrationBuilder.DropTable(
                name: "StreamingServices");

            migrationBuilder.DropColumn(
                name: "StreamingServiceId",
                table: "SportingEvents");

            migrationBuilder.AddColumn<string>(
                name: "AndroidPackage",
                table: "Broadcasters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IptvGuideNumber",
                table: "Broadcasters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAffiliate",
                table: "Broadcasters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MapsToBroadcasterId",
                table: "Broadcasters",
                type: "INTEGER",
                nullable: true);
        }
    }
}
