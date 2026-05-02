#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
USERNAME="${USERNAME:-admin}"
PASSWORD="${PASSWORD:-admin}"
CATEGORY_NAME="${CATEGORY_NAME:-Through Hole Resistors}"
FOOTPRINT="${FOOTPRINT:-TH Footprint}"
PACKAGE_TYPE="${PACKAGE_TYPE:-ThroughHole}"
PAGE_SIZE="${PAGE_SIZE:-200}"
DRY_RUN="${DRY_RUN:-false}"

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <input_file>"
  echo "Example: $0 ../utils/resistors.csv"
  exit 1
fi

INPUT_FILE="$1"

ACCESS_TOKEN=""
CATEGORY_ID=""

require_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "Missing required command: $cmd" >&2
    exit 1
  fi
}

trim() {
  local s="$1"
  s="${s%$'\r'}"
  s="${s#"${s%%[![:space:]]*}"}"
  s="${s%"${s##*[![:space:]]}"}"
  printf '%s' "$s"
}

login() {
  local body
  body=$(jq -n --arg u "$USERNAME" --arg p "$PASSWORD" '{usernameOrEmail:$u,password:$p}')

  local response
  response=$(curl -sS -f \
    -H 'Content-Type: application/json' \
    -X POST "$API_BASE_URL/api/auth/login" \
    -d "$body")

  ACCESS_TOKEN=$(printf '%s' "$response" | jq -r '.data.tokens.accessToken // .tokens.accessToken // empty')
  if [[ -z "$ACCESS_TOKEN" ]]; then
    echo "Login succeeded but access token was missing." >&2
    exit 1
  fi
}

resolve_category_id() {
  local encoded_name
  encoded_name=$(jq -rn --arg v "$CATEGORY_NAME" '$v|@uri')

  local page=1
  while true; do
    local response
    response=$(curl -sS -f \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      "$API_BASE_URL/api/component-categories?search=$encoded_name&page=$page&pageSize=$PAGE_SIZE&isActive=true")

    local count
    count=$(printf '%s' "$response" | jq '.items | length')
    if [[ "$count" -eq 0 ]]; then
      break
    fi

    local exact_id
    exact_id=$(printf '%s' "$response" | jq -r --arg name "$CATEGORY_NAME" '.items[] | select(.name == $name) | .id' | head -n 1)
    if [[ -n "$exact_id" ]]; then
      CATEGORY_ID="$exact_id"
      return
    fi

    page=$((page + 1))
  done

  echo "Category '$CATEGORY_NAME' was not found. Seed categories first or set CATEGORY_NAME." >&2
  exit 1
}

load_existing_keys() {
  local page=1

  while true; do
    local response
    response=$(curl -sS -f \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      "$API_BASE_URL/api/component-types?categoryId=$CATEGORY_ID&page=$page&pageSize=$PAGE_SIZE&isActive=true")

    local count
    count=$(printf '%s' "$response" | jq '.items | length')
    if [[ "$count" -eq 0 ]]; then
      break
    fi

    while IFS=$'\t' read -r kind value footprint; do
      [[ -z "$kind" && -z "$value" ]] && continue
      EXISTING_KEYS["$kind|$value|$footprint"]=1
    done < <(printf '%s' "$response" | jq -r '.items[] | [(.kind // ""), (.value // ""), (.footprint // "")] | @tsv')

    page=$((page + 1))
  done
}

create_component_type() {
  local kind="$1"
  local value="$2"
  local footprint="$3"

  local key="$kind|$value|$footprint"
  if [[ -n "${EXISTING_KEYS[$key]:-}" ]]; then
    echo "Skipping existing: $kind / $value / $footprint"
    skipped_count=$((skipped_count + 1))
    return
  fi

  if [[ "$DRY_RUN" == "true" ]]; then
    echo "Dry run: would create $kind / $value / $footprint"
    created_count=$((created_count + 1))
    EXISTING_KEYS["$key"]=1
    return
  fi

  local body
  body=$(jq -n \
    --arg categoryId "$CATEGORY_ID" \
    --arg kind "$kind" \
    --arg value "$value" \
    --arg footprint "$footprint" \
    --arg type "$PACKAGE_TYPE" \
    '{categoryId:$categoryId,kind:$kind,value:$value,footprint:$footprint,type:$type,description:null}')

  local response_file
  response_file=$(mktemp)
  local status_code
  status_code=$(curl -sS \
    -o "$response_file" \
    -w '%{http_code}' \
    -H 'Content-Type: application/json' \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -X POST "$API_BASE_URL/api/component-types" \
    -d "$body")

  if [[ "$status_code" == "201" ]]; then
    created_count=$((created_count + 1))
    EXISTING_KEYS["$key"]=1
  elif [[ "$status_code" == "409" ]]; then
    echo "Skipping duplicate from API: $kind / $value / $footprint"
    skipped_count=$((skipped_count + 1))
    EXISTING_KEYS["$key"]=1
  else
    echo "Failed to create '$kind / $value / $footprint' (HTTP $status_code)." >&2
    cat "$response_file" >&2
    rm -f "$response_file"
    exit 1
  fi

  rm -f "$response_file"
}

seed_from_file() {
  local header_skipped=false

  while IFS=$'\t' read -r raw_type raw_value _raw_ohm raw_accuracy raw_power || [[ -n "$raw_type$raw_value$raw_accuracy$raw_power" ]]; do
    local type
    local value
    local accuracy
    local power
    type=$(trim "$raw_type")
    value=$(trim "$raw_value")
    accuracy=$(trim "$raw_accuracy")
    power=$(trim "$raw_power")

    if [[ -z "$type" && -z "$value" && -z "$accuracy" && -z "$power" ]]; then
      continue
    fi

    if [[ "$header_skipped" == false ]]; then
      header_skipped=true
      continue
    fi

    [[ -z "$type" && -z "$value" ]] && continue

    local kind
    kind=$(trim "$type $accuracy $power")
    create_component_type "$kind" "$value" "$FOOTPRINT"
  done < "$INPUT_FILE"
}

main() {
  require_cmd curl
  require_cmd jq

  if [[ ! -f "$INPUT_FILE" ]]; then
    echo "Input file not found: $INPUT_FILE" >&2
    exit 1
  fi

  declare -gA EXISTING_KEYS=()
  declare -g created_count=0
  declare -g skipped_count=0

  echo "Logging in to $API_BASE_URL as $USERNAME..."
  login

  echo "Resolving category '$CATEGORY_NAME'..."
  resolve_category_id
  echo "Using category id: $CATEGORY_ID"

  echo "Loading existing key attributes in category..."
  load_existing_keys

  echo "Seeding key attributes from $INPUT_FILE..."
  seed_from_file

  echo ""
  if [[ "$DRY_RUN" == "true" ]]; then
    echo "Dry run complete. $created_count rows would be created, $skipped_count would be skipped."
  else
    echo "Seed complete. Created $created_count key attributes, skipped $skipped_count existing rows."
  fi
}

main "$@"