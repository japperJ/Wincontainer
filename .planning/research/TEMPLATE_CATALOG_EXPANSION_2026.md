# Template Catalog Expansion Research

## Scope and Current Inventory

The live catalog at `https://github.com/japperJ/wincontainer-templates/blob/main/templates.yaml` currently contains **38 named entries**. I checked names and image references before selecting additions; none of the candidates below duplicates an existing catalog entry or image.

Existing coverage includes Web, Databases, Management, Home, Automation, Dashboard, Media, Monitoring, Productivity, Networking, Git, CMS, Analytics, Finance, Development, Media Automation, Music, Photo Management, Wiki, and Games.

The catalog schema is compatible with:

- `name`, `description`, `category`, `image`, `container_name`, `website`, and literal `compose` YAML.
- Compose `services` containing `image`, `ports`, `volumes`, `environment`, and optional `container_name`.
- Multiple services in one template.

The WinContainers importer ignores unsupported Compose details such as restart policies, health checks, GPU reservations, devices, and network mode. Candidates requiring those features should be documented as advanced or deferred rather than presented as guaranteed one-click installs.

## Recommended Additions

These 25 candidates are popular self-hosted projects, have a maintained upstream project/image, and fill gaps not represented in the live catalog. Popularity was cross-checked using the Awesome Selfhosted catalog and GitHub repository activity; image and installation references point to project-owned documentation or registries.

| # | Name | Category | Recommended image | Typical port | Install shape | Confidence | Official source |
|---:|---|---|---|---:|---|---|---|
| 1 | Forgejo | Git | `codeberg.org/forgejo/forgejo:latest` | 3000 | 1 service | HIGH | https://forgejo.org/docs/latest/admin/installation/docker/ |
| 2 | Syncthing | File Sync | `syncthing/syncthing:latest` | 8384 | 1 service | HIGH | https://docs.syncthing.net/users/faq.html |
| 3 | File Browser | File Management | `filebrowser/filebrowser:latest` | 8080 | 1 service | HIGH | https://filebrowser.org/installation |
| 4 | Paperless-ngx | Documents | `paperlessngx/paperless-ngx:latest` | 8000 | 3 services | HIGH | https://docs.paperless-ngx.com/setup/ |
| 5 | Stirling-PDF | Documents | `stirlingtools/stirling-pdf:latest` | 8080 | 1 service | HIGH | https://github.com/Stirling-Tools/Stirling-PDF |
| 6 | Mealie | Recipes | `ghcr.io/mealie-recipes/mealie:latest` | 9000 | 1 service | HIGH | https://docs.mealie.io/documentation/getting-started/installation/ |
| 7 | Vikunja | Tasks | `vikunja/vikunja:latest` | 3456 | 1 service | HIGH | https://vikunja.io/docs/docker-walkthrough/ |
| 8 | Memos | Notes | `ghcr.io/usememos/memos:stable` | 5230 | 1 service | HIGH | https://www.usememos.com/docs/deploy-with-docker |
| 9 | SearXNG | Search | `searxng/searxng:latest` | 8080 | 1 service | HIGH | https://docs.searxng.org/admin/installation-docker.html |
| 10 | ntfy | Notifications | `binwiederhier/ntfy:latest` | 80 | 1 service | HIGH | https://docs.ntfy.sh/install/ |
| 11 | Gotify | Notifications | `gotify/server:latest` | 80 | 1 service | HIGH | https://gotify.net/docs/install |
| 12 | Audiobookshelf | Audio Media | `ghcr.io/advplyr/audiobookshelf:latest` | 80 | 1 service | HIGH | https://www.audiobookshelf.org/docs/ |
| 13 | Calibre-Web | E-books | `lscr.io/linuxserver/calibre-web:latest` | 8083 | 1 service | HIGH | https://docs.linuxserver.io/images/docker-calibre-web/ |
| 14 | Jellyseerr | Media Discovery | `fallenbagel/jellyseerr:latest` | 5055 | 1 service | HIGH | https://github.com/Fallenbagel/jellyseerr |
| 15 | qBittorrent | Downloads | `lscr.io/linuxserver/qbittorrent:latest` | 8080 | 1 service | HIGH | https://docs.linuxserver.io/images/docker-qbittorrent/ |
| 16 | SABnzbd | Downloads | `lscr.io/linuxserver/sabnzbd:latest` | 8080 | 1 service | HIGH | https://docs.linuxserver.io/images/docker-sabnzbd/ |
| 17 | Homebridge | Home Automation | `homebridge/homebridge:latest` | 8581 | 1 service | HIGH | https://github.com/homebridge/homebridge/wiki/Install-Homebridge-on-Docker |
| 18 | Eclipse Mosquitto | IoT Messaging | `eclipse-mosquitto:2` | 1883 | 1 service | HIGH | https://mosquitto.org/documentation/ |
| 19 | Zigbee2MQTT | IoT | `koenkk/zigbee2mqtt:latest` | 8080 | 1 service | HIGH | https://www.zigbee2mqtt.io/guide/installation/01_linux.html |
| 20 | MinIO | Object Storage | `quay.io/minio/minio:latest` | 9001 | 1 service | MEDIUM | https://min.io/docs/minio/container/index.html |
| 21 | RabbitMQ | Message Broker | `rabbitmq:4-management` | 15672 | 1 service | HIGH | https://hub.docker.com/_/rabbitmq |
| 22 | Open WebUI | AI Interface | `ghcr.io/open-webui/open-webui:main` | 8080 | 1 service, optional Ollama | HIGH | https://docs.openwebui.com/getting-started/quick-start/ |
| 23 | Ollama | Local AI Runtime | `ollama/ollama:latest` | 11434 | 1 service | HIGH | https://ollama.com/blog/ollama-is-now-available-as-an-official-docker-image |
| 24 | Headscale | VPN Control Plane | `headscale/headscale:latest` | 8080 | 1 service | MEDIUM | https://headscale.net/stable/setup/install/container/ |
| 25 | Duplicati | Backup | `linuxserver/duplicati:latest` | 8200 | 1 service | HIGH | https://docs.linuxserver.io/images/docker-duplicati/ |

