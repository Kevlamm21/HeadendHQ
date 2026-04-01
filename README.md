# HeadendHQ

A self-hosted ASP.NET Core backend for managing a smart TV headend. Orchestrates local EPG data via Zap2xml, sports schedule scraping, ErsatzTV channel mapping, and ADB stream control — designed to run as a single Docker container on Unraid.

## Architecture

- **ASP.NET Core / .NET 10** — Minimal APIs, no controllers
- **SQLite + EF Core** — lightweight persistence, no separate DB container
- **Single Docker container** — hosted on Unraid, behind a reverse proxy
- **GitHub Actions → GHCR** — CI/CD pipeline builds and pushes the image, then deploys via SSH

## Integrations

| System | Role |
|---|---|
| Zap2xml | Fetches local channel EPG XML for HDHomeRun / Tivimate |
| ErsatzTV | Virtual channel scheduling; HeadendHQ assigns games to channels |
| Home Assistant | Automation brain; watches Tivimate playback + ErsatzTV XMLTV |
| ADB | Controls encoder stream devices |
| Plex | Hosts dummy VOD sporting events until encoder is available |

## Roadmap

- [x] **Step 1** — App scaffold, Dockerfile, CI/CD pipeline, `/health` endpoint
- [ ] **Step 2** — Zap2xml integration: EPG XML fetch, static URL, nightly cron
- [ ] **Step 3** — Schedule scraper: NBA/NFL/ESPN events, ADB command endpoint, cleanup service
- [ ] **Step 4** — ErsatzTV channel mapping: API integration, game-to-channel assignment, nightly cron
- [ ] **Step 5** — Custom ErsatzTV scripted schedules: game-first, backfill, dead air, dummy videos
- [ ] **Step 6** — Home Assistant automations: XMLTV consumption, Tivimate playback watching, ADB triggers
- [ ] **Step 7** — Smarter HA automations: channel warming, OT logic

## Local Development

**Prerequisites:** .NET 10 SDK, Docker

```bash
# Run locally
cd src/HeadendHQ.Web
dotnet run

# Verify health endpoint
curl http://localhost:5291/health

# Build Docker image
docker build -t headendhq .

# Run Docker image
docker run -p 5000:8080 headendhq
```

## CI/CD Setup

The deploy workflow requires these GitHub Actions secrets:

| Secret | Description |
|---|---|
| `UNRAID_HOST` | IP or hostname of your Unraid server |
| `UNRAID_USER` | SSH user (`root` on Unraid by default) |
| `UNRAID_SSH_KEY` | Private SSH key with access to Unraid |

The workflow (`.github/workflows/deploy.yml`) triggers on push to `main`:
1. Builds the .NET project
2. Builds and pushes the Docker image to GHCR (`ghcr.io/kevlamm21/headendhq`)
3. SSHs into Unraid to pull the new image and restart the container on port `5000`
