#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
USERNAME="${USERNAME:-keruo}"
PASSWORD="${PASSWORD:-admin}"
TARGET_COUNT="${TARGET_COUNT:-2048}"

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
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

get_count() {
  local endpoint="$1"
  local response
  response=$(curl -sS -f \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    "$API_BASE_URL$endpoint?pageSize=1")
  printf '%s' "$response" | jq '.totalItems // 0'
}

create_suppliers() {
  local current
  current=$(get_count '/api/suppliers')
  
  if [[ $current -ge $TARGET_COUNT ]]; then
    echo "Suppliers: already have $current, skipping."
    return
  fi

  local need=$((TARGET_COUNT - current))
  echo "Creating $need suppliers to reach $TARGET_COUNT..."

  local i
  for ((i = current + 1; i <= TARGET_COUNT; i++)); do
    local code="SUP-BULK-$i"
    local name="Bulk Supplier $i"

    local body
    body=$(jq -n --arg code "$code" --arg name "$name" '{code:$code,name:$name}')

    curl -sS -f \
      -H 'Content-Type: application/json' \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -X POST "$API_BASE_URL/api/suppliers" \
      -d "$body" >/dev/null

    if (( i % 100 == 0 )); then
      echo "  Created $i suppliers..."
    fi
  done
}

create_categories() {
  local current
  current=$(get_count '/api/component-categories')
  
  if [[ $current -ge $TARGET_COUNT ]]; then
    echo "Categories: already have $current, skipping."
    return
  fi

  local need=$((TARGET_COUNT - current))
  echo "Creating $need categories to reach $TARGET_COUNT..."

  local i
  for ((i = current + 1; i <= TARGET_COUNT; i++)); do
    local name="Bulk Cat $i"

    local body
    body=$(jq -n --arg name "$name" '{name:$name,description:null,parentId:null}')

    curl -sS -f \
      -H 'Content-Type: application/json' \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -X POST "$API_BASE_URL/api/component-categories" \
      -d "$body" >/dev/null

    if (( i % 100 == 0 )); then
      echo "  Created $i categories..."
    fi
  done
}

create_component_types() {
  local current
  current=$(get_count '/api/component-types')
  
  if [[ $current -ge $TARGET_COUNT ]]; then
    echo "Component Types: already have $current, skipping."
    return
  fi

  echo "Fetching first valid category ID..."
  local cat_id
  cat_id=$(curl -sS -f \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    "$API_BASE_URL/api/component-categories?pageSize=1" | jq -r '.items[0].id // empty')

  if [[ -z "$cat_id" ]]; then
    echo "No categories found. Cannot create component types." >&2
    return
  fi

  local need=$((TARGET_COUNT - current))
  echo "Creating $need component types to reach $TARGET_COUNT..."

  local i
  for ((i = current + 1; i <= TARGET_COUNT; i++)); do
    local body
    body=$(jq -n \
      --arg catId "$cat_id" \
      --arg kind "Bulk$((i % 10))" \
      --arg value "Val$i" \
      '{categoryId:$catId,kind:$kind,value:$value,footprint:null,type:"Other"}')

    curl -sS -f \
      -H 'Content-Type: application/json' \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -X POST "$API_BASE_URL/api/component-types" \
      -d "$body" >/dev/null 2>&1 || true

    if (( i % 100 == 0 )); then
      echo "  Created $i component types..."
    fi
  done
}

create_components() {
  local current
  current=$(get_count '/api/components')
  
  if [[ $current -ge $TARGET_COUNT ]]; then
    echo "Components: already have $current, skipping."
    return
  fi

  echo "Fetching first valid component type ID..."
  local type_id
  type_id=$(curl -sS -f \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    "$API_BASE_URL/api/component-types?pageSize=1" | jq -r '.items[0].id // empty')

  if [[ -z "$type_id" ]]; then
    echo "No component types found. Cannot create components." >&2
    return
  fi

  local need=$((TARGET_COUNT - current))
  echo "Creating $need components to reach $TARGET_COUNT..."

  local i
  for ((i = current + 1; i <= TARGET_COUNT; i++)); do
    local body
    body=$(jq -n \
      --arg typeId "$type_id" \
      --arg partNum "BULK-$i" \
      '{componentTypeId:$typeId,partNumber:$partNum,unitCost:0.01,quantityOnHand:100}')

    curl -sS -f \
      -H 'Content-Type: application/json' \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -X POST "$API_BASE_URL/api/components" \
      -d "$body" >/dev/null 2>&1 || true

    if (( i % 100 == 0 )); then
      echo "  Created $i components..."
    fi
  done
}

