# Frontend Implementation Plan — Warehouse Stock Management

The backend is fully implemented. The frontend uses a TypeScript React shell with routing and shared providers already in place. The UI stack is **React 18 + Ant Design v5 + @tanstack/react-query v5 + Axios + React Table v7**. Implementation follows the existing folder split: `services/` → `hooks/` → `components/` → `pages/`.

---

## Phase 0 — Foundation *(blocks all other phases)*

**Goal:** Auth flow, API client with token refresh, Layout shell, QueryClient wiring.

### Task 0.1: Axios instance + JWT interceptors
- Create `frontend/src/services/api.ts`
- Create an Axios instance with no `baseURL` (proxy in `package.json` handles `http://localhost:5000`)
- **Request interceptor**: attach `Authorization: Bearer <accessToken>` from `localStorage`
- **Response interceptor**: on 401, call `POST /api/auth/refresh` with stored refresh token, update `localStorage` with new tokens, retry the original request once; on a second 401, clear tokens and redirect to `/login`

### Task 0.2: Auth service
- Create `frontend/src/services/authService.ts`
- Functions: `login(username, password)`, `logout(refreshToken)`, `refresh(refreshToken)`, `getMe()`
- All wired to `/api/auth/*`

### Task 0.3: Auth context
- Create `frontend/src/context/AuthContext.tsx`
- React context holding `{ currentUser, accessToken, login, logout, isAdmin, isReadOnly }`
- On mount: attempt `getMe()` if a token exists in `localStorage`; set `currentUser` from response
- Export `<AuthProvider>` wrapper and `useAuth` hook

### Task 0.4: Protected route component
- Create `frontend/src/components/Layout/ProtectedRoute.tsx`
- If no `currentUser`, redirect to `/login`
- Accept optional `requireAdmin` prop; redirect non-admins to `/dashboard`

### Task 0.5: Login page
- Create `frontend/src/pages/LoginPage.tsx`
- Ant Design `Form` with username and password fields
- On submit, call `authService.login()`, store tokens in `localStorage`, navigate to `/dashboard`
- Display Ant Design `Alert` on failure

### Task 0.6: Layout shell
- Create `frontend/src/components/Layout/Layout.tsx`
- Ant Design `Layout` with collapsible `Sider` (sidebar nav) and `Header` (current user display + logout)
- Sidebar menu items, filtered by role:
  - All roles: Dashboard, Search / Items, Receiving, Gathering, Stock Moves
  - Admin only: Users, Admin Panel
- Uses `useAuth` for role-gating menu items

### Task 0.7: Restructure App.tsx
- Wrap the app in `<QueryClientProvider>` (from `@tanstack/react-query`) and `<AuthProvider>`
- Separate `/login` route (no Layout, no ProtectedRoute) from all other routes (inside `<Layout>` + `<ProtectedRoute>`)
- Keep `frontend/src/App.css` minimal and in sync with app imports

---

## Phase 1 — Search & Component Browsing *(parallel with Phase 3 after Phase 0)*

**Goal:** `ItemsPage` — full component search with filters, component detail drawer, stock overview, QR label.

### Task 1.1: Search service
- Create `frontend/src/services/searchService.ts`
- `searchComponents({ q, type, manufacturer, category, supplierId, partNumber, page, pageSize })` → `GET /api/search/components`
- `searchLocations({ q, areaId, shelfId, hasStock, page, pageSize })` → `GET /api/search/locations`

### Task 1.2: Component service
- Create `frontend/src/services/componentService.ts`
- `getComponents(filters)` → `GET /api/components`
- `getComponent(id)` → `GET /api/components/{id}`
- `getComponentStock(id)` → `GET /api/stock/component/{id}`
- `createComponent(data)`, `updateComponent(id, data)`, `deleteComponent(id)` → Admin-only mutations

### Task 1.3: Search hook
- Create `frontend/src/hooks/useSearch.ts`
- `useComponentSearch(params)` using `useQuery(['component-search', params], ...)`
- Debounce the `q` param by 300ms using a `useRef` timer before passing to the query key

### Task 1.4: ComponentSearch component *(reusable)*
- Create `frontend/src/components/Warehouse/ComponentSearch.tsx`
- Ant Design `Input.Search` + collapsible `Form` with filters: Category (Select), Manufacturer (Input), PartNumber (Input), IsActive (Switch)
- Renders results in an Ant Design `Table`: columns Part#, Name, Manufacturer, Category, Qty on Hand, Supplier
- Accepts `onSelect` prop — row click fires `onSelect(component)`
- Handles controlled pagination (`current`, `pageSize`, `total` from API response)

