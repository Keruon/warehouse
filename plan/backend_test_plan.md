# Backend Test Suite Plan

## Goal

Create a repeatable backend test suite that runs after every backend build and verifies:
- API contracts
- authorization rules
- validation failures
- CRUD behavior
- stock and project business rules
- audit side effects
- PostgreSQL-backed persistence behavior

The suite should exercise the real ASP.NET Core pipeline, not just isolated methods, because this backend depends on:
- JWT authentication
- FluentValidation
- EF Core with PostgreSQL-specific behavior
- global query filters
- migrations and schema assumptions
- audit logging side effects

---

## Testing Strategy

### Primary approach: integration-first

Use integration tests as the main safety net.

Reason:
- Most behavior is expressed at controller + EF Core + validation + authorization boundaries.
- In-memory substitutes would miss PostgreSQL behavior, migrations, and query-filter interactions.
- The backend already proved that Docker/runtime validation catches issues local compile cannot in this environment.

### Secondary approach: focused unit tests

Add small unit tests only where they give clear value:
- validators
- pure mapping logic if any becomes non-trivial
- service guard logic that is expensive to reach through full API tests

---

## Recommended Test Project Layout

Create a dedicated test folder at repository root:

```text
tests/
	Storage.Backend.IntegrationTests/
	Storage.Backend.UnitTests/
```

### 1. `Storage.Backend.IntegrationTests`

Purpose:
- full HTTP-level API tests using `WebApplicationFactory`
- real PostgreSQL database per test run
- auth + validation + persistence + audit verification

Recommended packages:
- `xunit`
- `xunit.runner.visualstudio`
- `Microsoft.AspNetCore.Mvc.Testing`
- `Microsoft.NET.Test.Sdk`
- `Testcontainers.PostgreSql`
- `FluentAssertions`
- `coverlet.collector`

### 2. `Storage.Backend.UnitTests`

Purpose:
- fast validator and rule-level tests
- no HTTP server required

Recommended packages:
- `xunit`
- `FluentAssertions`
- `Microsoft.NET.Test.Sdk`
- `coverlet.collector`
- optionally `Moq` only if a seam truly benefits from mocking

---

## Database Test Strategy

### Use PostgreSQL, not EF in-memory

Use a disposable PostgreSQL container for integration tests.

Reason:
- the app uses Npgsql features and EF migrations
- delete behaviors and FK rules matter
- global query filters on `IsActive` matter
- enum/string conversion and schema shape matter

### Preferred setup

Use `Testcontainers.PostgreSql` to spin up a database for the integration test assembly.

Pattern:
1. start PostgreSQL container once per test collection
2. override app connection string inside `WebApplicationFactory`
3. apply migrations on startup
4. seed minimum baseline data needed by tests
5. reset data between tests

### Reset strategy

Preferred options:
- simplest: recreate schema/database per test class
- better long-term: use `Respawn` if needed for speed

Start simple first. Optimize only if test runtime becomes a problem.

---

## App Test Harness

Create shared test infrastructure in integration tests:

### `CustomWebApplicationFactory`

Responsibilities:
- boot the API in test mode
- replace `DefaultConnection`
- point to test PostgreSQL container
- optionally lower logging noise

### `DatabaseFixture`

Responsibilities:
- start/stop PostgreSQL test container
- expose connection string
- ensure migrations are applied

### `TestDataSeeder`

Seed only the minimum stable baseline needed for tests:
- admin user
- standard user
- readonly user
- production area
- default production shelf
- one normal warehouse area/shelf/location
- one supplier/component/component type if stock flows need them

### `AuthTestHelper`

Helpers to:
- login with known test users
- attach bearer token to `HttpClient`
- fetch user ids when needed

---

## Test Pyramid For This Repo

### Layer 1: smoke suite

Runs on every backend build.

Purpose:
- prove app boots
- prove migrations apply
- prove auth works
- prove one happy-path flow per major controller group

Target runtime:
- under 2 to 4 minutes

### Layer 2: full integration suite

Runs on pull requests and before releases.

Purpose:
- cover route behavior and main failure paths
- verify permissions and side effects

### Layer 3: focused unit suite

Runs together with integration tests, but should stay very fast.

---

## Route Coverage Plan

Every public route should have at least one test. Mutating routes should have:
- success case
- validation failure case
- authorization failure case when applicable
- not found or business-rule failure case when applicable

### 1. Auth: `api/auth`

Routes:
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

Tests:
- login succeeds with seeded admin user
- login fails with wrong password
- refresh succeeds with valid refresh token
- logout revokes refresh token
- `me` returns current user and active project state

### 2. Users: `api/users`

Tests:
- admin can create user
- non-admin cannot create user
- admin can list users
- self lookup works
- unauthorized user access is blocked where expected
- update changes allowed fields only
- delete deactivates or removes user per API semantics

### 3. Areas: `api/areas`

Tests:
- list/get works
- admin create/update/delete works
- non-admin write attempts fail
- duplicate uniqueness rules fail cleanly

### 4. Shelves: `api/shelves`

Tests:
- CRUD happy path
- invalid area reference fails
- non-admin write attempts fail

### 5. Locations: `api/locations`

Tests:
- CRUD happy path for warehouse locations
- project-only rules are enforced when `LocationKind.Project`
- duplicate code/name within shelf fails if expected
- delete follows soft-delete/query-filter behavior

### 6. Projects: `api/projects`

This is the highest-priority new suite.

