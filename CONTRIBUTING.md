# Contributing Guidelines

Thank you for considering contributing to this Docker Compose infrastructure stack! These guidelines explain how to add or modify services, maintain repository structure, and adhere to project standards.

---

## How to Contribute

### 1. Adding or Modifying Services in `docker-compose.yaml`

When adding a new container or updating an existing service, adhere strictly to the following conventions:

* **Container Identity & Image**:
  * Set `container_name: <service-name>`.
  * Set `restart: unless-stopped`.
  * Prefer images from trusted registries like LinuxServer.io (`lscr.io/linuxserver/...`) or GitHub Container Registry (`ghcr.io/...`).

* **Monitoring & Dependencies**:
  * Add dependencies for non-infrastructure services:
    ```yaml
    depends_on:
      - "diun"
      - "container-mon"
    ```
  * Enable monitoring labels:
    ```yaml
    labels:
      - "containermon.enable=true"
      - "diun.enable=true"
    ```

* **Networking & Ports**:
  * Include explicit DNS resolution:
    ```yaml
    dns:
      - "1.1.1.1"
      - "9.9.9.9"
    ```
  * Format port mappings as double-quoted strings (e.g., `- "8080:8080"`).

* **Volumes & Storage**:
  * **Bind Mounts Only**: Use host bind mounts relative to environment variables rather than Docker named volumes.
  * Config volume: `"${SERVICE_DIR}/<service-name>:/config"` (or `/app/config`).
  * Data volume: `"${DATA_DIR}/<path>:/data"`.

* **Environment Variables**:
  * Format as an array of double-quoted strings (`- "KEY=${VAR}"`).
  * Standard system and user/group variables:
    ```yaml
    environment:
      - "PGID=${PGID}"
      - "PUID=${PUID}"
      - "TZ=${TZ}"
      - "UMASK=${UMASK}"
    ```

* **Healthchecks**:
  * Define a healthcheck for services:
    ```yaml
    healthcheck:
      test: curl --fail http://localhost:<port> || exit 1
      interval: 60s
      retries: 5
      start_period: 60s
      timeout: 10s
    ```

---

### 2. Updating Management Scripts (`update.sh`)

* Whenever a service is added or removed, update the `services` array in `update.sh`.
* Ensure the array in `update.sh` remains in **alphabetical order**.

---

### 3. Updating Documentation & Changelog

* **[README.md](README.md)**: If adding a new service, update the services table in `README.md` with the service name, container name, ports, and a brief description.
* **[CHANGELOG.md](CHANGELOG.md)**: Record all structural, service, and configuration updates under the appropriate release version using [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

### 4. Validation & Testing

Before submitting your changes, verify the compose setup:

1. Validate YAML syntax and Compose configuration:
   ```bash
   docker compose config
   ```
2. Verify interactive script syntax:
   ```bash
   bash -n update.sh
   ```
