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
2. SchedulesDirect integration — EPG XML fetch, store at static URL, nightly cron job to update
3. Schedule scraper — sporting events from NBA.com/NFL.com/ESPN, ADB command endpoint, dummy VOD videos, cleanup service
4. ErsatzTV channel mapping — API integration, smart game-to-channel assignment, nightly cron
5. Custom ErsatzTV scripted schedules — schedule games first, backfill, dead air 2am–7am, dummy videos until encoder arrives
6. Home Assistant automations — consume ErsatzTV XMLTV, watch Tivimate playback, trigger ADB commands
7. Smarter HA automations — channel warming, OT logic, etc.

## Key Integrations
- **ScheudlesDirect** — local channel EPG data for HDHomeRun channels in Tivimate
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

## API Documentation
Schedules Direct JSON API docs are located in `/docs/schedules-direct-wiki/`.
Read these files when working on anything related to the SD API integration.
Key files:
- `API.md` - main endpoint reference
- `ErrorCodes.md` - error handling

## Schedules Direct Integration Notes
- Base URL: https://json.schedulesdirect.org/20141201/
- Password must be SHA1 hashed before sending to /token
- Token expires every 24hrs, passed in header as `token: VALUE`
- Lineup ID: USA-OTA-45014
- Fetch 2 days of schedule data (EPG_DAYS env variable)
- Match HDHomeRun lineup.json channel numbers to SD station IDs
- Credentials from env vars: SD_USERNAME, SD_PASSWORD
- Output xmltv.xml served over HTTP for TiviMate