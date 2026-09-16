#!/usr/bin/env bash
# ==============================================================================
# ServUO Patch & Upstream Management Helper
# ==============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UPSTREAM_DIR="${SCRIPT_DIR}/upstream"
PATCHES_DIR="${SCRIPT_DIR}/patches"
DOCKERFILE="${SCRIPT_DIR}/Dockerfile"

# Configuration (overridable via environment)
UPSTREAM_BRANCH="${SERVUO_BRANCH:-pub57}"
UPSTREAM_REPO="${SERVUO_REPO:-https://github.com/ServUO/ServUO.git}"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

usage() {
    cat <<EOF
Usage: $(basename "$0") <command> [arguments]

Commands:
  status            Show upstream commit hash vs. Dockerfile pinned commit
  check             Verify all patches in patches/*.patch apply cleanly against upstream
  export <name>     Export current git diff from upstream into patches/<name>.patch
  apply             Apply all patches in patches/ to the local upstream directory
  reset             Discard any changes in upstream and restore it to stock
  update            Fetch latest ${UPSTREAM_BRANCH}, check patch compatibility, and report/update commit
EOF
    exit 1
}

ensure_upstream() {
    if [[ ! -d "${UPSTREAM_DIR}/.git" ]]; then
        echo -e "${RED}Error: ${UPSTREAM_DIR} is not a valid git repository.${NC}"
        echo "Please clone it with: git clone --depth 1 --branch ${UPSTREAM_BRANCH} ${UPSTREAM_REPO} \"${UPSTREAM_DIR}\""
        exit 1
    fi
}

cmd_status() {
    ensure_upstream
    local upstream_commit
    upstream_commit=$(git -C "${UPSTREAM_DIR}" rev-parse HEAD)
    local dockerfile_commit
    dockerfile_commit=$(grep -E '^ARG SERVUO_COMMIT=' "${DOCKERFILE}" | cut -d'=' -f2)

    echo -e "${BLUE}=== ServUO Commit Status ===${NC}"
    echo -e "Upstream local clone commit : ${GREEN}${upstream_commit}${NC}"
    echo -e "Dockerfile pinned commit    : ${GREEN}${dockerfile_commit}${NC}"

    if [[ "${upstream_commit}" == "${dockerfile_commit}" ]]; then
        echo -e "${GREEN}✓ Local upstream and Dockerfile commits match.${NC}"
    else
        echo -e "${YELLOW}⚠ Local upstream and Dockerfile commits differ!${NC}"
    fi

    echo ""
    echo -e "${BLUE}=== Uncommitted Changes in Upstream ===${NC}"
    local dirty
    dirty=$(git -C "${UPSTREAM_DIR}" status --short)
    if [[ -z "${dirty}" ]]; then
        echo "Working tree is clean."
    else
        echo -e "${YELLOW}${dirty}${NC}"
    fi
}

cmd_check() {
    ensure_upstream
    echo -e "${BLUE}Checking patch applicability against upstream...${NC}"

    local failed=0
    for patch in "${PATCHES_DIR}"/*.patch; do
        [[ -e "$patch" ]] || continue
        local patch_name
        patch_name=$(basename "$patch")
        if git -C "${UPSTREAM_DIR}" apply --check "$patch" 2>/dev/null; then
            echo -e "  [${GREEN}PASS - READY${NC}] ${patch_name}"
        elif git -C "${UPSTREAM_DIR}" apply --reverse --check "$patch" 2>/dev/null; then
            echo -e "  [${YELLOW}PASS - ALREADY APPLIED${NC}] ${patch_name}"
        else
            echo -e "  [${RED}FAIL${NC}] ${patch_name}"
            failed=$((failed + 1))
        fi
    done

    if [[ $failed -eq 0 ]]; then
        echo -e "${GREEN}All patches verified!${NC}"
    else
        echo -e "${RED}${failed} patch(es) failed to apply.${NC}"
        exit 1
    fi
}

cmd_apply() {
    ensure_upstream
    echo -e "${BLUE}Applying patches to upstream directory...${NC}"
    local applied_count=0
    local skipped_count=0
    local failed_count=0

    for patch in "${PATCHES_DIR}"/*.patch; do
        [[ -e "$patch" ]] || continue
        local patch_name
        patch_name=$(basename "$patch")

        if git -C "${UPSTREAM_DIR}" apply --check "$patch" 2>/dev/null; then
            git -C "${UPSTREAM_DIR}" apply "$patch"
            echo -e "  [${GREEN}APPLIED${NC}] ${patch_name}"
            applied_count=$((applied_count + 1))
        elif git -C "${UPSTREAM_DIR}" apply --reverse --check "$patch" 2>/dev/null; then
            echo -e "  [${YELLOW}SKIPPED - ALREADY APPLIED${NC}] ${patch_name}"
            skipped_count=$((skipped_count + 1))
        else
            echo -e "  [${RED}FAIL - CONFLICT${NC}] ${patch_name}"
            failed_count=$((failed_count + 1))
        fi
    done

    if [[ $failed_count -gt 0 ]]; then
        echo -e "${RED}${failed_count} patch(es) could not be applied due to conflicts.${NC}"
        exit 1
    fi

    echo -e "${GREEN}Done. (${applied_count} applied, ${skipped_count} already present)${NC}"
}

cmd_reset() {
    ensure_upstream
    echo -e "${YELLOW}Discarding all local modifications in upstream directory...${NC}"
    git -C "${UPSTREAM_DIR}" checkout .
    git -C "${UPSTREAM_DIR}" clean -fd
    echo -e "${GREEN}Upstream directory restored to clean stock state.${NC}"
}

cmd_export() {
    ensure_upstream
    local name="${1:-}"
    if [[ -z "$name" ]]; then
        echo -e "${RED}Error: Please specify a patch name.${NC}"
        echo "Example: $(basename "$0") export 07-custom-feature"
        exit 1
    fi

    # Ensure .patch suffix
    [[ "$name" != *.patch ]] && name="${name}.patch"
    local dest="${PATCHES_DIR}/${name}"

    local diff_output
    diff_output=$(git -C "${UPSTREAM_DIR}" diff)

    if [[ -z "${diff_output}" ]]; then
        echo -e "${RED}Error: No uncommitted changes detected in ${UPSTREAM_DIR}.${NC}"
        exit 1
    fi

    echo "$diff_output" > "$dest"
    echo -e "${GREEN}Created patch: ${dest}${NC}"
    echo -e "Verify it with: $(basename "$0") check"
}

cmd_update() {
    ensure_upstream
    echo -e "${BLUE}Fetching latest changes from origin ${UPSTREAM_BRANCH}...${NC}"
    git -C "${UPSTREAM_DIR}" fetch origin "${UPSTREAM_BRANCH}"
    local latest_remote
    latest_remote=$(git -C "${UPSTREAM_DIR}" rev-parse "origin/${UPSTREAM_BRANCH}")
    local current_local
    current_local=$(git -C "${UPSTREAM_DIR}" rev-parse HEAD)

    if [[ "${latest_remote}" == "${current_local}" ]]; then
        echo -e "${GREEN}Local upstream is already up to date with origin/${UPSTREAM_BRANCH} (${current_local}).${NC}"
        return
    fi

    echo -e "New upstream commit found: ${YELLOW}${latest_remote}${NC}"
    read -rp "Checkout new commit and test patches? [y/N] " confirm
    if [[ "${confirm}" =~ ^[Yy]$ ]]; then
        git -C "${UPSTREAM_DIR}" checkout "${latest_remote}"
        echo -e "${BLUE}Testing existing patches against new commit...${NC}"
        if git -C "${UPSTREAM_DIR}" apply --check "${PATCHES_DIR}"/*.patch; then
            echo -e "${GREEN}All patches apply cleanly to the new commit!${NC}"
            echo -e "Updating ARG SERVUO_COMMIT in Dockerfile..."
            sed -i "s/^ARG SERVUO_COMMIT=.*/ARG SERVUO_COMMIT=${latest_remote}/" "${DOCKERFILE}"
            echo -e "${GREEN}Dockerfile updated to ${latest_remote}.${NC}"
        else
            echo -e "${RED}One or more patches failed to apply against new commit.${NC}"
            echo -e "Run '$(basename "$0") check' to see which patch needs updating."
        fi
    fi
}

# Main routing
case "${1:-}" in
    status)
        cmd_status
        ;;
    check)
        cmd_check
        ;;
    apply)
        cmd_apply
        ;;
    reset)
        cmd_reset
        ;;
    export)
        cmd_export "${2:-}"
        ;;
    update)
        cmd_update
        ;;
    *)
        usage
        ;;
esac