## Priority Tiers

### Add first

Forgejo, Syncthing, Paperless-ngx, Stirling-PDF, Mealie, Audiobookshelf, qBittorrent, SABnzbd, MinIO, Open WebUI, Ollama, and Duplicati provide the strongest coverage improvement and are recognizable to users.

### Add second

File Browser, Vikunja, Memos, SearXNG, ntfy, Gotify, Calibre-Web, Jellyseerr, Homebridge, Mosquitto, Zigbee2MQTT, RabbitMQ, and Headscale are useful but have more configuration or host-integration requirements.

## Compatibility Risks

| Candidate | Risk | Mitigation |
|---|---|---|
| Paperless-ngx | Requires Redis and a database in the supported production stack; OCR is resource-intensive. | Include all required services and mark the template as multi-service. Avoid a fake single-container entry. |
| Zigbee2MQTT | Requires access to a USB coordinator through Compose `devices`, which the importer does not currently support. | Add only after device passthrough is supported, or document that the initial template is incomplete. |
| Homebridge | Many plugins need host networking, mDNS, or device access. | Start with the basic web UI template and call out plugin/network limitations. |
| Open WebUI / Ollama | GPU support requires runtime-specific GPU configuration not represented by the importer. | Provide a CPU-safe template and explicitly document optional GPU setup. |
| Headscale | Requires TLS/reverse proxy and client-side configuration for useful deployment. | Treat as an advanced networking template, not a beginner one-click entry. |
| MinIO | Current upstream direction and image registry should be checked before merging. | Pin a tested image tag and validate the console/API port pair. |
| qBittorrent / SABnzbd | Download clients are most useful with shared storage and the existing *Arr entries. | Use a common `/data` mount convention and explain permissions. |
| Paperless-ngx / Memos | Some upstream examples use generated secrets or environment files. | Generate safe placeholders and clearly label values users must replace. |

## Explicitly Not Recommended

- **Immich:** too many services and hardware-dependent machine-learning options for the current importer.
- **Outline:** requires external OIDC/OAuth configuration and secrets before first use.
- **Coolify, Appwrite, ToolJet:** these are platform stacks rather than simple application templates and require many services, privileged behavior, or extensive environment configuration.
- **Kubernetes-only projects:** outside the current WSLC container model.

## Sources

| Source | Use | Confidence |
|---|---|---|
| https://raw.githubusercontent.com/japperJ/wincontainer-templates/main/templates.yaml | Live catalog and duplicate check | HIGH |
| https://github.com/awesome-selfhosted/awesome-selfhosted | Ecosystem popularity/discovery cross-check | HIGH |
| https://api.github.com/search/repositories?q=topic%3Aself-hosted+docker&sort=stars&order=desc&per_page=30 | Current GitHub activity and popularity signal | HIGH |
| https://docs.docker.com/reference/cli/docker/compose/ | Compose service and multi-file behavior | HIGH |
| Project-owned official links in the candidate table | Image/install verification | HIGH for all except MinIO and Headscale, MEDIUM pending a final tag/registry check |

## Recommendation

Add the 25 entries in priority order, but do not blindly copy upstream examples. Before publishing, normalize port mappings to avoid collisions, replace demo passwords/secrets, pin production image tags where practical, and omit unsupported Compose fields until the importer can represent them.