Routes:
- `POST /api/projects`
- `GET /api/projects`
- `GET /api/projects/active`
- `PUT /api/projects/active/{id}`
- `DELETE /api/projects/active`
- `PUT /api/projects/{id}/deactivate`
- `PUT /api/projects/{id}/activate`
- `DELETE /api/projects/{id}`
- `POST /api/projects/{id}/close`

Tests:
- create returns `201 Created` and `Location` header
- create normalizes/accepts valid code
- create rejects duplicate code
- list shows current active flag correctly
- set active succeeds for active project
- set active fails for inactive project
- clear active returns `204`
- deactivate clears active project from impacted users
- activate restores project availability
- delete removes project when no stock exists
- delete fails with `project_has_stock` when stock exists
- delete writes `DELETE_PROJECT` audit log
- close project stays admin-only

### 7. Stock: `api/stock`

Routes include:
- receive
- gather
- transfer
- project-return
- bulk-transfer
- stock lookups by component/location

Tests:
- receive increases stock
- gather uses active project when required
- transfer moves stock between locations
- project return moves stock back correctly
- bulk transfer handles multiple lines
- insufficient stock fails clearly
- stock queries return updated balances

### 8. Search: `api/search`

Tests:
- component search returns expected filtered results
- location search respects active/query-filter rules

### 9. Components / Component Types / Categories / Suppliers

For each controller:
- list/get/create/update/delete success path
- duplicate/validation failure path
- admin-only mutation enforcement

### 10. Audit logs: `api/audit-logs`

Tests:
- admin can query audit logs
- non-admin cannot
- entity-specific lookup returns expected records after known mutations

---

## Unit Test Targets

Focus unit tests where full integration coverage would be slow or repetitive.

### Validators

Add unit tests for:
- auth validators
- user validators
- location validators
- stock validators
- project validators

Each validator should test:
- valid payload accepted
- required field missing
- max length exceeded
- numeric/range rule failures

### Project service rules

If the project service keeps growing, add targeted tests for:
- duplicate code rejection
- active project clearing behavior on deactivate
- delete blocked by stock

These can still be integration tests if the service remains tightly coupled to EF.

---

## Authorization Matrix Tests

Use three seeded users:
- `Admin`
- `User`
- `ReadOnly`

Add matrix tests for representative endpoints:
- anonymous request gets `401`
- authenticated but unauthorized request gets `403`
- authorized request succeeds

Do not attempt full matrix coverage for every single endpoint initially. Cover one or two representative routes per policy first, then expand.

---

## Seed Data Requirements

The baseline seed should be deterministic and minimal.

Required baseline entities:
- one admin user
- one normal user
- one readonly user
- one production area
- one production default shelf
- one warehouse area
- one warehouse shelf
- one warehouse location
- one supplier
- one component category
- one component type
- one component

Avoid large fixture dumps. Prefer small builders/factories inside tests.

---

## Test Naming Convention

Use explicit names:

```text
Action_WhenCondition_ShouldResult
```

Examples:
- `CreateProject_WhenCodeIsUnique_ShouldReturnCreated`
- `CreateProject_WhenCodeAlreadyExists_ShouldReturnBadRequest`
- `DeleteProject_WhenProjectHasStock_ShouldReturnBadRequest`
- `GetAuditLogs_WhenUserIsNotAdmin_ShouldReturnForbidden`

---

## CI / Build Integration Plan

### Local commands

Add test commands for:
- unit tests only
- smoke integration tests
- full backend tests

Example target commands:

```text
dotnet test tests/Storage.Backend.UnitTests
dotnet test tests/Storage.Backend.IntegrationTests --filter Category=Smoke
dotnet test tests/Storage.Backend.IntegrationTests
```

### CI stages

1. Restore + build backend
2. Run unit tests
3. Run smoke integration tests
4. Run full integration tests on main/release pipelines

If CI agents already support Docker, `Testcontainers` is the cleanest option.

---

## Coverage Priorities

### Phase 1: foundation

- create integration test project
- create unit test project
- add PostgreSQL test container fixture
- add custom web app factory
- add auth helper and seed data
- prove one auth test and one health/app startup test

### Phase 2: critical business flows

- projects controller full suite
- stock controller happy paths + key failures
- auth suite

### Phase 3: CRUD surfaces

- areas
- shelves
- locations
- users
- suppliers
- component categories
- component types
- components

### Phase 4: search and audit surfaces

- search endpoints
- audit log endpoints
- representative authorization matrix tests

### Phase 5: hardening

- flaky test cleanup
- speed improvements
- coverage reporting
- database reset optimization

---

## Definition Of Done

The backend test suite is considered established when:
- test projects exist and run from CLI
- integration tests boot the real app against disposable PostgreSQL
- every controller has at least one integration test
- every mutating route has success and failure-path coverage
- projects and stock flows have detailed business-rule coverage
- smoke suite runs after every backend build
- audit-log side effects are verified for critical operations

---

## Initial Deliverables

First implementation slice should produce:
- `tests/Storage.Backend.IntegrationTests/Storage.Backend.IntegrationTests.csproj`
- `tests/Storage.Backend.UnitTests/Storage.Backend.UnitTests.csproj`
- `CustomWebApplicationFactory`
- PostgreSQL fixture via `Testcontainers`
- seeded auth helper
- smoke tests for:
	- login
	- `GET /api/auth/me`
	- create/list/delete project
	- delete project audit verification

That slice gives immediate value and covers the newest, highest-risk backend surface first.
