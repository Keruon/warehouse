-- =============================================
-- 1. Setup Enums (Based on Section 4.4 Check Constraints)
-- =============================================

-- Define valid Zone Types for WarehouseAreas
CREATE TYPE zone_type_enum AS ENUM ('Storage', 'Production', 'Shipping', 'Returns', 'Maintenance');

-- Define valid Component Types for ComponentTypes
CREATE TYPE component_type_enum AS ENUM ('SMD', 'ThroughHole', 'QFP', 'SOIC', 'DIP', 'Other');

-- =============================================
-- 2. Aggregates (Tables)
-- =============================================

-- ----------------------------------------------------------------
-- Table: WarehouseArea
-- ----------------------------------------------------------------
CREATE TABLE WarehouseArea (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(50) NOT NULL,
    ZoneType zone_type_enum NOT NULL,
    FloorLevel INTEGER NOT NULL CHECK (FloorLevel >= 0),
    Description TEXT,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL,
    ModifiedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ModifiedBy UUID NOT NULL,
    
    -- Section 4.3 Unique Constraint
    CONSTRAINT UQ_WarehouseArea_Name_Code_ZoneFloor UNIQUE (Name, Code, ZoneType, FloorLevel)
);
COMMENT ON TABLE WarehouseArea IS 'High-level zoning (e.g., Floor 1, Cold Storage)';

-- ----------------------------------------------------------------
-- Table: WarehouseShelf
-- ----------------------------------------------------------------
CREATE TABLE WarehouseShelf (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    AreaId UUID NOT NULL REFERENCES WarehouseArea(Id) ON DELETE CASCADE,
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(50) NOT NULL,
    WeightLimitKg DECIMAL(18, 2),
    Description TEXT,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL,
    ModifiedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ModifiedBy UUID NOT NULL,
    
    -- Section 4.3 Unique Constraint
    CONSTRAINT UQ_WarehouseShelf_Area_Name_Code UNIQUE (AreaId, Name, Code)
);
COMMENT ON TABLE WarehouseShelf IS 'Specific shelving units within an area';

-- ----------------------------------------------------------------
-- Table: WarehouseLocation
-- ----------------------------------------------------------------
CREATE TABLE WarehouseLocation (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ShelfId UUID NOT NULL REFERENCES WarehouseShelf(Id) ON DELETE CASCADE,
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(50) NOT NULL,
    BinX INTEGER NOT NULL CHECK (BinX >= 0),
    BinY INTEGER NOT NULL CHECK (BinY >= 0),
    Quantity INT NOT NULL DEFAULT 0 CHECK (Quantity >= 0), -- Section 5.5 Negative Quantities not allowed
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL,
    ModifiedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ModifiedBy UUID NOT NULL
);
COMMENT ON TABLE WarehouseLocation IS 'Physical bin slot';

-- Add Indexes for Section 4.2 & Location queries
CREATE INDEX IX_WarehouseLocation_ShelfId ON WarehouseLocation(ShelfId);

-- ----------------------------------------------------------------
-- Table: ComponentType
-- ----------------------------------------------------------------
CREATE TABLE ComponentType (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Kind VARCHAR(100) NOT NULL,
    Value VARCHAR(100) NOT NULL,
    Footprint VARCHAR(100),
    Type component_type_enum NOT NULL,
    Description TEXT,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL,
    ModifiedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ModifiedBy UUID NOT NULL
);
COMMENT ON TABLE ComponentType IS 'Master definition of a part type split into kind/value/footprint.';

-- ----------------------------------------------------------------
-- Table: ComponentCategory
-- ----------------------------------------------------------------
CREATE TABLE ComponentCategory (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    ParentId UUID REFERENCES ComponentCategory(Id) ON DELETE CASCADE, -- Self-referencing for hierarchy
    Description TEXT,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL,
    ModifiedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ModifiedBy UUID NOT NULL
);
COMMENT ON TABLE ComponentCategory IS 'Hierarchical categorization (e.g., Resistors > SMD > 0603)';

