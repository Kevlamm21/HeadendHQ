# HeadendHQ — Claude Agent Memory

## Project Overview
HeadendHQ is a self-hosted ASP.NET Core Web API (.NET 10) running as a single Docker container on an Unraid server. It serves as the central backend for managing a smart TV headend — orchestrating EPG data, sports schedule scraping, ErsatzTV channel mapping, and ADB stream control.

## Repo Structure
HeadendHQ/                  ← repo root
├── src/              ← .NET projects
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
3. ✅ Schedule scraper — sporting events from ESPN (multi-sport), sport/league/team + streaming preferences, ADB mapping, dummy VOD videos, cleanup service
4. ErsatzTV channel mapping — API integration, smart game-to-channel assignment, nightly cron
5. Custom ErsatzTV scripted schedules — schedule games first, backfill, dead air 2am–7am, dummy videos until encoder arrives
6. Home Assistant automations — consume ErsatzTV XMLTV, watch Tivimate playback, trigger ADB commands
7. Smarter HA automations — channel warming, OT logic, etc.


You should not run any tests in memory, when your work is completed you can run a dotnet build then give me a test plan to test what you just implimented