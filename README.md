# Field Task Manager

A web system for managing field tasks. An **Admin** creates tasks with a location on a map and assigns them to workers. **Workers** see only their own tasks, change task status, and add comments. The admin verifies completed tasks or returns them to work.

## Features

- Registration / login (JWT-based authentication)
- Admin account is created automatically on first run; self-registered users get the **Worker** role
- Task creation: title, description, location on a map, assignee, deadline
- Interactive map with task markers colored by status
- Location editing by dragging the marker (admin)
- Comments on tasks (author + timestamp)
- Filtering by status / assignee and full-text search by title / description
- Role-based access: workers see and act only on their own tasks

## Workflow

```
Created → In Progress → Done → Verified
```

- **Worker** (assignee only): `Created → InProgress`, `InProgress → Done`
- **Admin**: `Done → Verified` (confirm closure) or `Done → InProgress` (return to work)

## Tech stack

| Layer    | Technology |
|----------|------------|
| Backend  | .NET 9, ASP.NET Core Web API, EF Core 9, Npgsql, JWT |
| Frontend | Angular 18, PrimeNG 18 (Aura theme), Leaflet (OpenStreetMap) |
| Database | PostgreSQL 16 |
| Infra    | Docker, docker compose, nginx, Makefile |

Backend follows Clean Architecture: `Domain` → `Application` → `Infrastructure` → `Api`, with the repository pattern, services, and Unit of Work.

## Run

Prerequisites: Docker with the compose plugin (and `make`, optional).

```bash
git clone <repo-url>
cd field-task-manager
make run          # or: docker compose up --build -d
```

| What | URL |
|------|-----|
| Application | http://localhost:8080 |
| API / Swagger | http://localhost:5080/swagger |

**Default admin:** `admin@ftm.local` / `Admin123!`
(configurable via `Admin__Email` / `Admin__Password` env vars in `docker-compose.yml`)

Other commands: `make logs`, `make stop`, `make clean` (removes volumes and images).

## Project structure

```
backend/
  src/
    FieldTaskManager.Domain/          # entities, enums, repository interfaces
    FieldTaskManager.Application/     # DTOs, services, business rules
    FieldTaskManager.Infrastructure/  # EF Core, repositories, UoW, JWT, seeding
    FieldTaskManager.Api/             # controllers, middleware, DI
frontend/
  src/app/
    core/                             # auth, interceptor, guard, API services, models
    features/                         # login/register, shell, tasks, map
docker-compose.yml
Makefile
```

## Notes

- The database schema is created automatically on startup (`EnsureCreated`) — sufficient for a pilot; for production this would be replaced with EF Core migrations.
- The API waits for PostgreSQL via a healthcheck plus a retry loop on startup.
- nginx serves the Angular app and proxies `/api` to the backend, so the frontend needs no hardcoded API URL.
