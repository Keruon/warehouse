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
  name: string;
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