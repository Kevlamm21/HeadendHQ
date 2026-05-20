# HeadendHQ — Claude Agent Memory

## Project Overview
HeadendHQ is a self-hosted ASP.NET Core Web API (.NET 10) running as a single Docker container on an Unraid server. It serves as the central backend for managing a smart TV headend — orchestrating EPG data, sports schedule scraping, ErsatzTV channel mapping, and ADB stream control.

## Repo Structure
HeadendHQ/                  ← repo root
├── HeadendHQ/              ← .NET project
├── HeadendHQ.sln
├── Dockerfile
├── .github/workflows/
├── CLAUDE.md
├── README.md
└── LICENSE

## Tech Stack
- ASP.NET Core Web API, .NET 10, Minimal APIs
- SQLite via Entity Framework Core (no separate DB container)
- Docker, hosted on Unraid
- GitHub Actions CI/CD → GHCR (ghcr.io/Kevlamm21/headendhq)
- Public GitHub repo: github.com/Kevlamm21/HeadendHQ

## Architecture Principles
- Single Docker container, always
- Minimal APIs only — no controllers
- Follow Domain Driven Design Best Practices
- Third-party dependencies (SchedulesDirect, NBA.com, etc.) must be isolated behind their own service/interface so they can be swapped or disabled independently
- SQLite for all persistence — data is expected to be light (EPG xml, sporting events, ErsatzTV data)
- Cron jobs implemented as IHostedService background services

## Roadmap (Steps)
1. ✅ App scaffold, Dockerfile, CI/CD pipeline, health endpoint
2. ✅ HdHomerun integration — EPG XML fetch, store at static URL, nightly cron job to update
3. Schedule scraper — sporting events from NBA.com/NFL.com/ESPN, ADB command endpoint, dummy VOD videos, cleanup service
4. ErsatzTV channel mapping — API integration, smart game-to-channel assignment, nightly cron
5. Custom ErsatzTV scripted schedules — schedule games first, backfill, dead air 2am–7am, dummy videos until encoder arrives
6. Home Assistant automations — consume ErsatzTV XMLTV, watch Tivimate playback, trigger ADB commands
7. Smarter HA automations — channel warming, OT logic, etc.

## Key Integrations
- **HDHomeRun** — feeds directly into Tivimate; EPG XML comes from HeadendHQ
- **ErsatzTV** — channel scheduling; HeadendHQ assigns games to channels and generates scripted schedules
- **Tivimate** — on Nvidia Shield; watches ErsatzTV channels
- **Home Assistant** — automation brain; watches Tivimate playback + ErsatzTV XMLTV
- **ADB** — used to control encoder stream devices
- **Plex** — dummy VOD sporting events hosted here until encoder is available

## Coding Preferences
- Minimal APIs, not controllers
- Keep third-party integrations behind interfaces
- Prefer explicit, readable code over clever abstractions

## Schedule Scraper (Step 3)

### Overview
Scrape sporting event schedules from external sources, store them as `SportingEvent`
entities in SQLite, and map each event to an ADB launch command. Scraping and ADB
mapping are fully decoupled — the scraper stores events, the mapping service assigns
commands independently.

### Reference Project
A prior project called **UnitedSports** exists at `C:\Users\KLammers\Personal\United-Sports`
with NBA.com scraping and ADB extractor logic already built. Port relevant logic but
adapt it to HeadendHQ's DDD structure and Minimal API style. Do not re-analyze that
project unless explicitly told to — key details are captured in this file and in
the internal memory system.

Key UnitedSports details:
- NBA.com API: `https://cdn.nba.com/static/json/staticData/scheduleLeagueV2_1.json` (free, no auth)
- Broadcaster → StreamingService map: `"ESPN"→Espn`, `"NBA TV"→NbaLeaguePass`, `"Prime Video"→AmazonPrime`, `"Peacock"→Peacock`
- Peacock URLs require Playwright headless browser resolution (Microsoft.Playwright)
- ADB commands are built per StreamingService by extracting an ID/UUID from the EventUrl
- Do NOT copy: Mediator/CQRS, Ardalis.Specification, EF migrations, VideoCreated/Poster/Thumbnail/Tags fields

### Project Structure
Each third-party schedule source gets its own project, mirroring the `HeadendHQ.HdHomerun` pattern:

```
HeadendHQ.Core         — SportingEvent entity, Sport enum, StreamingService enum,
                         IScheduleSource interface, IAdbExtractor interface
HeadendHQ.Data         — AppDbContext: add SportingEvents DbSet
HeadendHQ.Nba          — NbaScheduleSource, NBA JSON models, Playwright Peacock resolver
HeadendHQ.AdbMapping   — IAdbExtractor implementations (ESPN, NBA, Prime, Peacock),
                         AdbMappingService
HeadendHQ.Web          — ScheduleScraperJob (IHostedService cron),
                         SportingEventEndpoints, AdbMappingEndpoints
```

Adding a future source (NFL, MLB, etc.) = new project + DI registration, nothing else changes.

### SportingEvent Entity
Intentionally slimmed — metadata fields (Poster, Thumbnail, Tags, SubTitle, Description) deferred to later:

```csharp
public class SportingEvent
{
    public int Id { get; set; }
    public string Title { get; set; }           // "Celtics vs. Lakers"
    public Sport Sport { get; set; }
    public StreamingService StreamingService { get; set; }
    public string EventUrl { get; set; }        // Source URL from scraper
    public string? AdbCommand { get; set; }     // Null until mapping service runs
    public string Provider { get; set; }        // e.g. "NBA.com"
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }        // Defaults to StartUtc + 3h if unknown
}
```

When `EventUrl` changes, `AdbCommand` must be reset to `null` so the mapping service
re-derives it on the next run.

### IScheduleSource Interface
```csharp
public interface IScheduleSource
{
    Sport Sport { get; }
    Task<IReadOnlyList<SportingEvent>> FetchEventsAsync(CancellationToken ct);
}
```

### IAdbExtractor Interface
```csharp
public interface IAdbExtractor
{
    StreamingService Service { get; }
    string BuildCommand(string eventUrl);
}
```

### ADB Mapping Service
- Lives in `HeadendHQ.AdbMapping`
- Injected with all `IAdbExtractor` registrations
- Queries for `SportingEvent` records where `AdbCommand` is null
- Matches event's `StreamingService` to the correct extractor, writes the command
- Swapping or removing ADB entirely = replace/remove `HeadendHQ.AdbMapping` only

### ADB Mapping Triggers
- **Automatically** after every nightly scrape run (`ScheduleScraperJob` calls it when done)
- **On-demand** via `POST /adb/map` endpoint (for manual re-runs)

### Cron Schedule
- Nightly scrape via `IHostedService` background service (same pattern as `HdHomerunXmltvJob`)
- Scrape window: next 7 days of events (configurable via `appsettings.json`)

### Cleanup
- Delete `SportingEvent` records older than N days after scraping
- Retention window configurable via `appsettings.json`

### Guidelines
- Scraper errors for one source must not crash others — catch and log per-source
- No direct `HttpClient` calls outside of source implementations
- Use a typed `HttpClient` per source
- Follow existing Minimal API endpoint extension method pattern
- No EF migrations — `EnsureCreated()` handles schema (existing project pattern)