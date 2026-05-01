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

docker exec -i "$CONTAINER_NAME" psql \
  -v ON_ERROR_STOP=1 \
  -v username="$USERNAME" \
  -v email="$EMAIL" \
  -v password="$PASSWORD" \
  -v actor_id="$ACTOR_ID" \
  -U "$DB_USER" \
  -d "$DB_NAME" <<'SQL'
CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
BEGIN
  IF to_regclass('"Users"') IS NULL THEN
    CREATE TABLE "Users" (
      "Id" uuid PRIMARY KEY,
      "Username" character varying(100) NOT NULL,
      "Email" character varying(256) NOT NULL,
      "PasswordHash" text NOT NULL,
      "Role" text NOT NULL,
      "FirstName" character varying(100),
      "LastName" character varying(100),
      "LastLoginAt" timestamp with time zone,
      "IsActive" boolean NOT NULL DEFAULT true,
      "CreatedAt" timestamp with time zone NOT NULL,
      "CreatedBy" uuid NOT NULL,
      "ModifiedAt" timestamp with time zone NOT NULL,
      "ModifiedBy" uuid NOT NULL
    );

    CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
  END IF;

  IF to_regclass('"RefreshTokens"') IS NULL THEN
    CREATE TABLE "RefreshTokens" (
      "Id" uuid PRIMARY KEY,
      "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
      "Token" text NOT NULL,
      "DeviceFingerprint" character varying(256),
      "ExpiresAt" timestamp with time zone NOT NULL,
      "CreatedAt" timestamp with time zone NOT NULL,
      "RevokedAt" timestamp with time zone,
      "IsRevoked" boolean NOT NULL DEFAULT false
    );

    CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");
    CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
  END IF;
END;
$$;

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
  :'username',
  :'email',
  crypt(:'password', gen_salt('bf', 12)),
  'Admin',
  'System',
  'Admin',
  true,
  NOW(),
  :'actor_id',
  NOW(),
  :'actor_id'
)
ON CONFLICT ("Username")
DO UPDATE SET
  "Email" = EXCLUDED."Email",
  "PasswordHash" = EXCLUDED."PasswordHash",
  "Role" = 'Admin',
  "IsActive" = true,
  "ModifiedAt" = NOW(),
  "ModifiedBy" = :'actor_id';
SQL

echo "Done. Login with username='$USERNAME' and password='$PASSWORD'."
