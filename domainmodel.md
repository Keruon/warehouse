  Domain Model

  1. High-Level Entity Overview

  ┌────────────────────────────────────────────────────────────────────────┐
  │                         WAREHOUSE HIERARCHY                            │
  │                                                                        │
  │    ┌─────────────────┐                                                 │
  │    │     AREA        │  1:Many                                         │
  │    │  (Physical zone)│          ┌───────────────────────────────────┐  │
  │    └────────┬────────┘          │     SHELF                         │  │
  │             │  1:Many           │  (Organized section)              │  │
  │             │                   └──────────┬────────────────────────┘  │
  │             │  1:Many                      │                           │
  │             ▼                              ▼                           │
  │    ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐       │
  │    │   SHELF         │  │   SHELF         │  │   SHELF         │       │
  │    │  (Contains)     │  │  (Contains)     │  │  (Contains)     │       │
  │    └────────┬────────┘  └────────┬────────┘  └────────┬────────┘       │
  │             │  1:Many            │  1:Many            │  1:Many        │
  │             ▼                    ▼                    ▼                │
  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐         │
  │  │  LOCATION       │  │  LOCATION       │  │  LOCATION       │         │
  │  │  (Physical slot)│  │  (Physical slot)│  │  (Physical slot)│         │
  │  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘         │
  │           │  0..N              │  0..N              │  0..N            │
  │           ▼                    ▼                     ▼                 │
  │    ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐       │
  │    │  COMPONENT      │  │  COMPONENT      │  │  COMPONENT      │       │
  │    │  Inventory item │  │  Inventory item │  │  Inventory item │       │
  │    └─────────────────┘  └─────────────────┘  └─────────────────┘       │
  │                                                                        │
  └────────────────────────────────────────────────────────────────────────┘

  2. Aggregates

  2.1 WarehouseArea Aggregate

  Manages physical zones within the warehouse facility.

  ┌─────────────┬──────────┬──────────┬─────────────────────────────────────────────────────────────────────────┐
  │    Field    │   Type   │ Nullable │                               Description                               │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ Id          │ Guid     │ No       │ Primary key                                                             │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ Name        │ String   │ No       │ Human-readable area name (e.g., "North", "Electronics", "Cold Storage") │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ Code        │ String   │ No       │ Short identifier for UI display                                         │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ Description │ String   │ Yes      │ Additional notes                                                        │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ FloorLevel  │ Int      │ Yes      │ Floor number for multi-story warehouses                                 │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ ZoneType    │ Enum     │ No       │ Type of zone (e.g., "Storage", "Production", "Shipping", "Returns")     │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ IsActive    │ Boolean  │ No       │ Soft delete flag                                                        │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ CreatedAt   │ DateTime │ No       │                                                                         │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ CreatedBy   │ Guid     │ No       │ User who created                                                        │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ ModifiedAt  │ DateTime │ No       │                                                                         │
  ├─────────────┼──────────┼──────────┼─────────────────────────────────────────────────────────────────────────┤
  │ ModifiedBy  │ Guid     │ No       │ User who last modified                                                  │
  └─────────────┴──────────┴──────────┴─────────────────────────────────────────────────────────────────────────┘

  2.2 WarehouseShelf Aggregate

  Organized sections within an area that group locations together.

  ┌───────────────┬──────────┬──────────┬────────────────────────────────────────────────────────────┐
  │     Field     │   Type   │ Nullable │                        Description                         │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Id            │ Guid     │ No       │ Primary key                                                │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ AreaId        │ Guid     │ No       │ Foreign key to WarehouseArea                               │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Name          │ String   │ No       │ Human-readable shelf name (e.g., "A1", "Component Rack 1") │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Code          │ String   │ No       │ Short identifier for UI display                            │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Description   │ String   │ Yes      │ Additional notes                                           │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ ShelfType     │ Enum     │ Yes      │ Type: "Mobile", "Static", "Rack", "Bin", "Pallet"          │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ CapacityCount │ Int      │ No       │ Maximum number of locations it can hold                    │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ WeightLimit   │ Decimal  │ Yes      │ Maximum weight in kg                                       │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ IsActive      │ Boolean  │ No       │ Soft delete flag                                           │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ CreatedAt     │ DateTime │ No       │                                                            │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ CreatedBy     │ Guid     │ No       │ User who created                                           │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ ModifiedAt    │ DateTime │ No       │                                                            │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ ModifiedBy    │ Guid     │ No       │ User who last modified                                     │
  └───────────────┴──────────┴──────────┴────────────────────────────────────────────────────────────┘

  Index:
  - AreaId (for area-level filtering)
  - AreaId + Name (unique)

  2.3 WarehouseLocation Aggregate

  Physical storage slots within a shelf where components are placed.

  ┌─────────────┬──────────┬──────────┬───────────────────────────────────────────────────────────┐
  │    Field    │   Type   │ Nullable │                        Description                        │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Id          │ Guid     │ No       │ Primary key                                               │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ ShelfId     │ Guid     │ No       │ Foreign key to WarehouseShelf                             │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Name        │ String   │ No       │ Human-readable location name (e.g., "A1-01", "Bin 3")     │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Code        │ String   │ No       │ Short identifier for UI display                           │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Description │ String   │ Yes      │ Additional notes                                          │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ BinX        │ Int      │ No       │ Coordinate X for picker systems                           │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ BinY        │ Int      │ No       │ Coordinate Y for picker systems                           │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Depth       │ Decimal  │ Yes      │ Depth in mm                                               │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Width       │ Decimal  │ Yes      │ Width in mm                                               │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Height      │ Decimal  │ Yes      │ Height in mm                                              │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Volume      │ Decimal  │ Yes      │ Calculated volume in mm³                                  │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ IsReserved  │ Boolean  │ No       │ Whether location is reserved for specific component types │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ IsActive    │ Boolean  │ No       │ Soft delete flag                                          │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ CreatedAt   │ DateTime │ No       │                                                           │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ CreatedBy   │ Guid     │ No       │ User who created                                          │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ ModifiedAt  │ DateTime │ No       │                                                           │
  ├─────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ ModifiedBy  │ Guid     │ No       │ User who last modified                                    │
  └─────────────┴──────────┴──────────┴───────────────────────────────────────────────────────────┘

  Index:
  - ShelfId (for shelf-level filtering)
  - ShelfId + Code (unique)
  - BinX + BinY (unique for grid coordinates)

  2.4 ComponentType Aggregate

  Standardized component definitions with type-specific parameters.

  ┌─────────────────┬──────────┬──────────┬────────────────────────────────────────────────┐
  │      Field      │   Type   │ Nullable │                  Description                   │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ Id              │ Guid     │ No       │ Primary key                                    │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ CategoryId      │ Guid     │ No       │ Foreign key to ComponentCategory               │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ CategoryName    │ String   │ Yes      │ Cached for performance                         │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ PartNumber      │ String   │ No       │ Manufacturer part number                       │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ StockSystemCode │ String   │ No       │ QR/Barcode readable code (SKU)                 │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ Manufacturer    │ String   │ No       │ Manufacturer name                              │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ SourcingOrigin  │ String   │ No       │ Country of origin                              │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ Type            │ Enum     │ No       │ SMD, Through-hole, QFP, SOIC, DIP, etc.        │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ PackageType     │ Enum     │ Yes      │ e.g., "0805", "1206", "1762", "QFP44"          │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ Footprint       │ String   │ No       │ PCB footprint code                             │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ SizeX           │ Decimal  │ No       │ Width in mm                                    │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ SizeY           │ Decimal  │ No       │ Length in mm                                   │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ SizeZ           │ Decimal  │ No       │ Height in mm                                   │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ MaxVoltage      │ Decimal  │ Yes      │ Maximum operating voltage in V                 │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ Value           │ String   │ Yes      │ Electrical value (e.g., "10kΩ", "100nF", "1A") │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ MinQty          │ Int      │ No       │ Minimum order quantity                         │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ PackagingUnit   │ Int      │ No       │ Quantity per tape/reel/bin (e.g., 3000 pcs)    │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ IsActive        │ Boolean  │ No       │ Soft delete flag                               │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ CreatedAt       │ DateTime │ No       │                                                │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ CreatedBy       │ Guid     │ No       │ User who created                               │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ ModifiedAt      │ DateTime │ No       │                                                │
  ├─────────────────┼──────────┼──────────┼────────────────────────────────────────────────┤
  │ ModifiedBy      │ Guid     │ No       │ User who last modified                         │
  └─────────────────┴──────────┴──────────┴────────────────────────────────────────────────┘

  Relationships:
  - ComponentCategory (parent-child)
  - Stored in StorageSystem table as references

  2.5 ComponentCategory Aggregate

  Top-level classification for component organization.

  ┌──────────────────┬──────────┬──────────┬───────────────────────────────────────────────────────────┐
  │      Field       │   Type   │ Nullable │                        Description                        │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Id               │ Guid     │ No       │ Primary key                                               │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Name             │ String   │ No       │ Human-readable category (e.g., "Resistors", "Capacitors") │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Code             │ String   │ No       │ Short identifier (e.g., "RES", "CAP")                     │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ Description      │ String   │ Yes      │ Category description                                      │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ CategoryLevel    │ Int      │ No       │ Hierarchical level (1-5)                                  │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ ParentCategoryId │ Guid     │ Yes      │ Null for top-level                                        │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ IsActive         │ Boolean  │ No       │ Soft delete flag                                          │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ CreatedAt        │ DateTime │ No       │                                                           │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ CreatedBy        │ Guid     │ No       │ User who created                                          │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ ModifiedAt       │ DateTime │ No       │                                                           │
  ├──────────────────┼──────────┼──────────┼───────────────────────────────────────────────────────────┤
  │ ModifiedBy       │ Guid     │ No       │ User who last modified                                    │
  └──────────────────┴──────────┴──────────┴───────────────────────────────────────────────────────────┘

  2.6 ComponentAggregate

  Individual inventory items that can be tracked.

  ┌────────────────────┬──────────┬──────────┬─────────────────────────────────────┐
  │       Field        │   Type   │ Nullable │             Description             │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ Id                 │ Guid     │ No       │ Primary key                         │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ ComponentTypeId    │ Guid     │ No       │ Foreign key to ComponentType        │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ ComponentTypeName  │ String   │ Yes      │ Cached for performance              │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ PartNumber         │ String   │ No       │ Specific part number                │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ BatchCode          │ String   │ Yes      │ Optional batch/lot identifier       │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ ExpiryDate         │ DateTime │ Yes      │ Optional expiry date for batch      │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ QuantityOnHand     │ Int      │ No       │ Current stock count                 │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ QuantityReserved   │ Int      │ No       │ Quantity reserved but not available │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ QuantityCommitted  │ Int      │ No       │ Committed to orders                 │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ MinimumStockLevel  │ Int      │ Yes      │ Reorder threshold                   │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ MaximumStockLevel  │ Int      │ Yes      │ Stock warning level                 │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ ReorderPoint       │ Int      │ Yes      │ Trigger reorder level               │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ UnitCost           │ Decimal  │ Yes      │ Average unit cost                   │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ SupplierId         │ Guid     │ Yes      │ First-in-first-out supplier         │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ SupplierCode       │ String   │ Yes      │ Supplier's identifier               │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ SupplierName       │ String   │ Yes      │ Supplier display name               │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ SupplierPartNumber │ String   │ Yes      │ Supplier's part number              │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ LastPriceChange    │ DateTime │ Yes      │                                     │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ SupplierLeadTime   │ Int      │ Yes      │ Days to delivery                    │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ LastPurchaseDate   │ DateTime │ Yes      │                                     │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ LastReceivedDate   │ DateTime │ Yes      │                                     │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ LastSoldDate       │ DateTime │ Yes      │                                     │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ IsActive           │ Boolean  │ No       │ Soft delete flag                    │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ CreatedAt          │ DateTime │ No       │                                     │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ CreatedBy          │ Guid     │ No       │ User who created                    │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ ModifiedAt         │ DateTime │ No       │                                     │
  ├────────────────────┼──────────┼──────────┼─────────────────────────────────────┤
  │ ModifiedBy         │ Guid     │ No       │ User who last modified              │
  └────────────────────┴──────────┴──────────┴─────────────────────────────────────┘

  Index:
  - ComponentTypeId
  - BatchCode (for tracking)
  - SupplierId (for supplier management)

  2.7 StockLocationAggregate

  Link between component inventory and physical locations.

  ┌─────────────┬──────────┬──────────┬──────────────────────────────────┐
  │    Field    │   Type   │ Nullable │           Description            │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ Id          │ Guid     │ No       │ Primary key                      │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ ComponentId │ Guid     │ No       │ Foreign key to Component         │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ LocationId  │ Guid     │ No       │ Foreign key to WarehouseLocation │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ BinX        │ Int      │ Yes      │ Coordinate X at location         │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ BinY        │ Int      │ Yes      │ Coordinate Y at location         │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ Quantity    │ Int      │ No       │ Quantity at this location        │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ LastUpdated │ DateTime │ No       │                                  │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ IsActive    │ Boolean  │ No       │ Soft delete flag                 │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ CreatedAt   │ DateTime │ No       │                                  │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ CreatedBy   │ Guid     │ No       │ User who created                 │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ ModifiedAt  │ DateTime │ No       │                                  │
  ├─────────────┼──────────┼──────────┼──────────────────────────────────┤
  │ ModifiedBy  │ Guid     │ No       │ User who last modified           │
  └─────────────┴──────────┴──────────┴──────────────────────────────────┘

  Index:
  - ComponentId (for component stock queries)
  - LocationId (for location inventory queries)

  2.8 Supplier Aggregate

  External vendors providing components.

  ┌───────────────┬──────────┬──────────┬────────────────────────────────────────────────────────────┐
  │     Field     │   Type   │ Nullable │                        Description                         │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Id            │ Guid     │ No       │ Primary key                                                │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Name          │ String   │ No       │ Supplier company name                                      │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ ContactName   │ String   │ Yes      │ Primary contact person                                     │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Email         │ String   │ Yes      │ Contact email                                              │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Phone         │ String   │ Yes      │ Contact phone                                              │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Address       │ String   │ Yes      │ Physical address                                           │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Website       │ String   │ Yes      │ Supplier URL                                               │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ IsActive      │ Boolean  │ No       │ Soft delete flag                                           │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ CreatedAt     │ DateTime │ No       │                                                            │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ CreatedBy     │ Guid     │ No       │ User who created                                           │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ ModifiedAt    │ DateTime │ No       │                                                            │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ ModifiedBy    │ Guid     │ No       │ User who last modified                                     │
  └───────────────┴──────────┴──────────┴────────────────────────────────────────────────────────────┘

  2.9 User Aggregate

  System users and authentication details.

  ┌───────────────┬──────────┬──────────┬────────────────────────────────────────────────────────────┐
  │     Field     │   Type   │ Nullable │                        Description                         │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Id            │ Guid     │ No       │ Primary key                                                │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Username      │ String   │ No       │ Unique login name                                          │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Email         │ String   │ No       │ Unique email address                                       │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ PasswordHash  │ String   │ No       │ Hashed password                                            │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Role          │ Enum     │ No       │ "Admin", "User", "ReadOnly"                                │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ FirstName     │ String   │ Yes      │                                                            │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ LastName      │ String   │ Yes      │                                                            │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ LastLoginAt   │ DateTime │ Yes      │ Timestamp of last successful login                         │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ IsActive      │ Boolean  │ No       │ Soft delete flag                                           │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ CreatedAt     │ DateTime │ No       │                                                            │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ ModifiedAt    │ DateTime │ No       │                                                            │
  └───────────────┴──────────┴──────────┴────────────────────────────────────────────────────────────┘

  2.10 AuditLog Aggregate

  Immutable record of all system modifications.

  ┌───────────────┬──────────┬──────────┬────────────────────────────────────────────────────────────┐
  │     Field     │   Type   │ Nullable │                        Description                         │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Id            │ Long     │ No       │ Primary key (Sequential)                                   │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ UserId        │ Guid     │ No       │ User performing the action                                 │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Action        │ String   │ No       │ e.g., "Create", "Update", "Delete", "Transfer"             │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ EntityType    │ String   │ No       │ e.g., "Component", "WarehouseLocation"                     │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ EntityId      │ Guid     │ No       │ ID of the affected entity                                  │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ OldValues     │ Json     │ Yes      │ Snapshot of data before change                             │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ NewValues     │ Json     │ Yes      │ Snapshot of data after change                              │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ IpAddress     │ String   │ Yes      │ Client IP                                                  │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ UserAgent     │ String   │ Yes      │ Client Browser/Device info                                 │
  ├───────────────┼──────────┼──────────┼────────────────────────────────────────────────────────────┤
  │ Timestamp     │ DateTime │ No       │ When the action occurred                                   │
  └───────────────┴──────────┴──────────┴────────────────────────────────────────────────────────────┘

  Index:
  - UserId + Timestamp
  - EntityType + EntityId

  3. Relationships

  3.1 WarehouseArea to WarehouseShelf

  WarehouseArea 1 —— Many WarehouseShelf
  - Cardinality: One-to-Many
  - Foreign Key: WarehouseShelf.AreaId
  - Description: An area contains multiple shelves. Each shelf belongs to exactly one area.

  3.2 WarehouseShelf to WarehouseLocation

  WarehouseShelf 1 —— Many WarehouseLocation
  - Cardinality: One-to-Many
  - Foreign Key: WarehouseLocation.ShelfId
  - Description: A shelf contains multiple locations. Each location belongs to exactly one shelf.

  3.3 ComponentType to Component

  ComponentType 1 —— Many Component
  - Cardinality: One-to-Many
  - Foreign Key: Component.ComponentTypeId
  - Description: One component type definition can have multiple inventory instances. Each component references exactly one type.

  3.4 Component to StockLocation

  Component 1 —— Many StockLocation
  - Cardinality: One-to-Many
  - Foreign Key: StockLocation.ComponentId
  - Description: A component (batch/instance) can be stored across multiple locations. Each stock location records a specific component's quantity at a specific place.

  3.5 ComponentCategory to Component

  ComponentCategory 1 —— Many Component
  - Cardinality: One-to-Many
  - Foreign Key: Component.CategoryId (via ComponentType)
  - Description: Categories organize component types hierarchically.

  3.6 Supplier to Component

  Supplier 1 —— Many Component
  - Cardinality: One-to-Many
  - Foreign Key: Component.SupplierId
  - Description: A supplier provides specific component batches.

  3.7 User to AuditLog

  User 1 —— Many AuditLog
  - Cardinality: One-to-Many
  - Foreign Key: AuditLog.UserId
  - Description: One user performs many audited actions.

  4. Constraints

  4.1 Primary Keys

  - WarehouseArea: Id (GUID)
  - WarehouseShelf: Id (GUID)
  - WarehouseLocation: Id (GUID)
  - ComponentType: Id (GUID)
  - ComponentCategory: Id (GUID)
  - Component: Id (GUID)
  - StockLocation: Id (GUID)
  - Supplier: Id (GUID)
  - User: Id (GUID)
  - AuditLog: Id (Long)

  4.2 Foreign Keys

  ┌───────────────────┬────────────────┬────────────────────────────────┐
  │    From Table     │    To Table    │             Column             │
  ├───────────────────┼────────────────┼────────────────────────────────┤
  │ WarehouseShelf    │ WarehouseArea  │ AreaId                         │
  ├───────────────────┼────────────────┼────────────────────────────────┤
  │ WarehouseLocation │ WarehouseShelf │ ShelfId                        │
  ├───────────────────┼────────────────┼────────────────────────────────┤
  │ Component         │ ComponentType  │ ComponentTypeId                │
  ├───────────────────┼────────────────┼────────────────────────────────┤
  │ ComponentCategory │ Component      │ (via ComponentType.CategoryId) │
  ├───────────────────┼────────────────┼────────────────────────────────┤
  │ Component         │ Supplier       │ SupplierId                     │
  ├───────────────────┼────────────────┼────────────────────────────────┤
  │ AuditLog          │ User           │ UserId                         │
  └───────────────────┴────────────────┴────────────────────────────────┘

  4.3 Unique Constraints

  - WarehouseArea: Name + Code + ZoneType + FloorLevel
  - WarehouseShelf: AreaId + Name + Code (unique within area)
  - WarehouseLocation: ShelfId + Name + Code (unique within shelf)
  - User: Username
  - User: Email

  4.4 Check Constraints


  - WarehouseArea: ZoneType IN ('Storage', 'Production', 'Shipping', 'Returns', 'Returns', 'Maintenance')
  - ComponentType: Type IN ('SMD', 'Through-hole', 'QFP', 'SOIC', 'DIP', 'Other')
  - StockLocation: Quantity >= 0

  4.5 Soft Delete

  - All aggregates include IsActive boolean flag
  - Soft deletes preserve historical data
  - Query layer filters out deleted records unless using 'IncludeDeleted' flag

  5. Business Rules

  5.1 Area Management

  1. No Overlap: Each location can only belong to one shelf, each shelf to one area.
  2. Area Capacity: No limit on shelves per area (configurable).
  3. Zone Types: Reserved zones have specific business rules (cold storage, hazardous, etc.).

  5.2 Shelf Management

  1. Shelf Naming: Shelves in same area should be unique (e.g., A1, A2, A3).
  2. Capacity Enforcement: Cannot create location if exceeds shelf capacity.
  3. Weight Limits: Track shelf weight limits for safety compliance.

  5.3 Location Management

  1. Location Naming: Locations in same shelf should be unique (e.g., A1-01, A1-02).
  2. Grid System: BinX/BinY coordinates enable automated picking systems.
  3. Reserved Locations: Can be marked as reserved for specific component types.

  5.4 Component Management

  1. One-to-Many Stock: Multiple stock locations can hold same component.
  2. Batch Tracking: BatchCode is optional but critical for expiry management.
  3. Stock Level: Tracks current, reserved, and committed quantities separately.
  4. Supplier Management: First-in-first-out supplier tracking.
  5. Costing: Average unit cost calculation.

  5.5 Stock Management

  1. Negative Quantities: Not allowed (Quantity >= 0).
  2. Stock Transfer: Requires stock location records.
  3. Batch Expiry: Optional expiry date for FIFO/FEFO tracking.

  6. 