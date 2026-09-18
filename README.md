# HeadendHQ

A self-hosted application for managing media sources for Video On Demand and Live Tv for a media server (Jellyfin/Plex). Orchestrates VOD cards and EPGData (coming soon) via HDHomerun, sports schedule scraping, ErsatzTV channel mapping, and ADB stream control — designed to run as a single Docker container on Unraid.

## Architecture

- **ASP.NET Core / .NET 10** — Minimal APIs
- **SQLite + EF Core** — lightweight persistence, no separate DB container

## Integrations

| System | Role |
|---|---|
| HDHomerun | Fetches local channel EPG XML for HDHomeRun / Tivimate |
| Home Assistant | Automation brain; Can read API to control ADB commands to local androidTVs; |
| Jellyfin | Hosts dummy VOD Titles |

## Roadmap

- [x] **Step 1** — App scaffold, Dockerfile, CI/CD pipeline, `/health` endpoint
- [x] **Step 2** — HDHomerun integration: EPG XML fetch, static URL, nightly cron
- [ ] **Step 3** — VOD & Schedule Scraping: Fetch metadata & links to NBA/NFL/ESPN events, map to ADB command, cleanup service, create VOD folders that a Media server can read with appropriate metadata & artwork
- [ ] **Step 4** — Integrate ErsatzTv for LiveTv experience with Media server content & our Live sports (See ADBTuner for example of how this will work)
- [ ] **Step 5** — Home Assistant automations: XMLTV consumption, Tivimate playback watching, ADB triggers
- [ ] **Step 6** — Smarter HA automations: channel warming, OT logic

## Local Development

**Prerequisites:** .NET 10 SDK, Node 22 + npm 11, Aspire CLI, Docker

| Path | Serves |
|---|---|
| `/` | Angular client (`HeadendHQ.Client`) |
| `/api/*` | API endpoints (e.g. `/api/iptv/guide`) |
| `/scalar/v1` | API documentation |
| `/hangfire` | Background job dashboard |

In Development the API forwards every non-API request to the Angular dev server (the `ReverseProxy` section in `appsettings.Development.json`, `http://localhost:4200`), so browse to the API's port. In the Docker image the built client is served from `wwwroot`.

`HeadendHQ.AppHost` (Aspire) is the dev startup. It starts the Angular dev server (`client`, port 4200) and the API (`web`, port 7208); browse to `https://localhost:7208`. The Aspire dashboard is at `https://localhost:17178`. The `client` resource has a persistent lifetime, so `ng serve` keeps running after the AppHost stops and is reused on the next run. Stop it from the Aspire dashboard, or kill the `ng serve` process.

```bash
# Run everything (from the repo root)
cd HeadendHQ.Client && npm install && cd ..
aspire run

# Verify health endpoint
curl https://localhost:7208/api/health

# Build Docker image
docker build -t headendhq .

# Run Docker image
docker run -p 5000:8080 headendhq
```