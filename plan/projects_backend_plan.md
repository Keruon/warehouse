# Projects Backend Plan

## Context

Projects are modelled as `WarehouseLocation` records with `LocationKind = Project`, stored inside
the production zone's "default" shelf. The `ProjectLocationService` already handles:
- `ListProjectsAsync`
- `GetActiveProjectAsync`
- `SetActiveProjectAsync`
- `ClearActiveProjectAsync`

Missing: **create, deactivate, activate, delete** operations and their corresponding controller
endpoints. **No database migration is required** — the existing schema supports all needed behaviour.

---

## Gap Analysis

| UI action        | Backend endpoint | Service method         | Status   |
|------------------|-----------------|------------------------|----------|
| Create project   | `POST /api/projects` | `CreateProjectAsync` | ❌ Missing |
| Deactivate       | `PUT /api/projects/{id}/deactivate` | `DeactivateProjectAsync` | ❌ Missing |
| Activate         | `PUT /api/projects/{id}/activate` | `ActivateProjectAsync` | ❌ Missing |
| Delete           | `DELETE /api/projects/{id}` | `DeleteProjectAsync` | ❌ Missing |
| Close (admin)    | `POST /api/projects/{id}/close` | `CloseProjectAsync` | ✅ Exists (Admin only, keep as-is) |
| List             | `GET /api/projects` | `ListProjectsAsync` | ✅ Exists |
| Get active       | `GET /api/projects/active` | `GetActiveProjectAsync` | ✅ Exists |
| Set active       | `PUT /api/projects/active/{id}` | `SetActiveProjectAsync` | ✅ Exists |
| Clear active     | `DELETE /api/projects/active` | `ClearActiveProjectAsync` | ✅ Exists |

---

## Authorization Policy

All new endpoints use `[Authorize]` (all authenticated roles). The existing
`CloseProject` retains `[Authorize(Roles = "Admin")]` — it is a distinct destructive operation
and is out of scope here.

---

## 1. DTOs

File: `backend/Helpers/DTOs/ProjectDtos.cs`

Add:

```csharp
public sealed class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
```

`ProjectLocationSummaryResponse` is already the correct return type for all mutating endpoints.

---

## 2. Service Interface

File: `backend/Services/Projects/IProjectLocationService.cs`

Add four method signatures:

```csharp
Task<ProjectLocationSummaryResponse> CreateProjectAsync(string name, string code, Guid actorId, CancellationToken cancellationToken = default);
Task<ProjectLocationSummaryResponse> DeactivateProjectAsync(Guid locationId, Guid actorId, CancellationToken cancellationToken = default);
Task<ProjectLocationSummaryResponse> ActivateProjectAsync(Guid locationId, Guid actorId, CancellationToken cancellationToken = default);
Task DeleteProjectAsync(Guid locationId, CancellationToken cancellationToken = default);
```

---

## 3. Service Implementation

File: `backend/Services/Projects/ProjectLocationService.cs`

### 3a. `CreateProjectAsync`

Logic:
1. Trim `name` and `code`; throw `ArgumentException` if either is empty after trim.
2. Check for duplicate `Code` (case-insensitive) among existing project locations —
   use `IgnoreQueryFilters` to include inactive ones. Throw `InvalidOperationException`
   with error code `duplicate_project_code` on collision.
3. Resolve the default production shelf via `ResolveShelfIdAsync(LocationKind.Project, null, actorId)`.
4. Insert a new `WarehouseLocation`:
   - `LocationKind = LocationKind.Project`
   - `ShelfId` = resolved shelf
   - `Name` = trimmed name
   - `Code` = trimmed, upper-cased code
   - `IsActive = true`
   - `BinX = 0`, `BinY = 0`
   - `CreatedBy / ModifiedBy = actorId`, timestamps = `DateTime.UtcNow`
5. `SaveChangesAsync`.
6. Return projection mapped inline (same shape as `ListProjectsAsync` SELECT).

### 3b. `DeactivateProjectAsync`

Logic:
1. Load location with `IgnoreQueryFilters`; enforce `LocationKind.Project`.
2. If already inactive, return current projection — idempotent, no save.
3. Set `IsActive = false`, update `ModifiedAt` / `ModifiedBy = actorId`.
4. Bulk-null `ActiveProjectLocationId` on all `Users` where `ActiveProjectLocationId = locationId`.
5. `SaveChangesAsync`.
6. Return updated projection.

Side effect: users whose active project is deactivated will have it cleared. Frontend detects
this via query invalidation of `['active-project']`.

### 3c. `ActivateProjectAsync`

