# docker

My fester-server Ubuntu server Docker setup.

This repository contains a Docker Compose configuration for running various media, automation, and self‑hosted services on a home server. It's designed to be easy to manage, update, and monitor.

## Services

The stack includes the following services (all defined in `docker-compose.yaml`):

| Service                    | Container Name         | Port(s)             | Description                                      |
| -------------------------- | ---------------------- | ------------------- | ------------------------------------------------ |
| **Diun**                   | `diun`                 | –                   | Docker image update notifier                     |
| **Container Monitor**      | `container-mon`        | –                   | Monitors container health and sends Slack alerts |
| **Transmission + OpenVPN** | `transmission-openvpn` | 9099                | Torrent client with VPN for secure downloads     |
| **Transmission (Rush)**    | `transmission-rush`    | 9091, 51413/tcp+udp | Secondary torrent client without VPN             |
| **SABnzbd**                | `sabnzbd`              | 8080, 9090          | Usenet downloader                                |
| **FlareSolverr**           | `flaresolverr`         | 8191                | Proxy for bypassing Cloudflare protection        |
| **Prowlarr**               | `prowlarr`             | 9696                | Indexer manager for \*arr apps                   |
| **Emby**                   | `emby`                 | 8096, 8920          | Media server with hardware transcoding           |
| **Bazarr**                 | `bazarr`               | 6767                | Subtitle manager                                 |
| **Radarr**                 | `radarr`               | 7878                | Movie management                                 |
| **Sonarr**                 | `sonarr`               | 8989                | TV show management                               |
| **Seerr**                  | `seerr`                | 5056                | Media request management                         |
| **Crosswatch**             | `crosswatch`           | 8787                | Cross-platform media server watch status sync    |
| **Homepage**               | `homepage`             | 3333                | Dashboard for all services                       |
| **Docker Socket Proxy**    | `dockerproxy`          | 127.0.0.1:2375      | Read‑only proxy for Docker socket                |
| **DuckDNS**                | `duckdns`              | –                   | Dynamic DNS updater                              |
| **SWAG**                   | `swag`                 | 80, 81, 443         | Nginx reverse proxy with Let's Encrypt           |
| **Rating Poster Database** | `rpdb-folders`         | 8750                | Fetches ratings and posters for media            |
| **Dockhand**               | `dockhand`             | 3210                | Docker container manager UI                      |

Each service is configured with health checks, proper volume mounts, and Slack notifications where applicable.

## Quick Start

### Prerequisites

- Docker and Docker Compose installed
- A Slack workspace (for notifications)
- A DuckDNS account (for dynamic DNS)
- PIA (Private Internet Access) or another OpenVPN provider (optional)

### Configuration

1. Copy `.env-template` to `.env` and fill in your values:
   ```bash
   cp .env-template .env
   ```
2. Adjust the following critical variables in `.env`:
   - `OPENVPN_PROVIDER`, `OPENVPN_CONFIG`, `OPENVPN_USERNAME`, `OPENVPN_PASSWORD`
   - `LOCAL_NETWORK` (your local subnet)
   - `SERVICE_DIR` and `DATA_DIR` (paths to configuration and media storage)
   - `SLACK_HOOK` (your Slack webhook URL)
   - `DUCKDNS_SUBDOMAINS` and `DUCKDNS_TOKEN`
   - Database credentials and other secrets

3. Ensure the directories referenced by `SERVICE_DIR` and `DATA_DIR` exist and have the correct permissions.

4. Start the stack:
   ```bash
   docker compose up -d
   ```

## Useful Commands

### Shell access

```bash
docker exec -it <service-name> /bin/bash
```

### Monitor logs

```bash
docker logs -f <service-name>
```

### Test NGINX and reload (SWAG container)

```bash
docker exec -it swag nginx -t
docker exec -it swag nginx -s reload
```

### List all running services

```bash
docker ps -a
```

### Remove all stopped services

```bash
docker system prune
```

### Remove a specific service

```bash
docker rm <name>
```

### Stop all services

```bash
docker stop $(docker ps -q)
```

### Remove all containers (both running and stopped)

```bash
docker rm $(docker ps -aq)
```

### Remove all Docker images

```bash
docker rmi $(docker images -q)
```

### Remove dangling images and unused images

```bash
docker image prune -a
```

### Check transmission-openvpn container is using the VPN

```bash
docker exec transmission-openvpn curl -s https://ipinfo.io/json
```

### Use transmission-openvpn VPN for other containers

Set the following parameters in the service’s `docker-compose.yaml` entry (after restart is a good place):

```yaml
restart: always
network_mode: "service:transmission-openvpn"
depends_on:
  - transmission-openvpn
```

Then verify the service is using the VPN:

```bash
docker exec <service-name> curl -s https://ipinfo.io/json
```

## Update Script

A convenience script `update.sh` is provided to pull and restart individual services or all services at once.

Run it with:

```bash
./update.sh
```

You will be presented with a numbered list of services. Enter the number of the service you want to update, or type `all` to update everything. After updating, the script will prune unused Docker images.

Alternatively, you can run the script with the `all` argument to update all services non‑interactively:

```bash
./update.sh all
```

Any other argument (or no argument) will launch the interactive mode.

## Environment Variables

Key environment variables (defined in `.env`):

| Variable                        | Purpose                            | Example                |
| ------------------------------- | ---------------------------------- | ---------------------- |
| `SERVICE_DIR`                   | Path to service configs            | `/home/user/docker`    |
| `DATA_DIR`                      | Path to media storage              | `/home/user/media`     |
| `HOST_HOME_DIR`                 | Host home directory for mounts     | `/home`                |
| `DOCKER_SOCK`                   | Path to Docker daemon socket       | `/var/run/docker.sock` |
| `DNS_PRIMARY` / `DNS_SECONDARY` | DNS resolver IPs                   | `1.1.1.1` / `9.9.9.9`  |
| `PUID` / `PGID`                 | User/group IDs for containers      | `1026` / `100`         |
| `UMASK`                         | Default umask                      | `022`                  |
| `TZ`                            | Timezone                           | `Etc/UTC`              |
| `EMBY_GIDLIST`                  | Render/video group IDs for Emby    | `44,993`               |
| `OPENVPN_PROVIDER`              | VPN provider                       | `PIA`                  |
| `OPENVPN_CONFIG`                | VPN server location                | `ca_vancouver`         |
| `OPENVPN_USERNAME` / `PASSWORD` | VPN credentials                    | `…`                    |
| `LOCAL_NETWORK`                 | Local subnet for access            | `192.168.86.0/24`      |
| `SLACK_HOOK`                    | Slack webhook for notifications    | `…`                    |
| `DUCKDNS_SUBDOMAINS` / `TOKEN`  | DuckDNS configuration              | `…`                    |
| `SWAG_URL` / `SWAG_EMAIL`       | Domain and email for Let's Encrypt | `…` / `…`              |
| `PORT_<SERVICE>`                | Host port mappings for services    | `8080`, `3333`, etc.   |

See the `.env-template` file for the full list.

## Directory Structure

```
/home/user/docker/
├── .env                    # Environment variables
├── docker-compose.yaml     # Main service definitions
├── update.sh               # Update script
├── CHANGELOG.md            # Project changelog
├── LICENSE                 # MIT License
└── service‑specific config directories (bazarr/, diun/, swag/, etc.)
```

Each service has its own subdirectory under `SERVICE_DIR` (by default `/home/user/docker`) for persistent configuration.

## License

This project is licensed under the MIT License—see the [LICENSE](LICENSE) file for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a detailed list of changes, features, and fixes across all versions.
