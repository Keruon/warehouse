#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
USERNAME="${USERNAME:-keruo}"
PASSWORD="${PASSWORD:-admin}"
PAGE_SIZE="${PAGE_SIZE:-200}"
DRY_RUN="${DRY_RUN:-false}"

ACCESS_TOKEN=""

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
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
    echo "Login failed." >&2
    exit 1
  fi
}

api_get() {
  local path="$1"
  curl -sS -f \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    "$API_BASE_URL$path"
}

api_delete() {
  local path="$1"
  curl -sS -f \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -X DELETE \
    "$API_BASE_URL$path" >/dev/null
}

collect_matching_ids() {
  local endpoint="$1"
  local jq_filter="$2"
  local page=1

  while true; do
    local response
    response=$(api_get "$endpoint?page=$page&pageSize=$PAGE_SIZE")

    local count
    count=$(printf '%s' "$response" | jq '.items | length')
    if [[ "$count" -eq 0 ]]; then
      break
    fi

    printf '%s' "$response" | jq -r "$jq_filter"
    page=$((page + 1))
  done
}

delete_ids() {
  local label="$1"
  local endpoint="$2"
  shift 2
  local ids=("$@")

  if [[ ${#ids[@]} -eq 0 ]]; then
    echo "$label: no matching bulk records found."
    return
  fi

  echo "$label: ${#ids[@]} matching records found."

  if [[ "$DRY_RUN" == "true" ]]; then
    local preview_count=${#ids[@]}
    if (( preview_count > 5 )); then
      preview_count=5
    fi

    echo "  Dry run only. First $preview_count ids:"
    printf '  %s\n' "${ids[@]:0:$preview_count}"
    return
  fi

  local id
  local deleted=0
  for id in "${ids[@]}"; do
    api_delete "$endpoint/$id"
    deleted=$((deleted + 1))

    if (( deleted % 100 == 0 )); then
      echo "  Deleted $deleted $label..."
    fi
  done

  echo "  Deleted $deleted $label."
}

main() {
  require_cmd curl
  require_cmd jq

  echo "Logging in to $API_BASE_URL as $USERNAME..."
  login
  echo "Dry run: $DRY_RUN"
  echo ""

  mapfile -t location_ids < <(collect_matching_ids "/api/locations" '.items[] | select((.name // "") | test("^Loc [0-9]+$")) | select((.code // "") | test("^L[0-9]+$")) | .id')
  delete_ids "locations" "/api/locations" "${location_ids[@]}"

  mapfile -t shelf_ids < <(collect_matching_ids "/api/shelves" '.items[] | select((.name // "") | test("^Shelf [0-9]+$")) | select((.code // "") | test("^S[0-9]+$")) | .id')
  delete_ids "shelves" "/api/shelves" "${shelf_ids[@]}"

  mapfile -t area_ids < <(collect_matching_ids "/api/areas" '.items[] | select((.name // "") | test("^Area [0-9]+$")) | select((.code // "") | test("^A[0-9]+$")) | .id')
  delete_ids "areas" "/api/areas" "${area_ids[@]}"

  mapfile -t component_ids < <(collect_matching_ids "/api/components" '.items[] | select((.partNumber // "") | startswith("BULK-")) | .id')
  delete_ids "components" "/api/components" "${component_ids[@]}"

  mapfile -t component_type_ids < <(collect_matching_ids "/api/component-types" '.items[] | select((.kind // "") | startswith("Bulk")) | select((.value // "") | startswith("Val")) | .id')
  delete_ids "component types" "/api/component-types" "${component_type_ids[@]}"

  mapfile -t category_ids < <(collect_matching_ids "/api/component-categories" '.items[] | select((.name // "") | startswith("Bulk Cat ")) | .id')
  delete_ids "categories" "/api/component-categories" "${category_ids[@]}"

  mapfile -t supplier_ids < <(collect_matching_ids "/api/suppliers" '.items[] | select((.code // "") | startswith("SUP-BULK-")) | .id')
  delete_ids "suppliers" "/api/suppliers" "${supplier_ids[@]}"

  echo ""
  if [[ "$DRY_RUN" == "true" ]]; then
    echo "Dry run complete. No records were deleted."
  else
    echo "Bulk test data cleanup complete."
  fi
}

main "$@"