### Task 1.5: ComponentDetailDrawer component
- Create `frontend/src/components/Warehouse/ComponentDetailDrawer.tsx`
- Ant Design `Drawer` with full component metadata
- Inner `Table` for stock by location (from `getComponentStock(id)`)
- `QRCode` component (from `qrcode.react`) displaying the component's `StockSystemCode`
- Print label button triggers browser `window.print()` scoped to the QR section

### Task 1.6: ItemsPage
- Create `frontend/src/pages/ItemsPage.tsx`
- Compose `ComponentSearch` + `ComponentDetailDrawer`
- Admin users see an **Add Component** button opening an Ant Design `Modal` with a creation form

---

## Phase 2 — Stock Operations *(parallel with Phase 1 after Phase 0)*

**Goal:** Guided form workflows for receiving, gathering, and transferring stock.

### Task 2.1: Stock service
- Create `frontend/src/services/stockService.ts`
- `receiveStock({ componentId, locationId, quantity, batchCode })` → `POST /api/stock/receive`
- `gatherStock({ componentId, locationId, quantity })` → `POST /api/stock/gather`
- `transferStock({ componentId, fromLocationId, toLocationId, quantity })` → `POST /api/stock/transfer`
- `bulkTransfer(transfers[])` → `POST /api/stock/bulk-transfer`
- `getStockAtLocation(locationId)` → `GET /api/stock/location/{id}`

### Task 2.2: Stock mutations hook
- Create `frontend/src/hooks/useStock.ts`
- `useMutation` wrappers for each stock operation
- On success, invalidate relevant React Query cache keys: `['component-search']`, `['stock', ...]`
- Show Ant Design `notification.success` / `notification.error` from mutation callbacks

### Task 2.3: Location service
- Create `frontend/src/services/locationService.ts`
- `getAreas()` → `GET /api/areas`
- `getShelvesByArea(areaId)` → `GET /api/shelves?areaId={id}`
- `getLocationsByShelf(shelfId)` → `GET /api/locations?shelfId={id}`

### Task 2.4: LocationPicker component *(reusable)*
- Create `frontend/src/components/Warehouse/LocationPicker.tsx`
- Three chained Ant Design `Select` components: Area → Shelf → Location
- Each subsequent select is disabled and reset until the prior one is selected
- Calls `onChange(locationId)` when a location is selected

### Task 2.5: ReceivingPage
- Create `frontend/src/pages/ReceivingPage.tsx`
- Two-step Ant Design `Steps` form:
  1. Search and select a component via `ComponentSearch`
  2. Pick target location via `LocationPicker`, enter quantity (required) and optional batch code
- Submit calls `stockService.receiveStock()`
- Shows updated stock count on success; resets form for the next receive

### Task 2.6: GatheringPage
- Create `frontend/src/pages/GatheringPage.tsx`
- Same two-step `Steps` pattern:
  1. Search and select component
  2. Select location from the component's current stock (driven by `GET /api/stock/component/{id}` — shows actual locations with quantities); enter quantity to pick
- Submit calls `stockService.gatherStock()`

### Task 2.7: StockMovesPage
- Create `frontend/src/pages/StockMovesPage.tsx`
- Ant Design `Tabs`:
  - **Transfer tab**: select component → select source location (from current stock) → select destination via `LocationPicker` → enter quantity → submit calls `stockService.transferStock()`
  - **Bulk Transfer tab**: editable table where each row is a transfer item; column Add/Remove rows; submit calls `stockService.bulkTransfer()`

---

## Phase 3 — User Management *(parallel with Phase 1 after Phase 0, Admin-gated)*

**Goal:** Full CRUD for users, role-based access.

### Task 3.1: User service
- Create `frontend/src/services/userService.ts`
- `getUsers({ page, pageSize })`, `getUser(id)`, `createUser(data)`, `updateUser(id, data)`, `deleteUser(id)` → `/api/users/*`

### Task 3.2: Users hook
- Create `frontend/src/hooks/useUsers.ts`
- `useQuery` for list and single user
- `useMutation` for create, update, delete with cache invalidation

### Task 3.3: UsersManager component *(shared between UsersPage and AdminPage)*
- Create `frontend/src/components/Admin/UsersManager.tsx`
- Ant Design `Table`: columns Username, Email, Role, Status (active/disabled), Created date
- **Add User** button opens a drawer/modal with Form: username, email, role Select (Admin/User/ReadOnly), password (new users only)
- **Edit** button opens same drawer pre-populated (no password field unless "Reset Password" toggled)
- **Disable / Delete** with Ant Design `Popconfirm`

### Task 3.4: UsersPage
- Create `frontend/src/pages/UsersPage.tsx`
- Wraps `UsersManager` inside `<ProtectedRoute requireAdmin>`

---

## Phase 4 — Admin Panel *(depends on Phase 0; sub-tasks can be built in parallel)*

**Goal:** Tabbed `AdminPage` for all master-data management. Entirely Admin-gated.

