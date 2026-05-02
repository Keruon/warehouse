#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
TEMPLATE_FILE="${TEMPLATE_FILE:-/home/keruo/storage/utils/cat.md}"
USERNAME="${USERNAME:-admin}"
PASSWORD="${PASSWORD:-admin}"
PAGE_SIZE="${PAGE_SIZE:-200}"

require_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "Missing required command: $cmd" >&2
    exit 1
  fi
}

trim() {
  local s="$1"
  s="${s#"${s%%[![:space:]]*}"}"
  s="${s%"${s##*[![:space:]]}"}"
  printf '%s' "$s"
}

login() {
  local login_body
  login_body=$(jq -n --arg u "$USERNAME" --arg p "$PASSWORD" '{usernameOrEmail:$u,password:$p}')

  local response
  response=$(curl -sS -f \
    -H 'Content-Type: application/json' \
    -X POST "$API_BASE_URL/api/auth/login" \
    -d "$login_body")

  ACCESS_TOKEN=$(printf '%s' "$response" | jq -r '.tokens.accessToken // .data.tokens.accessToken // empty')
  if [[ -z "$ACCESS_TOKEN" ]]; then
    echo "Login succeeded but access token was missing." >&2
    exit 1
  fi
}

fetch_all_active_categories() {
  local page=1
  local ids=()
  local parent_ids=()

  while true; do
    local response
    response=$(curl -sS -f \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      "$API_BASE_URL/api/component-categories?page=$page&pageSize=$PAGE_SIZE&isActive=true")

    local count
    count=$(printf '%s' "$response" | jq '.items | length')
    if [[ "$count" -eq 0 ]]; then
      break
    fi

    while IFS=$'\t' read -r cid pid; do
      [[ -z "$cid" ]] && continue
      ids+=("$cid")
      parent_ids+=("$pid")
    done < <(printf '%s' "$response" | jq -r '.items[] | [.id, (.parentId // "")] | @tsv')

    page=$((page + 1))
  done

  CATEGORY_IDS=("${ids[@]}")
  CATEGORY_PARENT_IDS=("${parent_ids[@]}")
}

fetch_all_active_components() {
  local page=1
  local ids=()

  while true; do
    local response
    response=$(curl -sS -f \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      "$API_BASE_URL/api/components?page=$page&pageSize=$PAGE_SIZE")

    local count
    count=$(printf '%s' "$response" | jq '.items | length')
    if [[ "$count" -eq 0 ]]; then
      break
    fi

    while IFS= read -r cid; do
      [[ -z "$cid" ]] && continue
      ids+=("$cid")
    done < <(printf '%s' "$response" | jq -r '.items[] | .id')

    page=$((page + 1))
  done

  COMPONENT_IDS=("${ids[@]}")
}

delete_existing_components() {
  fetch_all_active_components

  if [[ "${#COMPONENT_IDS[@]}" -eq 0 ]]; then
    echo "No active components to delete."
    return
  fi

  local cid
  for cid in "${COMPONENT_IDS[@]}"; do
    curl -sS -f \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -X DELETE "$API_BASE_URL/api/components/$cid" >/dev/null
  done

  echo "Deleted ${#COMPONENT_IDS[@]} active components."
}

fetch_all_active_component_types() {
  local page=1
  local ids=()

  while true; do
    local response
    response=$(curl -sS -f \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      "$API_BASE_URL/api/component-types?page=$page&pageSize=$PAGE_SIZE&isActive=true")

    local count
    count=$(printf '%s' "$response" | jq '.items | length')
    if [[ "$count" -eq 0 ]]; then
      break
    fi

    while IFS= read -r tid; do
      [[ -z "$tid" ]] && continue
      ids+=("$tid")
    done < <(printf '%s' "$response" | jq -r '.items[] | .id')

    page=$((page + 1))
  done

  COMPONENT_TYPE_IDS=("${ids[@]}")
}

delete_existing_component_types() {
  fetch_all_active_component_types

  if [[ "${#COMPONENT_TYPE_IDS[@]}" -eq 0 ]]; then
    echo "No active component types to delete."
    return
  fi

  local tid
  for tid in "${COMPONENT_TYPE_IDS[@]}"; do
    curl -sS -f \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -X DELETE "$API_BASE_URL/api/component-types/$tid" >/dev/null
  done

  echo "Deleted ${#COMPONENT_TYPE_IDS[@]} active component types."
}

delete_existing_categories() {
  local rounds=0

  while true; do
    fetch_all_active_categories

    if [[ "${#CATEGORY_IDS[@]}" -eq 0 ]]; then
      echo "No active categories to delete."
      break
    fi

    local leaves=()
    local idx

    for idx in "${!CATEGORY_IDS[@]}"; do
      local id="${CATEGORY_IDS[$idx]}"
      local has_child=false
      local pidx

      for pidx in "${!CATEGORY_PARENT_IDS[@]}"; do
        if [[ "${CATEGORY_PARENT_IDS[$pidx]}" == "$id" ]]; then
          has_child=true
          break
        fi
      done

      if [[ "$has_child" == false ]]; then
        leaves+=("$id")
      fi
    done

    if [[ "${#leaves[@]}" -eq 0 ]]; then
      echo "Unable to find leaf categories to delete. Check for API-side constraints." >&2
      exit 1
    fi

    for cid in "${leaves[@]}"; do
      curl -sS -f \
        -H "Authorization: Bearer $ACCESS_TOKEN" \
        -X DELETE "$API_BASE_URL/api/component-categories/$cid" >/dev/null
    done

    rounds=$((rounds + 1))
    if [[ "$rounds" -gt 200 ]]; then
      echo "Delete loop exceeded safety limit." >&2
      exit 1
    fi
  done
}

create_category() {
  local name="$1"
  local parent_id="${2:-}"
  local body

  if [[ -n "$parent_id" ]]; then
    body=$(jq -n --arg name "$name" --arg parentId "$parent_id" '{name:$name,parentId:$parentId,description:null}')
  else
    body=$(jq -n --arg name "$name" '{name:$name,parentId:null,description:null}')
  fi

  local response
  response=$(curl -sS -f \
    -H 'Content-Type: application/json' \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -X POST "$API_BASE_URL/api/component-categories" \
    -d "$body")

  local created_id
  created_id=$(printf '%s' "$response" | jq -r '.id // empty')
  if [[ -z "$created_id" ]]; then
    echo "Failed to parse created category id for '$name'." >&2
    exit 1
  fi

  printf '%s' "$created_id"
}

seed_from_template() {
  local current_root_id=""
  local current_first_child_id=""

  while IFS= read -r raw_line || [[ -n "$raw_line" ]]; do
    local line
    line=$(trim "$raw_line")

    [[ -z "$line" ]] && continue

    if [[ "$line" == '## '* ]]; then
      local root_name
      root_name=$(trim "${line#\#\# }")
      [[ -z "$root_name" ]] && continue

      current_root_id=$(create_category "$root_name")
      current_first_child_id=""
      echo "Created root: $root_name"
      continue
    fi

    if [[ "$line" == '- '* ]]; then
      local child_name
      child_name=$(trim "${line#- }")
      [[ -z "$child_name" ]] && continue

      if [[ -z "$current_first_child_id" ]]; then
        echo "Template error: found grandchild '$child_name' without a first-level parent." >&2
        exit 1
      fi

      create_category "$child_name" "$current_first_child_id" >/dev/null
      continue
    fi

    if [[ -z "$current_root_id" ]]; then
      echo "Template error: found child '$line' before any root header (## ...)." >&2
      exit 1
    fi

    current_first_child_id=$(create_category "$line" "$current_root_id")
  done < "$TEMPLATE_FILE"
}

main() {
  require_cmd curl
  require_cmd jq

  if [[ ! -f "$TEMPLATE_FILE" ]]; then
    echo "Template file not found: $TEMPLATE_FILE" >&2
    exit 1
  fi

  echo "Logging in to $API_BASE_URL as $USERNAME..."
  login

  echo "Deleting existing active components..."
  delete_existing_components

  echo "Deleting existing active component types..."
  delete_existing_component_types

  echo "Deleting existing active categories..."
  delete_existing_categories

  echo "Seeding categories from $TEMPLATE_FILE..."
  seed_from_template

  echo "Done. Categories seeded successfully."
}

main "$@"
