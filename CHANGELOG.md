# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Added [swag/www/robots.txt](swag/www/robots.txt) configured to disallow all web crawlers and automated scrapers across the domain.

### Changed
- Configured `INVITATIONS_ALLOWED=false` for `vaultwarden` in [docker-compose.yaml](docker-compose.yaml) to ensure `disableUserRegistration` evaluates to `true` and the "Create account" button is hidden from the web vault landing page when SMTP mail is disabled.

## [1.5.1] - 2026-11-03

### Added
- Added open-source audit notices and GitHub repository links (`FesterHead/fester-server/tree/main/challenge-recorder`) to [swag/www/maintenance/fairness.html](swag/www/maintenance/fairness.html) (in the top navigation bar and a dedicated auditable service section) and all maintenance challenge login pages (`2026-11-03`, `2026-12-15`, `2027-01-05`, `2027-03-02`).

### Changed
- Clarified authentication and fair-play disclosures across maintenance challenge login pages to highlight direct browser authentication with `disasteraware.pdc.org` and verifiable zero credential transit.

## [1.5.0] - 2026-11-03

### Added
- Added persistent rotating file logging to `/app/data/logs/challenge-recorder.log` (host directory `challenge-recorder/data/logs/`) with Hawaii Standard Time (`HST`) formatting across [challenge-recorder/main.py](challenge-recorder/main.py), [challenge-recorder/winner.py](challenge-recorder/winner.py), and [challenge-recorder/reset.py](challenge-recorder/reset.py), auditing HTTP requests, riddle queries, puzzle completion rankings, winner minting, and database resets.
- Added date-gated riddle unlocking endpoint (`/api/riddle`) to `challenge-recorder` supporting progressive daily challenge schedules and holding screens.
- Added `challenge-recorder` FastAPI service to [docker-compose.yaml](docker-compose.yaml), [update.sh](update.sh), [.gitignore](.gitignore), and [README.md](README.md) with SWAG reverse-proxy routing (`/api/`) for DisasterAWARE Pro JWT verification, cryptographic challenge proof validation, SQLite leaderboard persistence, and a database reset utility ([challenge-recorder/reset.py](challenge-recorder/reset.py)).
- Added `forgejo` (LTS v15) and `forgejo-runner` (v13) services to [docker-compose.yaml](docker-compose.yaml) with container monitoring labels, DNS resolution, healthchecks, and environment configurations (`PORT_FORGEJO_HTTP`, `PORT_FORGEJO_SSH`, `FORGEJO_RUNNER_TOKEN`, `DOCKER_GID`) in [.env](.env) and [.env-template](.env-template).
- Added `forgejo` and `forgejo-runner` to [update.sh](update.sh), [.gitignore](.gitignore), and [README.md](README.md).
- Added `swag/www/` static web content (excluding `swag/www/maintenance/`) to repository tracking via [.gitignore](.gitignore) and modernized [swag/www/rivergauge.html](swag/www/rivergauge.html), [swag/www/index.html](swag/www/index.html), [swag/www/festerhead/index.html](swag/www/festerhead/index.html), and [swag/www/wordle/index.html](swag/www/wordle/index.html) (including Rush Wordle customizations and Double Secret Probation mode).

### Removed
- Removed `/api/auth/login` proxy endpoint from [challenge-recorder/main.py](challenge-recorder/main.py) and eliminated proxy fallback in frontend client scripts across all maintenance challenges, enforcing direct-only browser authentication against `https://disasteraware.pdc.org/jwt/v1/auth/login` so credentials never touch the challenge stack.

## [1.4.0] - 2026-09-30

### Added
- Added `vaultwarden` and `vaultwarden-backup` services to [docker-compose.yaml](docker-compose.yaml) providing self-hosted Bitwarden-compatible password management and automated 7-Zip encrypted backups to local NAS storage (`${DATA_DIR}/vaultwarden`) via rclone and supercronic, with healthchecks, DNS resolution, and parameterized domain configuration (`VW_URL`) and backup passphrase (`VW_BACKUP_ZIP_PW`) in [.env](.env) and [.env-template](.env-template).
- Added `vaultwarden` and `vaultwarden-backup` to [update.sh](update.sh) and [README.md](README.md), including instructions for on-demand manual backup execution.

### Removed
- Extracted the `servuo` service from [docker-compose.yaml](docker-compose.yaml) into its own dedicated standalone repository ([FesterUO](https://github.com/FesterHead/FesterUO)), running as an independent Compose stack on the same host environment.
- Removed `servuo` service entries, update hooks, port variables, and documentation from [update.sh](update.sh), [README.md](README.md), [.env](.env), and [.env-template](.env-template).

## [1.3.0] - 2026-09-16

### Changed
- Configured `seerr` with `user: "${PUID}:${PGID}"` and `crosswatch` with `APP_UID` and `APP_GID` in `docker-compose.yaml` to run containers with standard host file ownership.

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
