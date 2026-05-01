# Production Zone Plan

## Goal

Refactor warehouse locations so production work can use project-specific target locations inside a production zone.

Expected behavior:
- A user can create a project as a location inside a production zone.
- A user can mark one project as the active project.
- Gathering can use the active project as the target destination.
- By default there is no active project.
- When no active project is selected, gathering only removes stock from warehouse storage and does not place it into a production project location.

## Scope Decisions

- Treat a project as a special kind of warehouse location rather than a separate aggregate.
- Project locations reuse the existing `WarehouseLocation` entity.
- If a shelf is required for project locations, use the shelf value `default`.
- Keep existing warehouse areas, shelves, and locations, but extend locations with production/project metadata.
- Preserve current stock movement logic, then add a gather-to-project path on top of it.
- There is exactly one production zone in the system.
- Active project selection is a manual user choice and is cleared only when the user explicitly deselects it.
- Gather-to-project must create a stock row in the project location so partially used inventory can remain allocated and later be reused or moved elsewhere.
- Default behavior must remain valid for normal warehouse gathering when no project is active.

## Implementation Phases

### Phase 1: Domain and Flow Definition
- Define how a production-zone project is represented in the data model.
- Active project is per user and persists until manually cleared.
- Define the gather flow contract:
	- source location is always a warehouse location with stock
	- target project location is optional
	- if target project exists, gathering becomes a warehouse-to-project transfer that creates or updates a stock row in the project location
	- if target project is not set, gathering remains a warehouse stock reduction
- Define visibility rules for project locations in admin, stock, and items views.

### Phase 2: Backend Changes

#### Backend 2.1: Refactor location model for production projects

- Reuse the existing `WarehouseLocation` entity for project locations instead of introducing a separate project model.
- Extend the location entity only with the minimum metadata needed to distinguish project locations from normal warehouse locations.
- Add fields such as:
	- location kind/category (warehouse bin vs production project)
	- optional production metadata needed for filtering and display
	- flag or relation that marks whether the location belongs to a production zone
- Define how the `default` shelf is created, resolved, and protected so project locations can satisfy the current required `ShelfId` relationship.
- Ensure location validation prevents creating project locations outside production zones.

#### Backend 2.2: Add active project handling
- Add an application-level way to store and resolve the active project.
- Implement this as a user preference persisted against the current user.
- Add endpoints or commands to:
	- set active project
	- clear active project
	- fetch current active project
- Add a close-project command that clears the active project if needed and transitions the project out of active use.
- Require explicit confirmation input for close-project so the operation cannot be triggered accidentally from the UI.
- Ensure default state is null for every user and remains unchanged until manually cleared.

#### Backend 2.3: Add project location creation and listing
- Extend location create/update handlers so admins can create project-type locations under production zones.
- Add filtered queries for:
	- production zones
	- project locations
	- available gather targets
- Return enough data for UI labels so project/location selectors never rely on UUIDs.

#### Backend 2.4: Refactor gather logic
- Split current gather behavior into explicit modes:
	- gather only: remove from warehouse stock
	- gather to active project: move quantity from warehouse source location to project target location by creating or updating a stock row in the project location
- Prefer implementing this as a dedicated service path instead of overloading the existing gather method with hidden side effects.
- Ensure batch code and expiry handling remain consistent when stock is moved into a project location.
- Ensure project stock can later be gathered, transferred, or returned so unused allocated inventory remains reusable.
- Add return-from-project behavior on a per-stock-line basis so a user can send back specific project stock rows individually.
- Add a close-project workflow that returns all remaining project stock to warehouse through the existing transfer flow before the project is closed.
- Define the warehouse return-target rule:
	- first try to find the first matching warehouse stock line for the returned item
	- if no matching warehouse stock line exists, create the returned stock in the first warehouse stock location
- Update audit logging to record:
	- source location
	- optional target project location
	- actor
	- quantity
	- operation type (`STOCK_GATHER` vs `STOCK_GATHER_TO_PROJECT`)
	- per-line return operations from project back to warehouse
	- project close operations that trigger bulk stock return

#### Backend 2.5: Update stock and item read models
- Update stock-level responses so project locations are distinguishable from normal warehouse locations.
- Update component detail and gather-related endpoints to return readable location/project names.
- Add a dedicated view model if needed for:
	- active project summary
	- stock currently gathered to a project
	- production-zone project inventory
	- returnable project stock lines, including batch and expiry context
- Expose enough return-target information for the UI to explain where returned stock will go.

#### Backend 2.6: Validation and permissions
- Restrict project creation to appropriate roles.
- Prevent project locations from being selected as source warehouse locations when gathering.
- Prevent inactive or archived projects from being used as active targets.
- Validate that active project belongs to a production zone and is active.
- Prevent project close if any stock line cannot be returned through the existing transfer flow; fail the close operation transactionally.

### Phase 3: Database Migrations

