# Backend & Database Development Tasks

> Warehouse Stock Management System — Backend & DB task breakdown.
> Architecture: ASP.NET Core 8.0, Clean Architecture, Mediator CQRS, EF Core (PostgreSQL), JWT Auth.
> Scope: Single-tenant. EF Core code-first migrations (discard create_db.sql).

---

## Phase 1: Foundation — Model Cleanup & Project Configuration

> Goal: Clean up the dual-model mess, fix bugs, and establish a solid project foundation.
> No dependencies — do this first.

### Task 1.1: Remove legacy models and DbContext
- Delete `backend/Models/models.cs` (old Area, Shelf, Item, Location, UserAccount, AuditLog, ComponentParameter classes)
- Delete `backend/Data/DbContext.cs` (old `StorageDbContext`)
- Remove all references to `StorageDbContext` from `Program.cs` and `ItemController.cs`
- Files: `backend/Models/models.cs`, `backend/Data/DbContext.cs`, `backend/Program.cs`, `backend/Controllers/ItemController.cs`

### Task 1.2: Fix ComponentType model bug
- The `Type` property in `backend/Models/ComponentType.cs` references the class itself instead of the `ComponentType` enum from `Enums.cs`
- Rename the enum in `Enums.cs` (e.g., to `ComponentPackageType`) to avoid name collision, or rename the property
- Ensure `Type` maps to the correct enum defined in domain model (SMD, Through-hole, QFP, SOIC, DIP, etc.)
- Files: `backend/Models/ComponentType.cs`, `backend/Models/Enums.cs`

### Task 1.3: Clean up WarehouseLocation model
- Remove duplicate properties from `backend/Models/WarehouseLocation.cs` that belong in `StockLocation.cs`: `BatchCode`, `ExpiryDate`, `Quantity`
- Verify remaining properties match domain model (Id, ShelfId, Name, Code, Description, BinX, BinY, Depth, Width, Height, Volume, IsReserved, IsActive, audit fields)
- Files: `backend/Models/WarehouseLocation.cs`

### Task 1.4: Add missing domain models
- Create `backend/Models/User.cs` — fields per domain model: Id (Guid), Username, Email, PasswordHash, Role (enum: Admin/User/ReadOnly), FirstName, LastName, LastLoginAt, IsActive, audit fields
- Create `backend/Models/AuditLog.cs` — fields: Id (long), UserId, Action, EntityType, EntityId (Guid), OldValues (string/JSON), NewValues (string/JSON), IpAddress, UserAgent, Timestamp
- Create `backend/Models/RefreshToken.cs` — fields: Id, UserId, Token (hashed), DeviceFingerprint, ExpiresAt, CreatedAt, RevokedAt, IsRevoked
- Add `UserRole` enum to `backend/Models/Enums.cs` (Admin, User, ReadOnly)
- Files: `backend/Models/User.cs`, `backend/Models/AuditLog.cs`, `backend/Models/RefreshToken.cs`, `backend/Models/Enums.cs`

### Task 1.5: Update ApplicationDbContext with all entities
- Register new DbSets: `Users`, `AuditLogs`, `RefreshTokens`
- Add Fluent API configuration in `OnModelCreating` for:
  - Unique constraints (User.Username, User.Email, Supplier.Code, etc.)
  - Index definitions matching domain model specs
  - Relationship configurations (cascade behaviors, required FKs)
  - Value conversions for enums
- Files: `backend/Data/ApplicationDbContext.cs`

### Task 1.6: Project configuration files
- Verify/create `backend/backend.csproj` with required NuGet packages: Mediator, FluentValidation, AutoMapper, EF Core, Npgsql, BCrypt.Net, Serilog, JWT
- Create `backend/appsettings.json` with sections: ConnectionStrings (PostgreSQL), JwtSettings (Issuer, Audience, SecretKey, AccessTokenExpirationMinutes, RefreshTokenExpirationDays), Serilog config
- Create `backend/appsettings.Development.json` with local dev overrides
- Files: `backend/backend.csproj`, `backend/appsettings.json`, `backend/appsettings.Development.json`

### Task 1.7: Generate initial EF Core migration
- Run `dotnet ef migrations add InitialCreate` to generate migration from current models
- Verify generated migration matches domain model expectations
- Run `dotnet ef database update` to apply
- Discard or archive `backend/create_db.sql` (no longer the source of truth)
- Files: `backend/Data/Migrations/` (generated)

---

## Phase 2: Infrastructure Layer — Repositories, Unit of Work, Base Services

> Goal: Establish the data access patterns used by all features.
> Depends on: Phase 1

