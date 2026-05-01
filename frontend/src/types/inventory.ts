export type PaginatedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
};

export type ComponentSearchParams = {
  q?: string;
  typeId?: string;
  manufacturer?: string;
  categoryId?: string;
  supplierId?: string;
  partNumber?: string;
  page?: number;
  pageSize?: number;
};

export type LocationSearchParams = {
  q?: string;
  areaId?: string;
  shelfId?: string;
  hasStock?: boolean;
};

export type ComponentResponse = {
  id: string;
  componentTypeId: string;
  componentTypeName?: string;
  categoryId: string;
  partNumber: string;
  batchCode?: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityCommitted: number;
  supplierId?: string;
  supplierCode?: string;
  supplierName?: string;
  unitCost: number;
  isActive: boolean;
};

export type ComponentTypeResponse = {
  id: string;
  categoryId: string;
  kind: string;
  value: string;
  footprint?: string;
  type: string;
  description?: string;
  isActive: boolean;
};

export type ComponentCategoryResponse = {
  id: string;
  name: string;
  parentId?: string;
  description?: string;
  isActive: boolean;
};

export type SupplierResponse = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
};

export type LocationResponse = {
  id: string;
  shelfId: string;
  areaId: string;
  name: string;
  code: string;
  description?: string;
  binX: number;
  binY: number;
  depth?: number;
  width?: number;
  height?: number;
  volume?: number;
  isReserved: boolean;
  isActive: boolean;
  currentStockQuantity: number;
};

export type StockLevelResponse = {
  componentId: string;
  locationId: string;
  quantity: number;
  batchCode?: string;
  expiryDate?: string;
};

export type CreateComponentRequest = {
  componentTypeId: string;
  partNumber: string;
  batchCode?: string;
  supplierId?: string;
  supplierPartNumber?: string;
  unitCost: number;
  minimumStockLevel?: number;
  maximumStockLevel?: number;
  reorderPoint?: number;
};

export type UpdateComponentRequest = CreateComponentRequest & {
  isActive: boolean;
};

export type AreaResponse = {
  id: string;
  name: string;
  code: string;
  zoneType: string;
  floorLevel: number;
  description?: string;
  isActive: boolean;
  shelfCount: number;
};

export type ShelfResponse = {
  id: string;
  areaId: string;
  name: string;
  code: string;
  weightLimitKg?: number;
  description?: string;
  isActive: boolean;
  locationCount: number;
};

export type LocationInventoryItemResponse = {
  componentId: string;
  partNumber: string;
  quantity: number;
  batchCode?: string;
  expiryDate?: string;
};

export type ReceiveStockRequest = {
  componentId: string;
  locationId: string;
  quantity: number;
  batchCode?: string;
  expiryDate?: string;
};

export type GatherStockRequest = {
  componentId: string;
  locationId: string;
  quantity: number;
};

export type TransferStockRequest = {
  componentId: string;
  fromLocationId: string;
  toLocationId: string;
  quantity: number;
};

export type BulkTransferItemRequest = {
  componentId: string;
  quantity: number;
};

export type BulkTransferRequest = {
  fromLocationId: string;
  toLocationId: string;
  items: BulkTransferItemRequest[];
};

export type UserRole = 'Admin' | 'User' | 'ReadOnly';

export type UserResponse = {
  id: string;
  username: string;
  email: string;
  role: UserRole;
  firstName?: string;
  lastName?: string;
  lastLoginAt?: string;
  isActive: boolean;
};

export type CreateUserRequest = {
  username: string;
  email: string;
  password: string;
  role: UserRole;
  firstName?: string;
  lastName?: string;
};

export type UpdateUserRequest = {
  email: string;
  role: UserRole;
  firstName?: string;
  lastName?: string;
  isActive: boolean;
};

export type ZoneType = 'Storage' | 'Production' | 'Shipping' | 'Returns' | 'Maintenance';

export type CreateAreaRequest = {
  name: string;
  code: string;
  zoneType: ZoneType;
  floorLevel: number;
  description?: string;
};

export type UpdateAreaRequest = CreateAreaRequest & {
  isActive: boolean;
};

export type CreateShelfRequest = {
  areaId: string;
  name: string;
  code: string;
  weightLimitKg?: number;
  description?: string;
};

export type UpdateShelfRequest = CreateShelfRequest & {
  isActive: boolean;
};

export type CreateLocationRequest = {
  shelfId: string;
  name: string;
  code: string;
  description?: string;
  binX: number;
  binY: number;
  depth?: number;
  width?: number;
  height?: number;
  volume?: number;
  isReserved: boolean;
};

export type UpdateLocationRequest = CreateLocationRequest & {
  isActive: boolean;
};

export type CreateComponentCategoryRequest = {
  name: string;
  parentId?: string;
  description?: string;
};

export type UpdateComponentCategoryRequest = CreateComponentCategoryRequest & {
  isActive: boolean;
};

export type ComponentPackageType = 'SMD' | 'ThroughHole' | 'QFP' | 'SOIC' | 'DIP' | 'Other';

export type CreateComponentTypeRequest = {
  categoryId: string;
  kind: string;
  value: string;
  footprint?: string;
  type: ComponentPackageType;
  description?: string;
};

export type UpdateComponentTypeRequest = CreateComponentTypeRequest & {
  isActive: boolean;
};

export type CreateSupplierRequest = {
  code: string;
  name: string;
};

export type UpdateSupplierRequest = CreateSupplierRequest & {
  isActive: boolean;
};

export type AuditLogResponse = {
  id: number;
  userId: string;
  action: string;
  entityType: string;
  entityId: string;
  oldValues?: string;
  newValues?: string;
  ipAddress?: string;
  userAgent?: string;
  timestamp: string;
};

export type AuditLogQuery = {
  page?: number;
  pageSize?: number;
  entityType?: string;
  userId?: string;
  fromUtc?: string;
  toUtc?: string;
};