create_areas() {
  local current
  current=$(get_count '/api/areas')
  
  if [[ $current -ge $TARGET_COUNT ]]; then
    echo "Areas: already have $current, skipping."
    return
  fi

  local need=$((TARGET_COUNT - current))
  echo "Creating $need areas to reach $TARGET_COUNT..."

  local i
  for ((i = current + 1; i <= TARGET_COUNT; i++)); do
    local body
    body=$(jq -n \
      --arg name "Area $i" \
      --arg code "A$i" \
      '{name:$name,code:$code,zoneType:"Storage",floorLevel:1,description:null}')

    curl -sS -f \
      -H 'Content-Type: application/json' \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -X POST "$API_BASE_URL/api/areas" \
      -d "$body" >/dev/null 2>&1 || true

    if (( i % 100 == 0 )); then
      echo "  Created $i areas..."
    fi
  done
}

create_shelves() {
  local current
  current=$(get_count '/api/shelves')
  
  if [[ $current -ge $TARGET_COUNT ]]; then
    echo "Shelves: already have $current, skipping."
    return
  fi

  echo "Fetching first valid area ID..."
  local area_id
  area_id=$(curl -sS -f \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    "$API_BASE_URL/api/areas?pageSize=1" | jq -r '.items[0].id // empty')

  if [[ -z "$area_id" ]]; then
    echo "No areas found. Cannot create shelves." >&2
    return
  fi

  local need=$((TARGET_COUNT - current))
  echo "Creating $need shelves to reach $TARGET_COUNT..."

  local i
  for ((i = current + 1; i <= TARGET_COUNT; i++)); do
    local body
    body=$(jq -n \
      --arg areaId "$area_id" \
      --arg name "Shelf $i" \
      --arg code "S$i" \
      '{areaId:$areaId,name:$name,code:$code,weightLimitKg:100,description:null}')

    curl -sS -f \
      -H 'Content-Type: application/json' \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -X POST "$API_BASE_URL/api/shelves" \
      -d "$body" >/dev/null 2>&1 || true

    if (( i % 100 == 0 )); then
      echo "  Created $i shelves..."
    fi
  done
}

create_locations() {
  local current
  current=$(get_count '/api/locations')
  
  if [[ $current -ge $TARGET_COUNT ]]; then
    echo "Locations: already have $current, skipping."
    return
  fi

  echo "Fetching shelf IDs for distributing locations..."
  local shelf_ids=()
  local page=1
  
  while true; do
    local response
    response=$(curl -sS -f \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      "$API_BASE_URL/api/shelves?page=$page&pageSize=200")
    
    local count
    count=$(printf '%s' "$response" | jq '.items | length')
    if [[ "$count" -eq 0 ]]; then
      break
    fi

    while IFS= read -r sid; do
      [[ -z "$sid" ]] && continue
      shelf_ids+=("$sid")
    done < <(printf '%s' "$response" | jq -r '.items[] | .id')

    page=$((page + 1))
  done

  if [[ "${#shelf_ids[@]}" -eq 0 ]]; then
    echo "No shelves found. Cannot create locations." >&2
    return
  fi

  local need=$((TARGET_COUNT - current))
  echo "Creating $need locations across ${#shelf_ids[@]} shelves to reach $TARGET_COUNT..."

  local i shelf_idx binx biny
  for ((i = current + 1; i <= TARGET_COUNT; i++)); do
    shelf_idx=$(( (i - 1) % ${#shelf_ids[@]} ))
    local shelf_id="${shelf_ids[$shelf_idx]}"
    
    binx=$(( ((i - 1) / ${#shelf_ids[@]}) % 100 + 1 ))
    biny=$(( ((i - 1) % 32 + 1 )))
    
    local body
    body=$(jq -n \
      --arg shelfId "$shelf_id" \
      --arg name "Loc $i" \
      --arg code "L$i" \
      --argjson binX "$binx" \
      --argjson binY "$biny" \
      '{shelfId:$shelfId,name:$name,code:$code,locationKind:"Warehouse",binX:$binX,binY:$binY,depth:10,width:10,height:10}')

    curl -sS -f \
      -H 'Content-Type: application/json' \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -X POST "$API_BASE_URL/api/locations" \
      -d "$body" >/dev/null 2>&1 || true

    if (( i % 100 == 0 )); then
      echo "  Created $i locations..."
    fi
  done
}

main() {
  require_cmd curl
  require_cmd jq

  echo "Logging in to $API_BASE_URL as $USERNAME..."
  login

  echo "Target count: $TARGET_COUNT"
  echo ""

  create_suppliers
  create_categories
  create_component_types
  create_components
  create_areas
  create_shelves
  create_locations

  echo ""
  echo "=== Final Counts ==="
  echo "Categories: $(get_count '/api/component-categories')"
  echo "Component Types: $(get_count '/api/component-types')"
  echo "Components: $(get_count '/api/components')"
  echo "Suppliers: $(get_count '/api/suppliers')"
  echo "Areas: $(get_count '/api/areas')"
  echo "Shelves: $(get_count '/api/shelves')"
  echo "Locations: $(get_count '/api/locations')"
  echo ""
  echo "Done."
}

main "$@"