### Task 2.1: Create generic repository interface and implementation
- Create `backend/Data/Repositories/IRepository.cs` — generic `IRepository<T>` interface with: GetByIdAsync, GetAllAsync, FindAsync (predicate), AddAsync, Update, Remove, ExistsAsync
- Create `backend/Data/Repositories/Repository.cs` — EF Core implementation using `ApplicationDbContext`
- Include soft-delete filtering (global query filter for `IsActive`)
- Files: `backend/Data/Repositories/IRepository.cs`, `backend/Data/Repositories/Repository.cs`

### Task 2.2: Create Unit of Work pattern
- Create `backend/Data/IUnitOfWork.cs` — interface exposing repository properties for each aggregate + `SaveChangesAsync()`
- Create `backend/Data/UnitOfWork.cs` — implementation wrapping `ApplicationDbContext` with lazy repository instantiation
- Register as scoped service in DI
- Files: `backend/Data/IUnitOfWork.cs`, `backend/Data/UnitOfWork.cs`

### Task 2.3: Create entity-specific repositories (where needed)
- Create `backend/Data/Repositories/IComponentRepository.cs` — extends IRepository with: GetByTypeAsync, GetBySupplierAsync, SearchAsync (name, partNumber, manufacturer filters)
- Create `backend/Data/Repositories/ComponentRepository.cs` — implementation with optimized queries
- Create `backend/Data/Repositories/IStockLocationRepository.cs` — extends IRepository with: GetByLocationAsync, GetByComponentAsync, GetStockSummaryAsync
- Create `backend/Data/Repositories/StockLocationRepository.cs`
- Create `backend/Data/Repositories/IAuditLogRepository.cs` — extends IRepository with: GetByEntityAsync, GetByUserAsync, GetByDateRangeAsync
- Create `backend/Data/Repositories/AuditLogRepository.cs`
- Files: `backend/Data/Repositories/` (6 files)

### Task 2.4: Create AutoMapper profiles
- Create `backend/Helpers/MappingProfiles/WarehouseMappingProfile.cs` — Area, Shelf, Location entity ↔ DTO mappings
- Create `backend/Helpers/MappingProfiles/ComponentMappingProfile.cs` — Component, ComponentType, ComponentCategory, Supplier mappings
- Create `backend/Helpers/MappingProfiles/UserMappingProfile.cs` — User entity ↔ DTO (exclude PasswordHash from response DTOs)
- Register AutoMapper in `Program.cs`
- Files: `backend/Helpers/MappingProfiles/` (3 files), `backend/Program.cs`

### Task 2.5: Create shared DTOs and response models
- Create `backend/Helpers/DTOs/` folder with request/response DTOs for each domain:
  - `AreaDtos.cs` — CreateAreaRequest, UpdateAreaRequest, AreaResponse
  - `ShelfDtos.cs` — CreateShelfRequest, UpdateShelfRequest, ShelfResponse
  - `LocationDtos.cs` — CreateLocationRequest, UpdateLocationRequest, LocationResponse
  - `ComponentDtos.cs` — CreateComponentRequest, UpdateComponentRequest, ComponentResponse, ComponentSearchRequest
  - `ComponentTypeDtos.cs` — CreateComponentTypeRequest, UpdateComponentTypeRequest, ComponentTypeResponse
  - `StockDtos.cs` — ReceiveStockRequest, TransferStockRequest, GatherStockRequest, StockLevelResponse
  - `UserDtos.cs` — CreateUserRequest, UpdateUserRequest, UserResponse (no password in response)
  - `AuthDtos.cs` — LoginRequest, LoginResponse (tokens), RefreshTokenRequest, TokenResponse
  - `CommonDtos.cs` — PaginatedResponse<T>, ErrorResponse, PagedQuery (page, pageSize, sortBy, sortDirection)
- Files: `backend/Helpers/DTOs/` (9 files)

### Task 2.6: Create shared FluentValidation validators
- Create `backend/Helpers/Validators/` folder:
  - Validators for each request DTO (e.g., CreateAreaRequestValidator, LoginRequestValidator)
  - Common rules: required fields, string length limits, enum range validation, GUID format
- Register FluentValidation in `Program.cs` (assembly scanning)
- Files: `backend/Helpers/Validators/` (multiple files), `backend/Program.cs`

---

## Phase 3: Authentication & Authorization

> Goal: Implement JWT auth with refresh tokens, user management, role-based access.
> Depends on: Phase 2

