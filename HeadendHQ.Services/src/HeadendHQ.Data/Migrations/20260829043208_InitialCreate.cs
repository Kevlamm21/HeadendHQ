using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeadendHQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Athletes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true),
                    Position = table.Column<string>(type: "TEXT", nullable: true),
                    Jersey = table.Column<string>(type: "TEXT", nullable: true),
                    ExperienceYears = table.Column<int>(type: "INTEGER", nullable: true),
                    Headshot_SourceKey = table.Column<string>(type: "TEXT", nullable: true),
                    Headshot_SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Headshot_ETag = table.Column<string>(type: "TEXT", nullable: true),
                    Headshot_LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Headshot_SourceUpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Headshot_FetchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Headshot_ImageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Athletes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Broadcasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true),
                    CallLetters = table.Column<string>(type: "TEXT", nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    AndroidPackage = table.Column<string>(type: "TEXT", nullable: true),
                    IsSubscribed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAffiliate = table.Column<bool>(type: "INTEGER", nullable: false),
                    MapsToBroadcasterId = table.Column<int>(type: "INTEGER", nullable: true),
                    IptvGuideNumber = table.Column<string>(type: "TEXT", nullable: true),
                    DetailFetchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Aliases = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Broadcasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogSyncState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InitialDiscoveryStartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    InitialDiscoveryCompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastBroadcasterRefreshUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Stage = table.Column<string>(type: "TEXT", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedLeagueSlugs = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogSyncState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TitleRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    PublicBaseUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ActorThumbMode = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HdHomerunSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceUrl = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HdHomerunSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Bytes = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Origin = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IptvChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuideNumber = table.Column<string>(type: "TEXT", nullable: false),
                    CallSign = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    IconUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IptvChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IptvGuideCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Content = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IptvGuideCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SportId = table.Column<int>(type: "INTEGER", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Abbreviation = table.Column<string>(type: "TEXT", nullable: true),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true),
                    IsFollowed = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupportsTeams = table.Column<bool>(type: "INTEGER", nullable: false),
                    TeamsRefreshedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleScrapingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScrapeWindowDays = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxAthletesPerTeam = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 12)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleScrapingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourceSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestsPerMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    MinDelayMs = table.Column<int>(type: "INTEGER", nullable: false),
                    JitterMs = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxConcurrency = table.Column<int>(type: "INTEGER", nullable: false),
                    PerRunRequestBudget = table.Column<int>(type: "INTEGER", nullable: false),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: false),
                    RosterTtlDays = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscoverySportSlugs = table.Column<string>(type: "TEXT", nullable: false),
                    MaxTeamLogoLookupsPerRun = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SportingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeTeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    AwayTeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    BroadcasterId = table.Column<int>(type: "INTEGER", nullable: true),
                    HomeTeamName = table.Column<string>(type: "TEXT", nullable: false),
                    AwayTeamName = table.Column<string>(type: "TEXT", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Variant = table.Column<string>(type: "TEXT", nullable: false),
                    SeasonYear = table.Column<int>(type: "INTEGER", nullable: true),
                    SeasonType = table.Column<int>(type: "INTEGER", nullable: true),
                    VenueName = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    SeriesType = table.Column<string>(type: "TEXT", nullable: true),
                    SeriesSummary = table.Column<string>(type: "TEXT", nullable: true),
                    WatchUrl = table.Column<string>(type: "TEXT", nullable: true),
                    DetailsFetchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TitleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportingEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: true),
                    Abbreviation = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    ShortDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Nickname = table.Column<string>(type: "TEXT", nullable: true),
                    PrimaryColorHex = table.Column<string>(type: "TEXT", nullable: true),
                    AlternateColorHex = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFollowed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreferredLogoRel = table.Column<string>(type: "TEXT", nullable: false),
                    RosterRefreshedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LogosVerifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Titles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    LaunchSlug = table.Column<string>(type: "TEXT", nullable: true),
                    EventUrl = table.Column<string>(type: "TEXT", nullable: true),
                    AdbCommand = table.Column<string>(type: "TEXT", nullable: true),
                    VodLauncherPath = table.Column<string>(type: "TEXT", nullable: true),
                    ArtworkCreated = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsVideoCreated = table.Column<bool>(type: "INTEGER", nullable: false),
                    LiveJobId = table.Column<string>(type: "TEXT", nullable: true),
                    Artwork_PrimaryLogoImageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Artwork_SecondaryLogoImageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Artwork_PrimaryColorHex = table.Column<string>(type: "TEXT", nullable: true),
                    Artwork_SecondaryColorHex = table.Column<string>(type: "TEXT", nullable: true),
                    Artwork_BadgeImageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Artwork_ProviderLogoImageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Artwork_WordmarkImageId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLive = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Titles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VodLauncherSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LibraryPaths = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VodLauncherSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AthleteExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "BroadcasterExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    BroadcasterId = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "BroadcasterLogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BroadcasterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Variant = table.Column<string>(type: "TEXT", nullable: false),
                    Image_SourceKey = table.Column<string>(type: "TEXT", nullable: true),
                    Image_SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Image_ETag = table.Column<string>(type: "TEXT", nullable: true),
                    Image_LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_SourceUpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_FetchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_ImageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcasterLogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcasterLogos_Broadcasters_BroadcasterId",
                        column: x => x.BroadcasterId,
                        principalTable: "Broadcasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "LeagueLogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Variant = table.Column<string>(type: "TEXT", nullable: false),
                    Rel = table.Column<string>(type: "TEXT", nullable: false),
                    Image_SourceKey = table.Column<string>(type: "TEXT", nullable: true),
                    Image_SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Image_ETag = table.Column<string>(type: "TEXT", nullable: true),
                    Image_LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_SourceUpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_FetchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_ImageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueLogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueLogos_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueWordmarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Variant = table.Column<string>(type: "TEXT", nullable: false),
                    Image_SourceKey = table.Column<string>(type: "TEXT", nullable: true),
                    Image_SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Image_ETag = table.Column<string>(type: "TEXT", nullable: true),
                    Image_LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_SourceUpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_FetchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_ImageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueWordmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueWordmarks_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SportingEventCast",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SportingEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportingEventCast", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SportingEventCast_SportingEvents_SportingEventId",
                        column: x => x.SportingEventId,
                        principalTable: "SportingEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SportExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    SportId = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "TeamExternalRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "TeamLogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rel = table.Column<string>(type: "TEXT", nullable: false),
                    Image_SourceKey = table.Column<string>(type: "TEXT", nullable: true),
                    Image_SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Image_ETag = table.Column<string>(type: "TEXT", nullable: true),
                    Image_LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_SourceUpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_FetchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Image_ImageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamLogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamLogos_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TitleCast",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TitleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: true),
                    HeadshotImageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_BroadcasterExternalRefs_BroadcasterId_SourceKey",
                table: "BroadcasterExternalRefs",
                columns: new[] { "BroadcasterId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BroadcasterExternalRefs_SourceKey_ExternalId",
                table: "BroadcasterExternalRefs",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_BroadcasterLogos_BroadcasterId_Variant",
                table: "BroadcasterLogos",
                columns: new[] { "BroadcasterId", "Variant" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Broadcasters_Slug",
                table: "Broadcasters",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_Sha256",
                table: "Images",
                column: "Sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IptvChannels_CallSign",
                table: "IptvChannels",
                column: "CallSign");

            migrationBuilder.CreateIndex(
                name: "IX_IptvChannels_GuideNumber",
                table: "IptvChannels",
                column: "GuideNumber",
                unique: true);

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
                name: "IX_LeagueLogos_LeagueId_Variant_Rel",
                table: "LeagueLogos",
                columns: new[] { "LeagueId", "Variant", "Rel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_Slug",
                table: "Leagues",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_SportId",
                table: "Leagues",
                column: "SportId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueWordmarks_LeagueId_Variant",
                table: "LeagueWordmarks",
                columns: new[] { "LeagueId", "Variant" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SportExternalRefs_SourceKey_ExternalId",
                table: "SportExternalRefs",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_SportExternalRefs_SportId_SourceKey",
                table: "SportExternalRefs",
                columns: new[] { "SportId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SportingEventCast_SportingEventId_AthleteId",
                table: "SportingEventCast",
                columns: new[] { "SportingEventId", "AthleteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SportingEvents_SourceKey_ExternalId",
                table: "SportingEvents",
                columns: new[] { "SourceKey", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SportingEvents_StartUtc",
                table: "SportingEvents",
                column: "StartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SportingEvents_TitleId",
                table: "SportingEvents",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sports_Slug",
                table: "Sports",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamExternalRefs_SourceKey_ExternalId",
                table: "TeamExternalRefs",
                columns: new[] { "SourceKey", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamExternalRefs_TeamId_SourceKey",
                table: "TeamExternalRefs",
                columns: new[] { "TeamId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamLogos_TeamId_Rel",
                table: "TeamLogos",
                columns: new[] { "TeamId", "Rel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LeagueId_DisplayName",
                table: "Teams",
                columns: new[] { "LeagueId", "DisplayName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TitleCast_TitleId_Order",
                table: "TitleCast",
                columns: new[] { "TitleId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Titles_StartUtc",
                table: "Titles",
                column: "StartUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AthleteExternalRefs");

            migrationBuilder.DropTable(
                name: "BroadcasterExternalRefs");

            migrationBuilder.DropTable(
                name: "BroadcasterLogos");

            migrationBuilder.DropTable(
                name: "CatalogSyncState");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "HdHomerunSettings");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "IptvChannels");

            migrationBuilder.DropTable(
                name: "IptvGuideCache");

            migrationBuilder.DropTable(
                name: "LeagueExternalRefs");

            migrationBuilder.DropTable(
                name: "LeagueLogos");

            migrationBuilder.DropTable(
                name: "LeagueWordmarks");

            migrationBuilder.DropTable(
                name: "ScheduleScrapingSettings");

            migrationBuilder.DropTable(
                name: "SourceSettings");

            migrationBuilder.DropTable(
                name: "SportExternalRefs");

            migrationBuilder.DropTable(
                name: "SportingEventCast");

            migrationBuilder.DropTable(
                name: "TeamExternalRefs");

            migrationBuilder.DropTable(
                name: "TeamLogos");

            migrationBuilder.DropTable(
                name: "TitleCast");

            migrationBuilder.DropTable(
                name: "VodLauncherSettings");

            migrationBuilder.DropTable(
                name: "Athletes");

            migrationBuilder.DropTable(
                name: "Broadcasters");

            migrationBuilder.DropTable(
                name: "Leagues");

            migrationBuilder.DropTable(
                name: "Sports");

            migrationBuilder.DropTable(
                name: "SportingEvents");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Titles");
        }
    }
}
