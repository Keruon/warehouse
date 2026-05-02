#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

COMPOSE_DIR="${COMPOSE_DIR:-$REPO_ROOT/docker}"
COMPOSE_FILE="${COMPOSE_FILE:-$COMPOSE_DIR/docker-compose.yml}"
ENV_FILE_DEFAULT="$COMPOSE_DIR/.env"
ENV_FILE_EXAMPLE="$COMPOSE_DIR/.env.example"

NO_CACHE=false
PULL=false
START_AFTER_BUILD=false
ENV_FILE_OVERRIDE=""
SERVICES=()

usage() {
  cat <<'EOF'
Usage: ./scripts/rebuild-docker-images.sh [options] [service ...]

Rebuild docker images defined in the compose file.

Options:
  --no-cache        Build without cache
  --pull            Always attempt to pull newer base images
  --up              Start rebuilt services in detached mode after build
  --env-file PATH   Use a specific compose env file
  -h, --help        Show this help

Examples:
  ./scripts/rebuild-docker-images.sh
  ./scripts/rebuild-docker-images.sh --no-cache
  ./scripts/rebuild-docker-images.sh backend frontend
  ./scripts/rebuild-docker-images.sh --no-cache --up backend
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-cache)
      NO_CACHE=true
      ;;
    --pull)
      PULL=true
      ;;
    --up)
      START_AFTER_BUILD=true
      ;;
    --env-file)
      if [[ $# -lt 2 ]]; then
        echo "Missing value for --env-file" >&2
        exit 1
      fi
      ENV_FILE_OVERRIDE="$2"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    --)
      shift
      while [[ $# -gt 0 ]]; do
        SERVICES+=("$1")
        shift
      done
      break
      ;;
    -*)
      echo "Unknown option: $1" >&2
      usage
      exit 1
      ;;
    *)
      SERVICES+=("$1")
      ;;
  esac
  shift
done

if [[ ! -f "$COMPOSE_FILE" ]]; then
  echo "Compose file not found: $COMPOSE_FILE" >&2
  exit 1
fi

ENV_FILE=""
if [[ -n "$ENV_FILE_OVERRIDE" ]]; then
  ENV_FILE="$ENV_FILE_OVERRIDE"
elif [[ -f "$ENV_FILE_DEFAULT" ]]; then
  ENV_FILE="$ENV_FILE_DEFAULT"
elif [[ -f "$ENV_FILE_EXAMPLE" ]]; then
  ENV_FILE="$ENV_FILE_EXAMPLE"
fi

COMPOSE_CMD=(docker compose -f "$COMPOSE_FILE")
if [[ -n "$ENV_FILE" ]]; then
  COMPOSE_CMD+=(--env-file "$ENV_FILE")
fi

BUILD_ARGS=(build)
if [[ "$NO_CACHE" == true ]]; then
  BUILD_ARGS+=(--no-cache)
fi
if [[ "$PULL" == true ]]; then
  BUILD_ARGS+=(--pull)
fi
if [[ ${#SERVICES[@]} -gt 0 ]]; then
  BUILD_ARGS+=("${SERVICES[@]}")
fi

echo "Rebuilding images with compose file: $COMPOSE_FILE"
if [[ -n "$ENV_FILE" ]]; then
  echo "Using env file: $ENV_FILE"
fi

"${COMPOSE_CMD[@]}" "${BUILD_ARGS[@]}"

if [[ "$START_AFTER_BUILD" == true ]]; then
  UP_ARGS=(up -d)
  if [[ ${#SERVICES[@]} -gt 0 ]]; then
    UP_ARGS+=("${SERVICES[@]}")
  fi

  echo "Starting services after rebuild..."
  "${COMPOSE_CMD[@]}" "${UP_ARGS[@]}"
fi

echo "Done."
