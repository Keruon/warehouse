#!/usr/bin/env bash
set -euo pipefail

CONTAINER_NAME="${CONTAINER_NAME:-storage-postgres}"
DB_NAME="${DB_NAME:-storage}"
DB_USER="${DB_USER:-storage}"
USERNAME="${USERNAME:-admin}"
EMAIL="${EMAIL:-admin@local.dev}"
PASSWORD="${PASSWORD:-admin}"

# Fixed actor ID used for CreatedBy / ModifiedBy auditing fields.
ACTOR_ID="00000000-0000-0000-0000-000000000001"

if ! docker ps --format '{{.Names}}' | grep -qx "$CONTAINER_NAME"; then
  echo "Container '$CONTAINER_NAME' is not running."
  echo "Start the stack first, e.g.: docker compose -f docker/docker-compose.yml --env-file docker/.env.example up -d"
  exit 1
fi

echo "Creating or updating admin user '$USERNAME' in database '$DB_NAME'..."

docker exec -i "$CONTAINER_NAME" psql -v ON_ERROR_STOP=1 -U "$DB_USER" -d "$DB_NAME" <<SQL
CREATE EXTENSION IF NOT EXISTS pgcrypto;

INSERT INTO "Users" (
  "Id",
  "Username",
  "Email",
  "PasswordHash",
  "Role",
  "FirstName",
  "LastName",
  "IsActive",
  "CreatedAt",
  "CreatedBy",
  "ModifiedAt",
  "ModifiedBy"
)
VALUES (
  gen_random_uuid(),
  '${USERNAME}',
  '${EMAIL}',
  crypt('${PASSWORD}', gen_salt('bf', 12)),
  'Admin',
  'System',
  'Admin',
  true,
  NOW(),
  '${ACTOR_ID}',
  NOW(),
  '${ACTOR_ID}'
)
ON CONFLICT ("Username")
DO UPDATE SET
  "Email" = EXCLUDED."Email",
  "PasswordHash" = EXCLUDED."PasswordHash",
  "Role" = 'Admin',
  "IsActive" = true,
  "ModifiedAt" = NOW(),
  "ModifiedBy" = '${ACTOR_ID}';
SQL

echo "Done. Login with username='$USERNAME' and password='$PASSWORD'."