#### Migration 3.1: Location schema extension
- Add the columns needed to represent project locations and/or production location kind.
- Backfill existing rows so all current warehouse locations are marked as normal warehouse locations.
- Keep defaults safe for existing data.
- If the current schema requires `ShelfId`, create or backfill a `default` shelf strategy that project locations can use consistently.

#### Migration 3.2: Active project persistence
- Add persistence for active project selection.
- Add nullable `ActiveProjectLocationId` on `Users` with a foreign key to `WarehouseLocations`.
- Default the column to null for all existing users.

#### Migration 3.3: Indexes and constraints
- Add indexes for:
	- location kind/type
	- area or zone filters for production locations
	- active project lookup
- Add constraints so project locations cannot violate required relationships.
- Add safeguards so the `default` shelf cannot be deleted while project locations depend on it.

#### Migration 3.4: Data correction and rollout safety
- Add backfill SQL or migration logic to classify existing production-related rows if any already exist.
- Ensure the migration is safe on the current live schema and does not depend on manual SQL.
- Verify EF snapshot, generated migration, and runtime model all match before deployment.
- If project status fields are introduced for close/archive behavior, backfill existing rows with a safe active default.

### Phase 4: Frontend Changes

#### Frontend 4.1: Admin location/project management
- Update the location management UI to let admins create a project location under a production zone.
- Make the form clearly distinguish between normal warehouse locations and project locations.
- Add filters and badges so production projects are visible in location lists.

#### Frontend 4.2: Active project UX
- Add an active project control in the production/gather workflow.
- Show:
	- current active project name
	- clear active project action
	- explicit empty state when no project is active
- Show the active project next to the username in the top bar so the current target is always visible.
- Avoid hidden defaults; the UI should always show whether gathering is targeting a project or not.
- Deselection must be an explicit manual UI action.
- Add a close-project action with clear messaging that all remaining project stock will be returned to warehouse using the existing transfer flow.
- Add an "Are you sure?" confirmation step before close-project executes.

#### Frontend 4.3: Gathering flow updates
- Update the gathering page so it shows:
	- source warehouse location
	- active project target if selected
	- no-target mode when no active project exists
- If an active project exists, the confirmation UI should make it clear that stock is being moved into that project and will remain available there for later reuse.
- If no active project exists, the confirmation UI should state that stock is only being removed from warehouse stock.

#### Frontend 4.4: Item detail and stock views
- Update stock-by-location views to display project names distinctly from warehouse locations.
- Add project/production labels where needed in item detail drawers, gather views, and transfer-related screens.
- Ensure dropdowns and tables always use names/codes, never raw UUIDs.
- Add a project inventory view that lists project stock rows individually and exposes a return action per line.

#### Frontend 4.6: Return stock from project view
- Add a per-line return control in the project view.
- Show enough line detail to make returns unambiguous:
	- component / part number
	- quantity in project stock row
	- batch code
	- expiry date
	- source or return target location when needed
- Ensure the return action works on a single stock line at a time and updates project inventory and warehouse stock views immediately.
- Add a close-project action in the same project view that triggers return of all remaining stock lines and then closes the project.
- Make the close-project action a destructive confirmation flow with explicit user confirmation before submit.
- Show success and failure feedback per workflow so users know whether the project was fully returned and closed.
- Show the resolved return destination in the UI when available: first matching warehouse line, otherwise the first warehouse stock location.

#### Frontend 4.5: Query and state management
- Add hooks/services for:
	- fetching active project
	- setting active project
	- clearing active project
	- listing project locations
- Invalidate relevant stock, gather, and item queries when active project changes.

### Phase 5: Testing and Verification
- Add backend tests for:
	- creating project locations only in production zones
	- setting and clearing active project
	- gathering with active project
	- gathering with no active project
	- invalid target project selection
	- closing a project with remaining stock and returning all lines through the existing transfer flow
	- failing project close when one return line cannot be transferred
	- returning stock uses the first matching warehouse line when one exists
	- returning stock creates a new line in the first warehouse stock location when no match exists
- Add frontend tests for:
	- active project empty state
	- gather confirmation text in both modes
	- project selection and clearing
	- location/project display labels
	- close-project confirmation and result handling
	- close-project cancellation from the "Are you sure?" dialog
	- return destination messaging in the project view
- Verify manually in Docker:
	- create production project
	- set active project
	- gather from warehouse into project
	- return a single line from project inventory
	- close project and verify all remaining stock returns to warehouse
	- clear active project
	- gather again with warehouse-only behavior

## Recommended Delivery Order

1. Implement the location model extension for the single production zone and add the EF migration.
2. Add per-user active project persistence on `Users.ActiveProjectLocationId`.
3. Add backend APIs for project locations and active project state.
4. Refactor gather logic so gather-to-project creates or updates project stock rows.
5. Implement per-line return and close-project workflows on top of the existing transfer flow.
6. Update frontend admin screens for project creation.
7. Update the top bar and gathering UI to show the active project and support manual deselection.
8. Update stock and item views to label project locations clearly, including per-line return and close-project actions.
9. Run migration, regression-test stock flows, and verify audit logging and reusable project stock behavior.

## Open Questions

- No open questions recorded.