### Task 4.1: Admin services (create all six files)
- `frontend/src/services/areaService.ts` — full CRUD `/api/areas`
- `frontend/src/services/shelfService.ts` — full CRUD `/api/shelves`
- `frontend/src/services/categoryService.ts` — full CRUD `/api/component-categories`
- `frontend/src/services/componentTypeService.ts` — full CRUD `/api/component-types`
- `frontend/src/services/supplierService.ts` — full CRUD `/api/suppliers`
- `frontend/src/services/auditService.ts` — `getAuditLogs(filters)`, `getEntityAuditLogs(entityType, entityId)`

### Task 4.2: AreasManager component
- Create `frontend/src/components/Admin/AreasManager.tsx`
- Table: Name, Code, ZoneType, Floor Level, Active
- Add/Edit drawer form: Name, Code, ZoneType (Select from enum values), FloorLevel, Description, IsActive toggle
- Delete blocked by backend if shelves exist — surface error as Ant Design `message.error`

### Task 4.3: ShelvesManager component
- Create `frontend/src/components/Admin/ShelvesManager.tsx`
- Table filterable by Area dropdown; columns: Name, Code, Type, Capacity, Weight Limit, Area, Active
- Drawer form includes Area Select (required), ShelfType Select, CapacityCount, WeightLimitKg
- Delete blocked if locations exist

### Task 4.4: LocationsManager component
- Create `frontend/src/components/Admin/LocationsManager.tsx`
- Table filterable by Area + Shelf cascade; columns: Name, Code, Shelf, BinX, BinY, Dimensions, Reserved, Active
- Drawer form: cascaded Area → Shelf → Name, Code, BinX, BinY, Depth, Width, Height, IsReserved, IsActive
- Delete blocked if stock exists

### Task 4.5: CategoriesManager component
- Create `frontend/src/components/Admin/CategoriesManager.tsx`
- Ant Design `Tree` showing category hierarchy (up to 5 levels)
- Context menu or inline buttons: Add Child, Edit, Delete
- Form: Name, Code, ParentCategory, Description
- Delete blocked if children or component types exist

### Task 4.6: ComponentTypesManager component
- Create `frontend/src/components/Admin/ComponentTypesManager.tsx`
- Table with filters: Category, Manufacturer, PartNumber, IsActive
- Columns: PartNumber, StockSystemCode (SKU), Name, Manufacturer, PackageType, Category, Active
- Drawer form: all `ComponentType` fields (PartNumber, StockSystemCode, Name, Manufacturer, Type enum, PackageType enum, electrical value fields, PCB footprint, CategoryId)

### Task 4.7: SuppliersManager component
- Create `frontend/src/components/Admin/SuppliersManager.tsx`
- Table: Code, Name, Contact, Website, Active
- Drawer form: Code (unique, enforced by backend), Name, ContactName, Email, Phone, Website, Address, IsActive
- Unique code violation surfaced from backend error message

### Task 4.8: AuditLogViewer component
- Create `frontend/src/components/Admin/AuditLogViewer.tsx`
- Table with filter bar: EntityType (Select), User (Select from user list), Date Range Picker
- Columns: Timestamp, User, Action, EntityType, EntityId
- Expandable row shows OldValues / NewValues JSON diff in formatted `pre` blocks
- Max 200 per page; read-only (no write actions)

### Task 4.9: AdminPage
- Create `frontend/src/pages/AdminPage.tsx`
- Ant Design `Tabs` with tabs: Areas, Shelves, Locations, Categories, Component Types, Suppliers, Users, Audit Log
- Reuses `UsersManager` for the Users tab
- Wrapped in `<ProtectedRoute requireAdmin>`

---

## Phase 5 — Dashboard *(depends on Phase 0, can be built any time after)*

### Task 5.1: Dashboard page
- Create `frontend/src/pages/DashboardPage.tsx`
- Four Ant Design `Statistic` cards (in a responsive `Row`/`Col` grid):
  - Total Component Types
  - Total Quantity on Hand
  - Active Locations
  - Low Stock Items (qty < configurable threshold — use 5 as default)
- Quick action buttons: Receive, Gather, Search (navigate to respective pages)
- Recent Audit Log table (last 10 entries, Admin only) — reuses `auditService.getAuditLogs`

---

## File Inventory