### Task 3.1: Implement auth service
- Create `backend/Services/Auth/IAuthService.cs` — interface: LoginAsync, RefreshTokenAsync, RevokeTokenAsync, ValidateTokenAsync
- Create `backend/Services/Auth/AuthService.cs` — implementation:
  - BCrypt password verification (work factor 12)
  - JWT access token generation (configurable 15-30 min expiry)
  - Refresh token generation, hashing, and storage in DB
  - Refresh token rotation (old token revoked on use)
  - Device fingerprint binding
- Files: `backend/Services/Auth/IAuthService.cs`, `backend/Services/Auth/AuthService.cs`

### Task 3.2: Implement JWT token helper
- Create `backend/Helpers/JwtTokenHelper.cs` — static/DI helper:
  - GenerateAccessToken(User, JwtSettings) → string
  - GenerateRefreshToken() → string
  - GetPrincipalFromExpiredToken(string token) → ClaimsPrincipal
  - HashToken(string token) → string (SHA-256)
- Load JwtSettings from appsettings.json via IOptions pattern
- Files: `backend/Helpers/JwtTokenHelper.cs`

### Task 3.3: Create auth controller
- Create `backend/Controllers/Users/AuthController.cs`:
  - `POST /api/auth/login` — authenticate, return access + refresh tokens
  - `POST /api/auth/refresh` — rotate refresh token, return new pair
  - `POST /api/auth/logout` — revoke refresh token
  - `GET /api/auth/me` — return current user profile (from JWT claims)
- All endpoints return standardized response format
- Files: `backend/Controllers/Users/AuthController.cs`

### Task 3.4: Create user management controller
- Create `backend/Controllers/Users/UserController.cs`:
  - `GET /api/users` — list users (Admin only), paginated
  - `GET /api/users/{id}` — get user by ID (Admin or self)
  - `POST /api/users` — create user (Admin only), hash password with BCrypt
  - `PUT /api/users/{id}` — update user (Admin only)
  - `DELETE /api/users/{id}` — soft-delete user (Admin only)
- Apply `[Authorize(Roles = "Admin")]` where appropriate
- Files: `backend/Controllers/Users/UserController.cs`

### Task 3.5: Configure auth middleware in Program.cs
- Ensure JWT bearer authentication is properly configured with validation parameters
- Add authorization policies for Admin, User, ReadOnly roles
- Add `UseAuthentication()` and `UseAuthorization()` in correct middleware order
- Files: `backend/Program.cs`

---

## Phase 4: Warehouse Structure Management (Areas, Shelves, Locations)

> Goal: Full CRUD for the warehouse hierarchy. Admin-only operations.
> Depends on: Phase 2. Parallel with Phase 3.

### Task 4.1: Area Mediator commands and queries
- Create `backend/Controllers/Areas/` handlers using Mediator CQRS:
  - `CreateAreaCommand` + handler — validate, create, audit log
  - `UpdateAreaCommand` + handler — validate, update, audit log
  - `DeleteAreaCommand` + handler — soft-delete, check for child shelves, audit log
  - `GetAreaByIdQuery` + handler — include shelf count
  - `GetAreasQuery` + handler — paginated, filterable by ZoneType, IsActive
- Files: `backend/Controllers/Areas/` (commands, queries, handlers — ~5 files)

### Task 4.2: Area controller
- Create `backend/Controllers/Areas/AreaController.cs`:
  - `GET /api/areas` — list areas (paginated)
  - `GET /api/areas/{id}` — get area with shelves
  - `POST /api/areas` — create area (Admin)
  - `PUT /api/areas/{id}` — update area (Admin)
  - `DELETE /api/areas/{id}` — soft-delete area (Admin)
- Files: `backend/Controllers/Areas/AreaController.cs`

### Task 4.3: Shelf Mediator commands and queries
- Create handlers for shelf CRUD following same pattern as Areas
- Include validation: AreaId must exist, CapacityCount > 0
- Queries should include location count per shelf
- Files: `backend/Controllers/Shelves/` (~5 files)

### Task 4.4: Shelf controller
- Create `backend/Controllers/Shelves/ShelfController.cs`:
  - `GET /api/shelves` — list (filterable by AreaId)
  - `GET /api/shelves/{id}` — get shelf with locations
  - `POST /api/shelves` — create (Admin)
  - `PUT /api/shelves/{id}` — update (Admin)
  - `DELETE /api/shelves/{id}` — soft-delete (Admin), check for child locations
- Files: `backend/Controllers/Shelves/ShelfController.cs`

### Task 4.5: Location Mediator commands and queries
- Create handlers for location CRUD
- Include validation: ShelfId must exist, BinX/BinY uniqueness per shelf
- Queries should include stock summary per location
- Files: `backend/Controllers/Locations/` (~5 files)

