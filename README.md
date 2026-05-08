# Project Info

This is entirely AI-generated project. 

plan folder contains summary of different plans used to direct build agents to complete programming tasks in phases.

Planned and compiled by using local qwen3.5 and copilot

Token usage was 22% of monthly copilot allotment (as of may 2026)


### Prerequisites

- Docker Engine with Compose plugin (`docker compose`)

### First-time setup

From repository root:

```bash
cp docker/.env.example docker/.env
```

You can keep the defaults for local usage, or edit `docker/.env` to change ports, DB credentials, and API URL.

### Build and start all services

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env up --build -d
```

This builds and starts:

- `postgres`
- `backend`
- `frontend`

### Development workflow

- Start (without rebuilding):

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d
```

- Rebuild only backend after backend code changes:

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build backend
```

- Rebuild only frontend after frontend code changes:

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d --build frontend
```

- Follow logs for all services:

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env logs -f
```

- Follow logs for one service:

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env logs -f backend
docker compose -f docker/docker-compose.yml --env-file docker/.env logs -f frontend
docker compose -f docker/docker-compose.yml --env-file docker/.env logs -f postgres
```

### Stop and cleanup

- Stop containers:

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env down
```

- Stop and remove volumes (deletes local DB data):

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env down -v
```

### Service URLs

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5000`
- Backend health check: `http://localhost:5000/health`

For a fuller Docker guide (including admin user bootstrap), see `docker/README.md`.
