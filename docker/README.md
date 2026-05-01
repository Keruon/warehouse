# Docker Development Setup

This folder contains Docker Compose setup for local development of the Storage stack.

Services:

- `postgres` (`postgres:16-alpine`)
- `backend` (ASP.NET Core 8)
- `frontend` (React + TypeScript)

## Current state

- The backend project file exists and is built from `backend/backend.csproj`.
- Database bootstrap SQL is mounted from `backend/archive/create_db.sql`.
- Compose startup has been verified with all three containers running.

## Start the stack

From repository root:

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env.example up --build -d
```

Or from this `docker/` folder:

```bash
cp .env.example .env
docker compose up --build -d
```

## Stop the stack

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env.example down
```

## Service URLs and ports

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5000`
- Backend health: `http://localhost:5000/health`
- PostgreSQL: `localhost:5432`

Default port and environment values are defined in `docker/.env.example`.

## Login

- Frontend login page: `http://localhost:3000/login`
- Backend login endpoint: `POST /api/auth/login`

No default app user is seeded by migrations/bootstrap SQL.

## Create a default admin user

After the stack is running, create (or update) an admin user directly in Postgres:

```bash
./scripts/create-admin-user.sh
```

Defaults:

- `username`: `admin`
- `password`: `admin`
- `email`: `admin@local.dev`

Optional overrides:

```bash
CONTAINER_NAME=storage-postgres \
DB_NAME=storage \
DB_USER=storage \
USERNAME=admin \
PASSWORD=admin \
EMAIL=admin@local.dev \
./scripts/create-admin-user.sh
```

Implementation details:

- Uses `docker exec` + `psql` against `storage-postgres`
- Uses `pgcrypto` to write a BCrypt-compatible password hash
- Upserts by username and enforces `Admin` role

## Quick verification

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env.example ps
curl -i http://localhost:5000/health
curl -i http://localhost:3000
```

Expected results:

- `/health` returns `200 OK`
- frontend root returns `200 OK`
- `/api/auth/me` returns `401 Unauthorized` without a token (expected)
