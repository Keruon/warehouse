# Docker staging for Storage project

This folder stages local Docker development for three compose targets:

- `postgres`
- `backend`
- `frontend`

## What was found during analysis

- Frontend is a React app (`react-scripts`) and has a proxy to `http://localhost:5000`.
- Backend source exists in `backend/`, but there is currently no `.csproj` or `.sln` file in the repository.
- Legacy database bootstrap SQL exists in `backend/archive/create_db.sql` and is mounted into PostgreSQL init scripts.

## Start the stack

From this `docker/` folder:

```bash
cp .env.example .env
docker compose up --build
```

Or from repository root:

```bash
docker compose -f docker/docker-compose.yml --env-file docker/.env.example up --build
```

## Important backend note

The backend container checks for a `backend/*.csproj` file at startup.

- If found: it runs `dotnet restore` and `dotnet run` on port `8080` (mapped to host `${BACKEND_PORT}`).
- If not found: it prints a clear message and stays alive so the rest of the stack can still run.

To fully enable the API container, add your backend project file (for example `backend/backend.csproj`).