### Task 4.6: Location controller
- Create `backend/Controllers/Locations/LocationController.cs`:
  - `GET /api/locations` — list (filterable by ShelfId, AreaId)
  - `GET /api/locations/{id}` — get location with current stock
  - `POST /api/locations` — create (Admin)
  - `PUT /api/locations/{id}` — update (Admin)
  - `DELETE /api/locations/{id}` — soft-delete (Admin), check for stock at location
- Files: `backend/Controllers/Locations/LocationController.cs`

---

## Phase 5: Component & Inventory Management

> Goal: CRUD for component types, categories, suppliers, and inventory operations (receive, gather, transfer).
> Depends on: Phase 2, Phase 4 (locations must exist for stock operations).

### Task 5.1: Component category CRUD
- Mediator handlers + controller for ComponentCategory:
  - CRUD operations with hierarchical parent-child support (up to 5 levels)
  - Validate CategoryLevel and ParentCategoryId consistency
- Controller: `GET/POST/PUT/DELETE /api/component-categories`
- Files: `backend/Controllers/Inventory/ComponentCategoryController.cs`, handlers

### Task 5.2: Component type CRUD
- Mediator handlers + controller for ComponentType:
  - CRUD with CategoryId FK validation
  - Search by PartNumber, Manufacturer, StockSystemCode
- Controller: `GET/POST/PUT/DELETE /api/component-types`
- Files: `backend/Controllers/Inventory/ComponentTypeController.cs`, handlers

### Task 5.3: Supplier CRUD
- Mediator handlers + controller for Supplier:
  - CRUD with unique Code constraint enforcement
- Controller: `GET/POST/PUT/DELETE /api/suppliers`
- Files: `backend/Controllers/Inventory/SupplierController.cs`, handlers