| File | Purpose |
|------|---------|
| `frontend/src/services/api.ts` | Axios instance + JWT interceptors + silent refresh |
| `frontend/src/services/authService.ts` | Login, logout, refresh, getMe |
| `frontend/src/services/componentService.ts` | Component CRUD + stock query |
| `frontend/src/services/stockService.ts` | receive, gather, transfer, bulkTransfer, location stock |
| `frontend/src/services/locationService.ts` | Areas, shelves, locations — for picker |
| `frontend/src/services/searchService.ts` | Component + location full-text search |
| `frontend/src/services/userService.ts` | User CRUD |
| `frontend/src/services/areaService.ts` | Area CRUD |
| `frontend/src/services/shelfService.ts` | Shelf CRUD |
| `frontend/src/services/categoryService.ts` | Category CRUD |
| `frontend/src/services/componentTypeService.ts` | ComponentType CRUD |
| `frontend/src/services/supplierService.ts` | Supplier CRUD |
| `frontend/src/services/auditService.ts` | Audit log queries |
| `frontend/src/context/AuthContext.tsx` | Auth state, currentUser, isAdmin, AuthProvider |
| `frontend/src/hooks/useAuth.ts` | Consume AuthContext |
| `frontend/src/hooks/useSearch.ts` | Debounced React Query component/location search |
| `frontend/src/hooks/useStock.ts` | useMutation wrappers for stock operations |
| `frontend/src/hooks/useUsers.ts` | User list + mutations |
| `frontend/src/components/Layout/Layout.tsx` | Sidebar + header shell |
| `frontend/src/components/Layout/ProtectedRoute.tsx` | Auth guard + role guard |
| `frontend/src/components/Warehouse/ComponentSearch.tsx` | Reusable search bar + paginated results table |
| `frontend/src/components/Warehouse/ComponentDetailDrawer.tsx` | Component detail + stock + QR code + print |
| `frontend/src/components/Warehouse/LocationPicker.tsx` | Cascading Area → Shelf → Location selects |
| `frontend/src/components/Admin/AreasManager.tsx` | Areas CRUD |
| `frontend/src/components/Admin/ShelvesManager.tsx` | Shelves CRUD |
| `frontend/src/components/Admin/LocationsManager.tsx` | Locations CRUD |
| `frontend/src/components/Admin/CategoriesManager.tsx` | Category hierarchy CRUD |
| `frontend/src/components/Admin/ComponentTypesManager.tsx` | ComponentType CRUD |
| `frontend/src/components/Admin/SuppliersManager.tsx` | Supplier CRUD |
| `frontend/src/components/Admin/AuditLogViewer.tsx` | Audit log read-only table |
| `frontend/src/components/Admin/UsersManager.tsx` | User CRUD (shared between UsersPage and AdminPage) |
| `frontend/src/pages/LoginPage.tsx` | Login form |
| `frontend/src/pages/DashboardPage.tsx` | Stats cards + quick actions + recent audit |
| `frontend/src/pages/ItemsPage.tsx` | Component search + detail drawer |
| `frontend/src/pages/ReceivingPage.tsx` | Guided receive workflow |
| `frontend/src/pages/GatheringPage.tsx` | Guided gather workflow |
| `frontend/src/pages/StockMovesPage.tsx` | Transfer + bulk transfer |
| `frontend/src/pages/UsersPage.tsx` | User management (Admin-only) |
| `frontend/src/pages/AdminPage.tsx` | Tabbed admin panel |
| `frontend/src/App.tsx` | Modify: add QueryClientProvider, AuthProvider, /login route |
| `frontend/src/App.css` | Create: minimal reset / empty |

---

## Verification Checklist

1. Start backend (`dotnet run` in `backend/`) and frontend (`npm start` in `frontend/`); no console errors on load
2. Navigate to root `/` → redirected to `/login`
3. Login with admin credentials → redirected to `/dashboard`
4. Navigate to `/items`, search by keyword → results load; click row → detail drawer shows stock and QR code
5. Complete a **receive** flow: find component → pick location → enter qty → verify stock count increments
6. Complete a **gather** flow: pick component with stock → pick location from its current stock → reduce qty → verify
7. Complete a **transfer**: select component, source location, destination location, qty → verify
8. Navigate to `/admin`: cycle through all tabs and create, edit, delete each entity; verify backend validation errors surface as Ant Design notifications
9. Attempt `/admin` or `/users` as a non-admin user → verify redirect to `/dashboard`
10. Expire or delete the access token from `localStorage`; make a navigation-triggered API call → verify silent refresh completes without a re-login prompt
11. Logout → verify redirect to `/login` and both tokens cleared from `localStorage`

---

## Key Decisions

- **TypeScript**: The frontend is implemented in TypeScript and all new work should use `.ts`/`.tsx` files.
- **Ant Design over Tailwind/Shadcn**: `package.json` already commits to Ant Design v5; this plan is consistent with that choice
- **`pages/` at `frontend/src/pages/`**: matches the existing workspace directory layout.
- **Login route outside Layout**: `App.tsx` must be modified so `/login` renders without the sidebar/header `<Layout>` wrapper
- **ReadOnly role**: sees all views but all write buttons (Add, Edit, Delete, Submit forms) are hidden or disabled via `isAdmin` / role checks; no separate ReadOnly-specific page set needed
