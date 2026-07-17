# Feature Analysis: Template Catalog Expansion

## Current State
- **25 templates** across **10 categories** (Web, Databases, Management, Home, Automation, Dashboard, Media, Monitoring, Productivity, Networking)
- Hosted at `github.com/japperJ/wincontainer-templates`
- Fetched by WinContainers app on launch, cached 24h, falls back to 6 seeds

## Identified Gaps
The following categories have **zero entries** in the current catalog. Each represents a distinct self-hosting use case with well-known, actively maintained Docker images.

---

## New Category: Git / Source Control

### Feature: Gitea
- **Standard approach:** Lightweight self-hosted Git service (fork of Gogs), Go binary, single container
- **Image:** `gitea/gitea:latest`
- **Port:** 3000 (web), 22 (SSH)
- **Compose pattern:** Gitea container + optional PostgreSQL + volume for data
- **Verify source:** `docs.gitea.com/installation/install-with-docker` — official docs, HIGH confidence
- **Docker Hub:** `gitea/gitea` — official image, HIGH confidence
- **Pitfalls:**
  - SSH port 22 conflicts with host SSH — remap to non-standard port (e.g., 2222)
  - Default config generates `app.ini` on first run; manual edits require container restart
  - Forgejo is a community fork (same image pattern) if the user prefers it

---

## New Category: CMS (Content Management)

### Feature: WordPress
- **Standard approach:** PHP-based CMS with MySQL backend, official Docker images
- **Image:** `wordpress:latest` + `mysql:8.0`
- **Port:** 8080:80
- **Verify source:** `hub.docker.com/_/wordpress` — official Docker Library, HIGH confidence
- **Compose:** Official docker-library docs provide ready-to-use `docker-compose.yml`

### Feature: Ghost
- **Standard approach:** Node.js-based publishing platform with MySQL
- **Image:** `ghost:5-alpine` + `mysql:8.0`
- **Port:** 8080:2368
- **Verify source:** `hub.docker.com/_/ghost` — official Docker Library, HIGH confidence
- **Compose:** Official docker-library docs provide ready-to-use `compose.yaml`

---

## New Category: Analytics

### Feature: Matomo
- **Standard approach:** PHP-based web analytics (formerly Piwik), MySQL backend
- **Image:** `matomo:latest` (official) + `mysql:8.0`
- **Port:** 8080:80
- **Verify source:** `matomo.org/faq/how-to-install/install-matomo-with-docker/` — official, HIGH confidence
- **Compose:** Official FAQ provides compose examples in `.examples/` folder

---

## New Category: Finance

### Feature: Firefly III
- **Standard approach:** PHP-based personal finance manager (double-entry bookkeeping), PostgreSQL recommended
- **Image:** `fireflyiii/core:latest` + `postgres:16-alpine`
- **Port:** 8080:8080
- **Verify source:** `docs.firefly-iii.org/how-to/firefly-iii/installation/docker/` — official docs, HIGH confidence
- **Pitfalls:**
  - Requires `APP_KEY` (32-char random string) — must be generated before first run
  - Cron container needed for recurring transactions, budget alerts
  - Separate Data Importer container for bank CSV/API imports (GoCardless/Spectre)
  - Multi-container stack is non-trivial — simplify for a single-entry template

### Recommendation
Use the **simplified** compose with just `fireflyiii/core:latest` + `postgres:16-alpine` + minimal env vars. Omit cron and data-importer for the template, note them in description.

---

## New Category: Development

### Feature: code-server (VS Code in Browser)
- **Standard approach:** Browser-based VS Code via LinuxServer.io image
- **Image:** `lscr.io/linuxserver/code-server:latest`
- **Port:** 8443:8443
- **Verify source:** `docs.linuxserver.io/images/docker-code-server/` — official LinuxServer docs, HIGH confidence
- **Compose:** Ready-to-use compose snippet in docs
- **Pitfalls:**
  - Default password auth — recommend setting `PASSWORD` env var
  - `PUID/PGID` must match host user for volume permissions
  - No persistence of extensions unless `/config` is mounted

---

## New Category: Media Automation (The *Arr Stack)

### Feature: Sonarr (TV Shows), Radarr (Movies), Prowlarr (Indexers)
- **Standard approach:** LinuxServer.io images, single root path `/data` for hardlinks
- **Images:** `lscr.io/linuxserver/sonarr:latest`, `lscr.io/linuxserver/radarr:latest`, `lscr.io/linuxserver/prowlarr:latest`
- **Ports:** 8989 (Sonarr), 7878 (Radarr), 9696 (Prowlarr)
- **Verify source:** LinuxServer.io docs — HIGH confidence
- **Pitfalls:**
  - **Hardlinks require single root mount** — all *arr containers must share the same `/data` path. Separate volume mounts break atomic moves
  - `PUID/PGID` mismatch causes "permission denied" on imports
  - Prowlarr must be configured *first* (add indexers), then connected to Sonarr/Radarr as "Applications"
  - Without a download client (qBittorrent, SABnzbd), the stack is non-functional — template should note this dependency
  - Best to include all 3 in one template entry with multi-service compose

---

## New Category: Music

### Feature: Navidrome
- **Standard approach:** Open-source music server, lightweight Go binary
- **Image:** `deluan/navidrome:latest`
- **Port:** 4533:4533
- **Verify source:** Docker Hub `deluan/navidrome` — HIGH (official image, active project)
- **Compose:** Single service + volume for config + volume/bind for music

---

## New Category: Photo Management