### Task 5.4: Component (inventory item) CRUD
- Rewrite `ItemController.cs` → move to `backend/Controllers/Inventory/ComponentController.cs`
- Use ApplicationDbContext and new models
- Mediator handlers:
  - CreateComponentCommand — validate ComponentTypeId, optional SupplierId
  - UpdateComponentCommand — update metadata (not quantity — that's via stock ops)
  - GetComponentQuery — include type info, stock locations, supplier
  - SearchComponentsQuery — filter by type, category, manufacturer, partNumber, supplier; paginated
- Controller: `GET/POST/PUT/DELETE /api/components`
- Delete old `backend/Controllers/ItemController.cs`
- Files: `backend/Controllers/Inventory/ComponentController.cs`, handlers

### Task 5.5: Stock operations service
- Create `backend/Services/Stock/IStockService.cs` — interface:
  - ReceiveStockAsync(componentId, locationId, quantity, batchCode?, expiryDate?) — adds stock to location
  - GatherStockAsync(componentId, locationId, quantity) — removes stock from location
  - TransferStockAsync(componentId, fromLocationId, toLocationId, quantity) — move between locations
  - GetStockLevelsAsync(componentId) — returns all locations + quantities for a component
  - GetLocationInventoryAsync(locationId) — returns all components at a location
- Create `backend/Services/Stock/StockService.cs` — implementation:
  - Update Component.QuantityOnHand on receive/gather
  - Create/update StockLocation records
  - Validate sufficient quantity before gather/transfer
  - All operations wrapped in transactions
  - Audit log every operation
- Files: `backend/Services/Stock/IStockService.cs`, `backend/Services/Stock/StockService.cs`

### Task 5.6: Stock operations controller
- Create `backend/Controllers/Inventory/StockController.cs`:
  - `POST /api/stock/receive` — receive stock at a location
  - `POST /api/stock/gather` — gather (pick) stock from a location
  - `POST /api/stock/transfer` — transfer between locations
  - `GET /api/stock/component/{id}` — stock levels across all locations
  - `GET /api/stock/location/{id}` — inventory at a specific location
  - `POST /api/stock/bulk-transfer` — bulk transfer multiple components between locations
- Files: `backend/Controllers/Inventory/StockController.cs`

---

## Phase 6: Search Service

> Goal: Implement search/filter functionality across components and locations.
> Depends on: Phase 5.

### Task 6.1: Implement search service
- Create `backend/Services/Search/ISearchService.cs` — interface:
  - SearchComponentsAsync(query, filters, pagination) — full-text + attribute search
  - SearchLocationsAsync(query, filters) — find locations by area, shelf, stock level
- Create `backend/Services/Search/SearchService.cs` — implementation:
  - Build dynamic LINQ queries from filter parameters
  - Support searching by: component name, part number, manufacturer, category, supplier, batch code
  - Support location filtering by: area, shelf, occupancy status
- Files: `backend/Services/Search/ISearchService.cs`, `backend/Services/Search/SearchService.cs`

### Task 6.2: Search controller
- Create `backend/Controllers/Inventory/SearchController.cs`:
  - `GET /api/search/components?q=&type=&manufacturer=&category=` — search components
  - `GET /api/search/locations?areaId=&shelfId=&hasStock=` — search locations
- Files: `backend/Controllers/Inventory/SearchController.cs`

---

## Phase 7: Audit Logging & Middleware

> Goal: Implement audit trail for all inventory operations and global error handling.
> Depends on: Phase 2. Parallel with Phases 3-6 (wire up as each phase completes).

### Task 7.1: Implement audit log service
- Create `backend/Services/IAuditService.cs` — interface: LogAsync(userId, action, entityType, entityId, oldValues?, newValues?, ipAddress?, userAgent?)
- Create `backend/Services/AuditService.cs` — implementation:
  - Write to AuditLog table via repository
  - Serialize old/new values as JSON
  - Capture IP address from HttpContext
  - Register as scoped service
- Files: `backend/Services/IAuditService.cs`, `backend/Services/AuditService.cs`

### Task 7.2: Create audit log query controller
- Create `backend/Controllers/AuditLogs/AuditLogController.cs`:
  - `GET /api/audit-logs` — list audit logs (Admin only), paginated, filterable by entity type, user, date range
  - `GET /api/audit-logs/{entityType}/{entityId}` — get audit trail for specific entity
- Files: `backend/Controllers/AuditLogs/AuditLogController.cs`

### Task 7.3: Global error handling middleware
- Create `backend/Middleware/ErrorHandlingMiddleware.cs`:
  - Catch unhandled exceptions
  - Return standardized ErrorResponse JSON
  - Log errors via Serilog
  - Don't leak stack traces in production
- Register in `Program.cs` pipeline
- Files: `backend/Middleware/ErrorHandlingMiddleware.cs`, `backend/Program.cs`

### Task 7.4: Request logging middleware
- Create `backend/Middleware/RequestLoggingMiddleware.cs`:
  - Log request method, path, status code, duration
  - Use Serilog structured logging
- Register in `Program.cs`
- Files: `backend/Middleware/RequestLoggingMiddleware.cs`, `backend/Program.cs`

### Task 7.5: Configure Serilog
- Set up Serilog in `Program.cs`:
  - Console sink (development)
  - PostgreSQL sink (production) — structured JSON logging
  - Enrich with request context (CorrelationId, UserId)
- Files: `backend/Program.cs`, `backend/appsettings.json`

---

## Phase 8: Health Check & API Finalization

> Goal: Health endpoint, Swagger polish, CORS, final Program.cs cleanup.
> Depends on: All previous phases.

### Task 8.1: Health check endpoint
- Add `GET /health` endpoint using ASP.NET Core health checks
- Include PostgreSQL connectivity check
- Files: `backend/Program.cs`

### Task 8.2: Finalize Program.cs and DI registration
- Ensure all services, repositories, middleware registered in correct order:
  1. Serilog
  2. DbContext
  3. Repositories + Unit of Work
  4. Services (Auth, Stock, Search, Audit)
  5. AutoMapper
  6. FluentValidation
  7. MediatR
  8. Authentication + Authorization
  9. CORS
  10. Middleware (error handling, request logging)
  11. Controllers
  12. Swagger
  13. Health checks
- Files: `backend/Program.cs`

### Task 8.3: Swagger/OpenAPI configuration
- Add XML comments to controllers for Swagger docs
- Configure Swagger to include JWT auth header input
- Group endpoints by tag/controller
- Files: `backend/Program.cs`

### Task 8.4: Clean up empty directories
- Remove `backend/Data/Context/` if unused
- Confirm all controller subdirectories have their files
- Remove `backend/Controllers/Reports/` if not in scope for initial release
- Files: directory cleanup

---

## Verification Checklist

After each phase, verify:
1. `dotnet build` succeeds with no errors
2. `dotnet ef migrations` can generate/apply without issues
3. All endpoints return correct HTTP status codes (200, 201, 400, 401, 403, 404, 500)
4. JWT authentication works end-to-end (login → access protected endpoint → refresh → logout)
5. Audit logs are created for all create/update/delete operations
6. Stock operations maintain quantity consistency (no negative stock, totals match)
7. Soft deletes filter correctly in all queries
8. Pagination works on all list endpoints
9. FluentValidation returns clear error messages on bad input
10. Swagger UI shows all endpoints with correct request/response schemas