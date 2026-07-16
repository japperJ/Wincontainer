# Research Summary: Template Catalog Expansion

## Executive Summary

The current WinContainers template catalog has 25 entries across 10 categories. Research identified **13 missing categories** with well-established self-hosted Docker images. Adding entries for Gitea, WordPress, Ghost, Matomo, Firefly III, code-server, the \*Arr media stack (Sonarr/Radarr/Prowlarr), Navidrome, PhotoPrism, BookStack, Minecraft server, and Traefik would bring the catalog to **~45 entries across ~18 categories** — covering the vast majority of common self-hosting use cases.

**Key finding:** Most complex multi-service apps (Immich, Outline, Mattermost) are poor template candidates because they require 10+ containers, external auth providers, or extensive env file configuration. Prioritize **single-service** or **2-service** templates for usability.

## Key Findings

1. **Why not Immich** — 10+ containers, `.env` file required. PhotoPrism (2 containers) is the better template entry.
2. **Why not Outline** — requires OIDC/OAuth provider. No built-in auth. BookStack has simpler auth.
3. ***Arr stack works best as one entry** — Sonarr + Radarr + Prowlarr sharing a single `/data` volume mount. Separating them breaks hardlinks.
4. **Single-service templates dominate** — 10 of 13 recommended additions are 1-2 container stacks.
5. **Gaps by category size** — Adding 13 new templates grows catalog from 10 to ~18 categories. Every broad self-hosting category now covered.

## Recommended New Entries

| # | Name | Category | Containers | Image |
|---|---|---|---|---|
| 26 | Gitea | Git | 1 | gitea/gitea:latest |
| 27 | WordPress | CMS | 2 (WP + MySQL) | wordpress:latest |
| 28 | Ghost | CMS | 2 (Ghost + MySQL) | ghost:5-alpine |
| 29 | Matomo | Analytics | 2 (Matomo + MySQL) | matomo:latest |
| 30 | Firefly III | Finance | 2 (FF3 + PostgreSQL) | fireflyiii/core:latest |
| 31 | code-server | Development | 1 | lscr.io/linuxserver/code-server:latest |
| 32 | Sonarr, Radarr, Prowlarr | Media Automation | 3 | linuxserver/* |
| 33 | Navidrome | Music | 1 | deluan/navidrome:latest |
| 34 | PhotoPrism | Photo Management | 2 (PhotoPrism + MariaDB) | photoprism/photoprism:latest |
| 35 | BookStack | Wiki | 2 (BookStack + MySQL) | linuxserver/bookstack:latest |
| 36 | Minecraft Server | Games | 1 | itzg/minecraft-server:latest |
| 37 | Traefik | Networking | 1 | traefik:latest |
| 38 | Netdata | Monitoring | 1 | netdata/netdata:latest |

## Pitfalls to Document

- **Gitea:** SSH port 22 may conflict with host SSH
- **Firefly III:** Requires `APP_KEY` generation before first run
- **\*Arr stack:** Must share single root mount `/data` for hardlinks; requires separate download client
- **Minecraft:** `EULA=TRUE` required by Mojang
- **Code-server:** `PUID/PGID` must match host user
- **Traefik:** Dashboard access requires explicit config
- **Netdata:** Requires `--pid=host` and `SYS_PTRACE` capability for full host metrics

## Sources

| Source | Type | Confidence |
|---|---|---|
| docs.gitea.com/installation/install-with-docker | Official | HIGH |
| hub.docker.com/_/wordpress | Official | HIGH |
| hub.docker.com/_/ghost | Official | HIGH |
| matomo.org/faq/how-to-install/install-matomo-with-docker/ | Official | HIGH |
| docs.firefly-iii.org/installation/docker | Official | HIGH |
| docs.linuxserver.io/images/docker-code-server | Official | HIGH |
| docs.immich.app/install/docker-compose | Official | HIGH |
| docs.getoutline.com/s/hosting/doc/docker | Official | HIGH |
| github.com/itzg/docker-minecraft-server | Official (GitHub) | HIGH |
