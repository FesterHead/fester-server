# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]


### Changed
- Configured `seerr` with `user: "${PUID}:${PGID}"` and `crosswatch` with `APP_UID` and `APP_GID` in `docker-compose.yaml` to run containers with `1026:100` (`steve:users`) file ownership.

## [1.2.0] - 2026-09-13

### Added
- Added Git workflow and commit rules to [AGENTS.md](AGENTS.md) and [.github/copilot-instructions.md](.github/copilot-instructions.md) specifying user-managed commits and no automated git commands.
- Added environment variable configurations to [.env](.env) and [.env-template](.env-template) for DNS resolvers (`DNS_PRIMARY`, `DNS_SECONDARY`), Docker daemon socket (`DOCKER_SOCK`), host home directory (`HOST_HOME_DIR`), Emby hardware transcoding GIDs (`EMBY_GIDLIST`), Transmission limits, and host port mappings (`PORT_<SERVICE>`).

### Changed
- Replaced hardcoded host path in `dockhand` volume mapping with `${SERVICE_DIR}:${SERVICE_DIR}` in `docker-compose.yaml`.
- Parameterized DNS settings, Docker daemon socket mounts, Emby hardware transcoding GIDs, Homepage `/home` mount, Transmission limits, and host port mappings in `docker-compose.yaml` to use `.env` variables.
- Formatted `dockerproxy` ports, volumes, and environment variables in `docker-compose.yaml` to adhere to double-quoted string conventions.
- Updated [README.md](README.md) environment variables table and anonymized example paths to generic `/home/user/...` paths.
- Synchronized [AGENTS.md](AGENTS.md) and [.github/copilot-instructions.md](.github/copilot-instructions.md) with updated DNS, volume, and port configuration conventions.

## [1.1.0] - 2026-08-11

### Added
- Added `crosswatch` service to `docker-compose.yaml`.
- Added `crosswatch` to `update.sh`.
- Added `crosswatch` entry to [README.md](README.md) services table.
- Added [AGENTS.md](AGENTS.md) with repository guidelines for AI coding assistants.
- Added [CONTRIBUTING.md](CONTRIBUTING.md) with contribution instructions and standards.
- Added [.github/copilot-instructions.md](.github/copilot-instructions.md) synchronized with [AGENTS.md](AGENTS.md).
- Added GitHub issue templates (`bug_report.yml`, `config.yml`, `feature_request.yml`) and pull request template (`PULL_REQUEST_TEMPLATE.yml`).

## [1.0.0] - 2026-08-11

- Initial commit.