Logic:
1. Load location with `IgnoreQueryFilters`; enforce `LocationKind.Project`.
2. If already active, return current projection — idempotent, no save.
3. Set `IsActive = true`, update `ModifiedAt` / `ModifiedBy = actorId`.
4. `SaveChangesAsync`.
5. Return updated projection.

### 3d. `DeleteProjectAsync`

Logic:
1. Load location with `IgnoreQueryFilters`; enforce `LocationKind.Project`.
   Throw `KeyNotFoundException` if not found.
2. **Stock guard**: if any `StockLocation` row references this `LocationId` with `Quantity > 0`,
   throw `InvalidOperationException("Project has stock and cannot be deleted.")`
   → surface as error code `project_has_stock`.
3. Clear `ActiveProjectLocationId` on all users who reference this project (prevent FK violation).
4. Hard-delete the `WarehouseLocation` record.
5. `SaveChangesAsync`.

---

## 4. Controller

File: `backend/Controllers/Locations/ProjectController.cs`

Add four new action methods (all `[Authorize]`, no role restriction):

### `POST /api/projects`
- Accept `[FromBody] CreateProjectRequest request`.
- Call `CreateProjectAsync(request.Name, request.Code, GetCurrentUserId(), cancellationToken)`.
- Return `Created($"/api/projects/{result.Id}", result)` (201) on success.
- `InvalidOperationException` → `BadRequest(new ErrorResponse { Code = "...", Message = ... })`.

### `PUT /api/projects/{locationId:guid}/deactivate`
- Call `DeactivateProjectAsync(locationId, GetCurrentUserId(), cancellationToken)`.
- Return `Ok(result)`.
- `KeyNotFoundException` → `NotFound`.
- `InvalidOperationException` → `BadRequest`.

### `PUT /api/projects/{locationId:guid}/activate`
- Call `ActivateProjectAsync(locationId, GetCurrentUserId(), cancellationToken)`.
- Same error mapping as deactivate.

### `DELETE /api/projects/{locationId:guid}`
- Call `DeleteProjectAsync(locationId, cancellationToken)`.
- Return `NoContent()` (204) on success.
- `KeyNotFoundException` → `NotFound`.
- `InvalidOperationException` → `BadRequest` with error code/message.

---

## 5. Validation

Add validation to `CreateProjectRequest` consistent with the existing validator pattern in
`backend/Helpers/Validators/`:
- `Name`: required, max 100 chars.
- `Code`: required, max 50 chars. Upper-casing is applied in the service layer.

---

## 6. Migration

**No migration required.** All needed columns and tables already exist:
- `WarehouseLocation` with `LocationKind`, `IsActive`, `Name`, `Code`, `ShelfId` ✅
- `User.ActiveProjectLocationId` nullable FK ✅
- `StockLocation` with `LocationId` and `Quantity` ✅

---

## 7. Audit Logging

Check whether existing `AuditService` is called in other warehouse mutation controllers. If
audit logging is expected, add `IAuditService` calls for:
- `CREATE_PROJECT`, `DEACTIVATE_PROJECT`, `ACTIVATE_PROJECT`, `DELETE_PROJECT`

---

## 8. FK Constraint Check (Before Implementation)

Before implementing `DeleteProjectAsync`, verify the FK from `StockLocation.LocationId` →
`WarehouseLocation.Id` in `ApplicationDbContext.cs`.

If the delete behavior is `Restrict` or `NoAction`, the stock guard in step 2 of
`DeleteProjectAsync` is mandatory (the DB will reject the delete otherwise). If it is
`Cascade`, the guard is still recommended for a meaningful error message.

---

## Delivery Phases

### Phase 1 — DTO + Interface
- Add `CreateProjectRequest` to `ProjectDtos.cs`.
- Add four method signatures to `IProjectLocationService`.

### Phase 2 — Service implementation
- Implement all four methods in `ProjectLocationService`.
- Verify FK behavior in `ApplicationDbContext` before finalizing `DeleteProjectAsync`.

### Phase 3 — Controller endpoints
- Add the four new actions to `ProjectController`.
- Inject `IAuditService` into `ProjectController`; call it in `DeleteProject` with action `DELETE_PROJECT` after successful delete.
- Smoke test with Swagger UI or curl.

### Phase 4 — Frontend integration
- Frontend service and hooks call the new endpoints.
- Confirm all React Query cache invalidations fire correctly.

---

## Open Questions

1. ~~Should `CreateProject` return `201 Created` with a `Location` header?~~ → **Yes, 201 with Location header.**
2. ~~Should deactivating/deleting a project produce an `AuditLog` entry?~~ → **Delete only; deactivate/activate do not require audit logging.**
3. ~~Is there a business rule limiting the maximum number of concurrent active projects?~~ → **No limit.**