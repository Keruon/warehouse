## Plan: ComponentType Field Split Refactor

Refactor ComponentType from a combined name to explicit fields (`kind`, `value`, `footprint`) while keeping existing `Type` enum (SMD/ThroughHole/etc.), performing an immediate API/UI switch (no transitional `Name` compatibility), and using no automatic parsing for existing `Name` values.

**Steps**
1. Finalize domain contract for ComponentType and naming rules: define required/optional semantics for `Kind`, `Value`, `Footprint`, keep `Type` enum as package-family, and define uniqueness rule as `CategoryId + Kind + Value + Footprint`. This drives all downstream validators and indexes.
2. Backend model and DbContext update: replace `Name` usage in model/queries/constraints with new fields; add EF configuration and unique indexes for the new key tuple; remove obsolete uniqueness assumptions tied to `Name`. *Depends on 1*
3. DTO/API contract replacement: update create/update/response DTOs and request validation to require `kind`, `value`, `footprint`; remove `name` from API contracts and controller binding. *Depends on 2*
4. Command/query handler refactor: update create/update duplicate checks and listing/sorting/filter logic in component type handlers and search service logic currently reading `Name`/`ComponentTypeName`; define new display composition helper for responses where needed. *Depends on 3*
5. Database migration strategy (immediate switch): create migration adding new columns (`Kind`, `Value`, `Footprint`), backfilling no values automatically, then making them NOT NULL only after explicit data preparation; remove/deprecate old `Name` column and related indexes in final migration step.
6. Data preparation scripts: add/update scripts to seed valid `Kind/Value/Footprint` records and provide a manual mapping workflow for existing rows (export unresolved rows, apply curated updates, verify no NULLs before hard constraints). *Parallel with 5 until final constraint enforcement*
7. Frontend type/service updates: replace `ComponentTypeResponse.name` usage with structured fields in TS models and API services; adjust query params and payload builders where component type text was used. *Depends on 3*
8. Frontend UI refactor: update admin component type forms/tables/filters and warehouse selectors to capture/display `kind + value + footprint` explicitly; introduce a shared formatter for labels (for dropdowns/details/search result display). *Depends on 7*
9. Search behavior validation and hardening: verify search endpoints/UI no longer depend on legacy combined name fields and return stable results for existing filters (category/type/manufacturer/partNumber). *Depends on 4 and 8*
10. Cleanup and compatibility close-out: remove any remaining legacy `Name` references in code, docs, scripts, and tests; ensure no old payloads are accepted. *Depends on 5, 8, 9*

**Relevant files**
- `/home/keruo/storage/backend/Models/ComponentType.cs` - replace `Name` with `Kind/Value/Footprint`; keep `Type` enum
- `/home/keruo/storage/backend/Data/ApplicationDbContext.cs` - indexes, uniqueness, query/filter configuration updates
- `/home/keruo/storage/backend/Helpers/DTOs/ComponentTypeDtos.cs` - request/response contract changes
- `/home/keruo/storage/backend/Helpers/Validators/ComponentValidators.cs` - enforce required field rules and length constraints
- `/home/keruo/storage/backend/Controllers/Items/ComponentTypeController.cs` - endpoint request binding updates
- `/home/keruo/storage/backend/Controllers/Items/ComponentTypeHandlers.cs` - duplicate checks, ordering/filtering updates
- `/home/keruo/storage/backend/Services/Search/SearchService.cs` - remove dependence on legacy combined name text
- `/home/keruo/storage/backend/Models/Component.cs` - re-evaluate `ComponentTypeName` cached display behavior
- `/home/keruo/storage/backend/Helpers/MappingProfiles/ComponentMappingProfile.cs` - mappings for new fields
- `/home/keruo/storage/backend/Migrations/20260423122658_InitialCreate.cs` and `/home/keruo/storage/backend/Migrations/ApplicationDbContextModelSnapshot.cs` - baseline references; ensure new migration supersedes correctly
- `/home/keruo/storage/scripts/seed-sample-data.sh` - seed explicit component type fields
- `/home/keruo/storage/scripts/repair-legacy-schema.sh` - if used, align schema expectations with new fields
- `/home/keruo/storage/frontend/src/types/inventory.ts` - frontend type model change
- `/home/keruo/storage/frontend/src/services/componentTypeService.ts` - payload/query contract updates
- `/home/keruo/storage/frontend/src/components/Admin/ComponentTypesManager.tsx` - create/edit/list UI changes
- `/home/keruo/storage/frontend/src/components/Warehouse/ComponentSearch.tsx` - label/filter rendering updates
- `/home/keruo/storage/frontend/src/components/Warehouse/ComponentDetailDrawer.tsx` - display formatting updates
- `/home/keruo/storage/frontend/src/pages/ItemsPage.tsx` - component type selector labels and mapping

**Verification**
1. Schema verification: run migration on dev DB, confirm `ComponentTypes` contains `Kind/Value/Footprint` with expected constraints/indexes and no legacy `Name` dependence.
2. Data integrity verification: run SQL checks for NULL/empty `Kind/Value/Footprint` and duplicate `(CategoryId, Kind, Value, Footprint)` combinations before enforcing NOT NULL/unique constraints.
3. Backend API verification: execute CRUD and list endpoints for component types using only new fields; verify 2xx for valid payloads and 4xx for invalid/missing fields.
4. Search regression verification: test `/api/search/components` and related UI flows with authenticated requests; confirm no enum/name conversion errors.
5. Frontend verification: create/edit/search/select flows in Admin + Warehouse pages render and submit structured fields correctly.
6. Script verification: run seed script on empty DB and non-empty DB; confirm idempotency and valid component type rows.

**Decisions**
- Keep existing `ComponentType.Type` enum as a separate package-family dimension.
- Perform immediate switch from `Name` to `Kind/Value/Footprint` (no compatibility payload period).
- Do not auto-parse existing combined names; use explicit/manual data preparation.

**Further Considerations**
1. Naming normalization: enforce canonical casing/unit format (e.g., `100nF` vs `0.1uF`) to avoid semantic duplicates.
2. Cached display field policy: decide whether `Component.ComponentTypeName` remains and is generated from structured fields or is removed entirely.
3. Operational rollout: plan migration window because immediate switch without compatibility can break older clients instantly.

