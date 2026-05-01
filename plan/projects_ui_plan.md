# Projects UI Plan

## Goal
Add frontend project management capabilities for:
- Create project
- Delete project
- Set active project
- Deactivate project

The UX should make project state obvious in all stock-related flows and prevent invalid actions.

## Scope

### In scope
- Shared (non-admin-only) project management UI for project lifecycle actions.
- Global active-project visibility and quick switching.
- Guardrails for destructive actions (delete/deactivate).
- Query cache updates and optimistic/near-optimistic refresh behavior.
- Access model where all authenticated users can manage projects.

### Out of scope (for this plan)
- Backend contract changes.
- New business rules beyond existing API semantics.
- Reporting dashboards for historical project usage.

## UX Surfaces

### 1) Project Management Panel (primary shared surface)
Recommended location:
- Add a top-level Projects page visible to all authenticated users.
- Optional: keep a secondary shortcut/tab in Admin, but not as the only entry point.

Table columns:
- Name
- Code
- Status (Active/Inactive)
- Is Current User Active Project (Yes/No)
- Actions

Actions per row:
- Set Active
- Deactivate (when active status is true)
- Activate (when active status is false)
- Delete (when allowed by backend)

Top-level actions:
- Create Project button
- Optional status filter (All/Active/Inactive)
- Optional search by name/code

### 2) Header/Top Bar (global context)
- Show current active project for logged-in user.
- Add quick clear/switch controls.
- If active project becomes inactive/deleted, display fallback status immediately after refresh.

### 3) Gathering Page integration
- Project selector should list only valid selectable projects (typically active only).
- Keep current informational tags:
  - Active project
  - Hidden inactive count (if desired)
- Closing/return actions continue to work with current flow.

## Interaction Design

### Create project flow
1. User clicks Create Project.
2. Modal form opens with fields:
	- Name (required)
	- Code (required, uppercase normalization optional)
3. On submit:
	- Disable submit button while pending.
	- Show inline/backend validation errors.
4. On success:
	- Close modal.
	- Refresh projects list.
	- Optional toast: Project created.

### Set active flow
1. User clicks Set Active on a row (or chooses from selector).
2. Mutation runs.
3. On success:
	- Refresh active-project query.
	- Refresh projects list.
	- Refresh project inventory query if open.
4. On error:
	- Show backend message (for example inactive_project_failed).

### Deactivate flow
1. User clicks Deactivate.
2. Confirmation modal:
	- Explain that inactive projects cannot be set active.
3. On success:
	- Refresh projects list.
	- If deactivated project was user active project, clear active project state in UI.

### Delete flow
1. User clicks Delete.
2. Destructive confirmation modal with project name/code.
3. On success:
	- Remove from list (via refetch or optimistic update).
	- If deleted project was active project, clear active project state.
4. On backend conflict/validation:
	- Show message (for example project has stock and cannot be deleted).

## API and State Plan

## Query keys (standardize)
- ['projects']
- ['active-project']
- ['project-inventory', projectId]

### Mutations to support in frontend service/hook layer
- createProject(payload)
- setActiveProject(projectId)
- clearActiveProject()
- deactivateProject(projectId)
- activateProject(projectId) (if backend has explicit endpoint; otherwise update mutation)
- deleteProject(projectId)

### Cache invalidation rules
After create/deactivate/activate/delete:
- invalidate ['projects']
- invalidate ['active-project'] when action can affect active state
- invalidate ['project-inventory'] when active project can change

After setActive/clearActive:
- invalidate ['active-project']
- invalidate ['projects']
- invalidate ['project-inventory']

## Component Plan

### New/updated components
- ProjectManager (new): table + actions + create modal
- ProjectFormModal (new, optional extraction)
- Reuse existing active project tags in layout and gathering page

### Suggested file targets
- frontend/src/components/Projects/ProjectsManager.tsx (new)
- frontend/src/pages/ProjectsPage.tsx (new)
- frontend/src/services/projectService.ts (extend if needed)
- frontend/src/hooks/useProject.ts (new optional hook wrapper)
- frontend/src/App.tsx (add Projects route)
- frontend/src/components/Layout/Layout.tsx (add navigation item + status/action refinements)
- frontend/src/pages/GatheringPage.tsx (keep selector constraints in sync)

## Validation and Error Handling

### Form validation
- Name required, trimmed.
- Code required, trimmed, max length per backend constraints.

### API error mapping
- Centralize Axios error-to-message helper for consistent toasts.
- Prefer backend message/code when available.

### Empty states
- No projects yet: show empty call-to-action.
- Filtered empty: show clear filter action.

## Permissions
- All authenticated users:
  - Create projects.
  - Set and clear active project.
  - Deactivate and activate projects.
  - Delete projects (subject to backend business constraints, such as existing stock links).

If backend still enforces admin-only authorization for some operations, backend policy must be updated to match this product rule.

## Test Plan

### Manual scenarios
1. Create project success.
2. Create project duplicate code/name error.
3. Set active to active project success.
4. Set active to inactive project fails with clear error.
5. Deactivate non-active project success.
6. Deactivate currently active project clears active state.
7. Delete inactive empty project success.
8. Delete blocked project shows backend reason.
9. Header and gathering page update immediately after all actions.

### Regression focus
- Receiving/Gathering/Stock Moves flows still compile and run.
- No stale project shown in selectors after state changes.

## Delivery Phases

### Phase 1: Data and hooks
- Finalize service methods and React Query hooks.
- Standardize query keys and invalidation.

### Phase 2: Shared Projects UI
- Implement Projects page with table and create/deactivate/delete actions.
- Add confirmations and toasts.

### Phase 3: Navigation and global integration
- Add Projects route/menu entry for all authenticated users.
- Ensure layout and gathering selector sync with project state changes.
- Harden active-project edge cases.

### Phase 4: Validation pass
- Run full frontend build and manual scenario matrix.
- Fix UX consistency issues found during testing.

## Acceptance Criteria
- Any authenticated user can create, delete, set active, and deactivate projects from frontend.
- Active project state is consistent across header, gathering page, and projects views.
- Invalid actions are blocked with clear messaging.
- Frontend compiles cleanly and no project-related regressions are introduced.