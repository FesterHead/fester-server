# Project Guidelines for AI Agents

This repository manages a Docker Compose infrastructure stack for media services, torrent clients, reverse proxies, and container monitoring.

## Architecture & Conventions

### Service Structure in `docker-compose.yaml`

When adding or modifying services in `docker-compose.yaml`, adhere strictly to the following formatting and structural standards:

1. **Naming & Container Configuration**:
   - Explicitly specify `container_name: <service-name>`.
   - Set `restart: unless-stopped`.
   - Prefer LinuxServer.io or GitHub Container Registry images (`lscr.io/linuxserver/...` or `ghcr.io/...`).

2. **Monitoring & Dependencies**:
   - Non-infrastructure services must depend on container monitoring:
     ```yaml
     depends_on:
       - "diun"
       - "container-mon"
     ```
   - Enable monitoring labels:
     ```yaml
     labels:
       - "containermon.enable=true"
       - "diun.enable=true"
     ```

3. **Networking & DNS**:
   - Explicitly define DNS resolution using environment variables:
     ```yaml
     dns:
       - "${DNS_PRIMARY}"
       - "${DNS_SECONDARY}"
     ```
   - Format port mappings as double-quoted strings, parameterizing the host port with environment variables where applicable (e.g., `- "${PORT_SERVICE}:8080"` or `- "8080:8080"`).

4. **Volumes & Storage**:
   - **Bind Mounts Only**: Use host bind mounts relative to environment variables rather than Docker named volumes.
   - Standard config volume: `"${SERVICE_DIR}/<service-name>:/config"` (or `/app/config`).
   - Standard data volume: `"${DATA_DIR}/<path>:/data"`.
   - Standard docker socket: `"${DOCKER_SOCK}:/var/run/docker.sock"`.

5. **Environment Variables**:
   - Format as an array of double-quoted strings (`- "KEY=${VAR}"`).
   - Standard permission and system variables:
     ```yaml
     environment:
       - "PGID=${PGID}"
       - "PUID=${PUID}"
       - "TZ=${TZ}"
       - "UMASK=${UMASK}"
     ```

6. **Healthchecks**:
   - Define a healthcheck for services:
     ```yaml
     healthcheck:
       test: curl --fail http://localhost:<port> || exit 1
       interval: 60s
       retries: 5
       start_period: 60s
       timeout: 10s
     ```

### Service Management Script (`update.sh`)

- Whenever a service is added to or removed from `docker-compose.yaml`, update the `services` array in `update.sh`.
- Keep the `services` array in `update.sh` sorted **alphabetically**.

### Versioning & Changelog

- Document all structural, service, and configuration changes in [CHANGELOG.md](CHANGELOG.md).
- Adhere to [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### Git Workflow & Commit Rules

- **NO Automated `git add` or `git commit`**: The agent MUST NEVER run `git add`, `git commit`, or `git push` commands, and must NOT prompt or ask the user to run them.
- **User-Managed Commits**: All git staging, committing, and pushing is handled exclusively by the user.
- **Commit Messages**: Do NOT generate or propose commit messages unless the user explicitly requests assistance with one.

### Instruction Synchronization

- Keep [AGENTS.md](AGENTS.md) and [.github/copilot-instructions.md](.github/copilot-instructions.md) synchronized whenever guidelines or project conventions are updated.