-- ----------------------------------------------------------------
-- Table: Component
-- ----------------------------------------------------------------
CREATE TABLE Component (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ComponentTypeId UUID NOT NULL REFERENCES ComponentType(Id) ON DELETE CASCADE,
    ComponentTypeName VARCHAR(200), -- Cached field per spec
    PartNumber VARCHAR(100) NOT NULL,
    BatchCode VARCHAR(100), -- Optional per spec
    QuantityOnHand INT NOT NULL DEFAULT 0 CHECK (QuantityOnHand >= 0),
    QuantityReserved INT NOT NULL DEFAULT 0 CHECK (QuantityReserved >= 0),
    QuantityCommitted INT NOT NULL DEFAULT 0 CHECK (QuantityCommitted >= 0),
    MinimumStockLevel INT,
    MaximumStockLevel INT,
    ReorderPoint INT,
    UnitCost DECIMAL(19, 2),
    SupplierId UUID, -- FKF to Supplier (needs table or just ID? Assuming generic Supplier table or external. Creating reference logic below).
    SupplierCode VARCHAR(100),
    SupplierName VARCHAR(200),
    SupplierPartNumber VARCHAR(200),
    LastPriceChange TIMESTAMP WITH TIME ZONE,
    SupplierLeadTime INT CHECK (SupplierLeadTime >= 0),
    LastPurchaseDate TIMESTAMP WITH TIME ZONE,
    LastReceivedDate TIMESTAMP WITH TIME ZONE,
    LastSoldDate TIMESTAMP WITH TIME ZONE,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL,
    ModifiedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ModifiedBy UUID NOT NULL
);
COMMENT ON TABLE Component IS 'Inventory instance record';

-- Add Indexes for Section 4.2
CREATE INDEX IX_Component_ComponentTypeId ON Component(ComponentTypeId);
CREATE INDEX IX_Component_BatchCode ON Component(BatchCode);
CREATE INDEX IX_Component_SupplierId ON Component(SupplierId);

-- ----------------------------------------------------------------
-- Table: StockLocation
-- ----------------------------------------------------------------
CREATE TABLE StockLocation (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ComponentId UUID NOT NULL REFERENCES Component(Id) ON DELETE CASCADE,
    LocationId UUID NOT NULL REFERENCES WarehouseLocation(Id) ON DELETE CASCADE,
    BinX INTEGER NOT NULL CHECK (BinX >= 0),
    BinY INTEGER NOT NULL CHECK (BinY >= 0),
    Quantity INT NOT NULL DEFAULT 0 CHECK (Quantity >= 0),
    BatchCode VARCHAR(100),
    ExpiryDate TIMESTAMP WITH TIME ZONE,
    LastUpdated TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL,
    ModifiedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ModifiedBy UUID NOT NULL
);
COMMENT ON TABLE StockLocation IS 'Physical stock record linking Component to Location';

-- Add Indexes for Section 4.2
CREATE INDEX IX_StockLocation_ComponentId ON StockLocation(ComponentId);
CREATE INDEX IX_StockLocation_LocationId ON StockLocation(LocationId);

-- =============================================
-- 3. Additional Tables (If Supplier is local)
-- Note: The spec implies SupplierId FK in Component but doesn't define the SupplierAggregate table.
-- I will create a generic Supplier table to satisfy the FK constraint defined in Section 4.2.
-- =============================================

CREATE TABLE Supplier (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Code VARCHAR(100) NOT NULL,
    Name VARCHAR(200) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL,
    ModifiedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ModifiedBy UUID NOT NULL
);

CREATE UNIQUE INDEX IX_Supplier_Code ON Supplier(Code);

-- =============================================
-- 4. Indexes & Comments (Refinement)
-- =============================================

-- Explicit indexes mentioned in Section 4.2
-- (Already created above, but adding comments for clarity)
COMMENT ON INDEX IX_Component_ComponentTypeId IS 'Foreign Key to ComponentType';
COMMENT ON INDEX IX_Component_BatchCode IS 'Tracking index';
COMMENT ON INDEX IX_Component_SupplierId IS 'Foreign Key to Supplier';

COMMENT ON INDEX IX_StockLocation_ComponentId IS 'Foreign Key to Component';
COMMENT ON INDEX IX_StockLocation_LocationId IS 'Foreign Key to WarehouseLocation';

-- =============================================
-- 5. Helper Function for Audit Logs (Optional)
-- In a real app, you might replace GUIDs here with application user IDs
-- =============================================

COMMENT ON COLUMN Component.CreatedBy IS 'User who created the record';
COMMENT ON COLUMN Component.ModifiedBy IS 'User who last modified the record';
COMMENT ON COLUMN StockLocation.LastUpdated IS 'Timestamp of last quantity change';