### Feature: Immich
- **Standard approach:** Full-stack photo/video management (Next.js frontend + NestJS backend + ML), multi-container
- **Image:** Complex multi-service (microservices, postgres, redis) — official compose at `immich-app/immich`
- **Port:** 2283:2283 (web)
- **Verify source:** `docs.immich.app` — HIGH confidence
- **Pitfalls:**
  - **Complex stack** — 10+ containers in production compose (server, web, machine learning, postgres, redis, typesense). Too heavy for a single template entry
  - Requires `.env` file with specific config
  - Consider Photoprism (simpler) or LibrePhotos (simpler) as alternatives

### Recommendation
Skip Immich (too complex). Add **PhotoPrism** instead as lighter alternative.

### Feature: PhotoPrism
- **Image:** `photoprism/photoprism:latest`
- **Port:** 2342:2342
- **Compose:** Single service or with MariaDB, supports TensorFlow for AI tagging

---

## New Category: Knowledge Base / Wiki

### Feature: Outline
- **Standard approach:** Node.js wiki/knowledge base, PostgreSQL + Redis, OIDC auth
- **Image:** `outlinewiki/outline:latest` + `postgres:16-alpine` + `redis:7-alpine`
- **Port:** 3000:3000
- **Verify source:** `docs.getoutline.com/s/hosting/doc/docker` — official docs, HIGH confidence
- **Pitfalls:**
  - **Requires OIDC/OAuth provider** — no built-in username/password auth. This makes it a poor template for newcomers
  - Generates `SECRET_KEY` and `UTILS_SECRET` via `openssl rand -hex 32` — must be set before first run
  - Requires domain + reverse proxy for production
  - Needs SMTP config for email notifications

### Recommendation
Skip Outline (auth barrier too high). Add **BookStack** instead — simpler, has built-in auth.

---

## New Category: Games

### Feature: Minecraft Server
- **Standard approach:** `itzg/minecraft-server` image — auto-downloads latest server jar
- **Image:** `itzg/minecraft-server:latest`
- **Port:** 25565:25565
- **Verify source:** `github.com/itzg/docker-minecraft-server` — HIGH confidence (10k+ stars, active)
- **Compose:** Single service, set `EULA=TRUE` env var (required by Mojang EULA)
- **Pitfalls:**
  - Must accept Mojang EULA (`EULA=TRUE`) — legal requirement, document explicitly
  - RAM heavy (2-4 GB minimum for modded servers)
  - `itzg/minecraft-server` supports many server types (vanilla, Paper, Forge, Fabric) via `TYPE` env var

---

## Existing Category Expansions

### Dashboard: Add Organizr or Flame
- **Organizr:** `organizr/organizr` — PHP-based landing page for your server
- **Flame:** `pawelmalak/flame` — simple Go dashboard with built-in editor
- Both simpler alternatives to Homer/Heimdall

### Networking: Add Traefik
- **Image:** `traefik:latest`
- **Port:** 80:80, 443:443, 8080:8080 (dashboard)
- **Compose:** Single service with labels for auto-discovery
- **Pitfall:** Dashboard access requires `--api.insecure=true` for test or a proper config for production

### Monitoring: Add Netdata
- **Image:** `netdata/netdata:latest`
- **Port:** 19999:19999
- **Compose:** Single service, mounts `/etc/passwd`, `/etc/group`, `/proc`, `/sys` for host metrics
- **Pitfall:** Read-only filesystem requires specific volume cap-add

---

## Summary Table

| Category | Recommended | Confidence | Reason |
|---|---|---|---|
| Git | Gitea | HIGH | Simple, single container, official image |
| CMS | WordPress, Ghost | HIGH | Official Docker Library images |
| Analytics | Matomo | HIGH | Official image, active project |
| Finance | Firefly III | HIGH | Most capable, active, good docs |
| Development | code-server | HIGH | LinuxServer.io, single container |
| Media Automation | Sonarr+Radarr+Prowlarr | HIGH | LinuxServer.io, well documented |
| Music | Navidrome | HIGH | Single binary, lightweight |
| Photo Management | PhotoPrism (skip Immich) | MEDIUM | Immich too complex for template |
| Wiki | BookStack (skip Outline) | MEDIUM | Outline needs OIDC auth provider |
| Games | Minecraft (itzg) | HIGH | 10k+ GitHub stars, active |
| Dashboard | Organizr, Flame | MEDIUM | Niche niche — may not add much |
| Networking | Traefik | HIGH | Critical reverse proxy for homelab |
| Monitoring | Netdata | HIGH | Beautiful real-time dashboards |

## Sources

| Source | Type | Confidence |
|---|---|---|
| docs.gitea.com/installation/install-with-docker | Official | HIGH |
| hub.docker.com/_/wordpress | Official | HIGH |
| hub.docker.com/_/ghost | Official | HIGH |
| matomo.org/faq/how-to-install/install-matomo-with-docker/ | Official | HIGH |
| docs.firefly-iii.org/how-to/firefly-iii/installation/docker/ | Official | HIGH |
| docs.linuxserver.io/images/docker-code-server/ | Official | HIGH |
| docs.immich.app/install/docker-compose | Official | HIGH |
| docs.getoutline.com/s/hosting/doc/docker | Official | HIGH |
| github.com/itzg/docker-minecraft-server | Official (GitHub) | HIGH |
| docker.recipes/media/sonarr-radarr-stack | Web (guide) | MEDIUM |
| selfhosting.sh/apps/firefly-iii/ | Web (guide) | MEDIUM |
