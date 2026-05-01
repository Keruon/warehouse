#!/usr/bin/env bash

set -euo pipefail

DB_CONTAINER="${DB_CONTAINER:-storage-postgres}"
DB_NAME="${POSTGRES_DB:-storage}"
DB_USER="${POSTGRES_USER:-storage}"
LEGACY_CATEGORY_ID="${LEGACY_CATEGORY_ID:-00000000-0000-0000-0000-000000000001}"

docker exec -i "$DB_CONTAINER" psql -v ON_ERROR_STOP=1 -v legacy_category_id="$LEGACY_CATEGORY_ID" -U "$DB_USER" -d "$DB_NAME" <<'SQL'
BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'zone_type_enum') THEN
        CREATE TYPE zone_type_enum AS ENUM ('Storage', 'Production', 'Shipping', 'Returns', 'Maintenance');
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'component_type_enum') THEN
        CREATE TYPE component_type_enum AS ENUM ('SMD', 'ThroughHole', 'QFP', 'SOIC', 'DIP', 'Other');
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS "ComponentCategories" (
    "Id" uuid PRIMARY KEY,
    "Name" text NOT NULL,
    "ParentId" uuid NULL,
    "Description" text NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "ModifiedAt" timestamp with time zone NOT NULL,
    "ModifiedBy" uuid NOT NULL,
    CONSTRAINT "FK_ComponentCategories_ComponentCategories_ParentId"
        FOREIGN KEY ("ParentId") REFERENCES "ComponentCategories" ("Id")
);

CREATE TABLE IF NOT EXISTS "ComponentTypes" (
    "Id" uuid PRIMARY KEY,
    "CategoryId" uuid NOT NULL,
    "Kind" text NOT NULL,
    "Value" text NOT NULL,
    "Footprint" text NULL,
    "Type" component_type_enum NOT NULL,
    "Description" text NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "ModifiedAt" timestamp with time zone NOT NULL,
    "ModifiedBy" uuid NOT NULL,
    CONSTRAINT "FK_ComponentTypes_ComponentCategories_CategoryId"
        FOREIGN KEY ("CategoryId") REFERENCES "ComponentCategories" ("Id")
);

INSERT INTO "ComponentCategories" ("Id", "Name", "ParentId", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
VALUES (
    '00000000-0000-0000-0000-000000000001'::uuid,
    'Legacy Imported',
    NULL,
    'Fallback category for repaired legacy component types.',
    TRUE,
    NOW(),
    '00000000-0000-0000-0000-000000000001'::uuid,
    NOW(),
    '00000000-0000-0000-0000-000000000001'::uuid
)
ON CONFLICT ("Id") DO NOTHING;

ALTER TABLE "ComponentTypes"
    ADD COLUMN IF NOT EXISTS "CategoryId" uuid NULL;

ALTER TABLE "ComponentTypes"
    ADD COLUMN IF NOT EXISTS "Kind" text;

ALTER TABLE "ComponentTypes"
    ADD COLUMN IF NOT EXISTS "Value" text;

ALTER TABLE "ComponentTypes"
    ADD COLUMN IF NOT EXISTS "Footprint" text;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'ComponentTypes' AND column_name = 'Name'
    ) THEN
        EXECUTE '
            UPDATE "ComponentTypes"
            SET "Kind" = COALESCE(NULLIF(TRIM("Kind"), ''''), NULLIF(TRIM("Name"), ''''), ''Legacy''),
                "Value" = COALESCE(NULLIF(TRIM("Value"), ''''), NULLIF(TRIM("Name"), ''''), ''Unknown''),
                "CategoryId" = COALESCE("CategoryId", ''00000000-0000-0000-0000-000000000001''::uuid)
            WHERE COALESCE(TRIM("Kind"), '''') = ''''
               OR COALESCE(TRIM("Value"), '''') = ''''
               OR "CategoryId" IS NULL';
    ELSE
        UPDATE "ComponentTypes"
        SET "Kind" = COALESCE(NULLIF(TRIM("Kind"), ''), 'Legacy'),
            "Value" = COALESCE(NULLIF(TRIM("Value"), ''), 'Unknown'),
            "CategoryId" = COALESCE("CategoryId", '00000000-0000-0000-0000-000000000001'::uuid)
        WHERE COALESCE(TRIM("Kind"), '') = ''
           OR COALESCE(TRIM("Value"), '') = ''
           OR "CategoryId" IS NULL;
    END IF;
END $$;

ALTER TABLE "ComponentTypes"
    ALTER COLUMN "CategoryId" SET NOT NULL;

ALTER TABLE "ComponentTypes"
    ALTER COLUMN "Kind" SET NOT NULL;

ALTER TABLE "ComponentTypes"
    ALTER COLUMN "Value" SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_ComponentTypes_ComponentCategories_CategoryId'
    ) THEN
        ALTER TABLE "ComponentTypes"
            ADD CONSTRAINT "FK_ComponentTypes_ComponentCategories_CategoryId"
            FOREIGN KEY ("CategoryId") REFERENCES "ComponentCategories" ("Id");
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS "Suppliers" (
    "Id" uuid PRIMARY KEY,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "ModifiedAt" timestamp with time zone NOT NULL,
    "ModifiedBy" uuid NOT NULL
);

CREATE TABLE IF NOT EXISTS "WarehouseAreas" (
    "Id" uuid PRIMARY KEY,
    "Name" text NOT NULL,
    "Code" text NOT NULL,
    "ZoneType" zone_type_enum NOT NULL,
    "FloorLevel" integer NOT NULL,
    "Description" text NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "ModifiedAt" timestamp with time zone NOT NULL,
    "ModifiedBy" uuid NOT NULL
);

CREATE TABLE IF NOT EXISTS "WarehouseShelves" (
    "Id" uuid PRIMARY KEY,
    "AreaId" uuid NOT NULL,
    "Name" text NOT NULL,
    "Code" text NOT NULL,
    "WeightLimitKg" numeric NULL,
    "Description" text NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "ModifiedAt" timestamp with time zone NOT NULL,
    "ModifiedBy" uuid NOT NULL,
    CONSTRAINT "FK_WarehouseShelves_WarehouseAreas_AreaId"
        FOREIGN KEY ("AreaId") REFERENCES "WarehouseAreas" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "WarehouseLocations" (
    "Id" uuid PRIMARY KEY,
    "ShelfId" uuid NOT NULL,
    "Name" text NOT NULL,
    "Code" text NOT NULL,
    "Description" text NULL,
    "BinX" integer NOT NULL,
    "BinY" integer NOT NULL,
    "Depth" numeric NULL,
    "Width" numeric NULL,
    "Height" numeric NULL,
    "Volume" numeric NULL,
    "IsReserved" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "ModifiedAt" timestamp with time zone NOT NULL,
    "ModifiedBy" uuid NOT NULL,
    CONSTRAINT "FK_WarehouseLocations_WarehouseShelves_ShelfId"
        FOREIGN KEY ("ShelfId") REFERENCES "WarehouseShelves" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "Components" (
    "Id" uuid PRIMARY KEY,
    "ComponentTypeId" uuid NOT NULL,
    "ComponentTypeName" text NULL,
    "PartNumber" text NOT NULL,
    "BatchCode" text NULL,
    "QuantityOnHand" integer NOT NULL,
    "QuantityReserved" integer NOT NULL,
    "QuantityCommitted" integer NOT NULL,
    "MinimumStockLevel" integer NULL,
    "MaximumStockLevel" integer NULL,
    "ReorderPoint" integer NULL,
    "UnitCost" numeric NOT NULL,
    "SupplierId" uuid NULL,
    "SupplierCode" text NULL,
    "SupplierName" text NULL,
    "SupplierPartNumber" text NULL,
    "LastPriceChange" timestamp with time zone NULL,
    "SupplierLeadTime" integer NOT NULL,
    "LastPurchaseDate" timestamp with time zone NULL,
    "LastReceivedDate" timestamp with time zone NULL,
    "LastSoldDate" timestamp with time zone NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "ModifiedAt" timestamp with time zone NOT NULL,
    "ModifiedBy" uuid NOT NULL,
    CONSTRAINT "FK_Components_ComponentTypes_ComponentTypeId"
        FOREIGN KEY ("ComponentTypeId") REFERENCES "ComponentTypes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Components_Suppliers_SupplierId"
        FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS "StockLocations" (
    "Id" uuid PRIMARY KEY,
    "ComponentId" uuid NOT NULL,
    "LocationId" uuid NOT NULL,
    "BinX" integer NOT NULL,
    "BinY" integer NOT NULL,
    "Quantity" integer NOT NULL,
    "BatchCode" text NULL,
    "ExpiryDate" timestamp with time zone NULL,
    "LastUpdated" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "ModifiedAt" timestamp with time zone NOT NULL,
    "ModifiedBy" uuid NOT NULL,
    CONSTRAINT "FK_StockLocations_Components_ComponentId"
        FOREIGN KEY ("ComponentId") REFERENCES "Components" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_StockLocations_WarehouseLocations_LocationId"
        FOREIGN KEY ("LocationId") REFERENCES "WarehouseLocations" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "UserId" uuid NOT NULL,
    "Action" character varying(100) NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid NOT NULL,
    "OldValues" text NULL,
    "NewValues" text NULL,
    "IpAddress" character varying(64) NULL,
    "UserAgent" character varying(512) NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    CONSTRAINT "FK_AuditLogs_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_ComponentCategories_ParentId" ON "ComponentCategories" ("ParentId");
CREATE INDEX IF NOT EXISTS "IX_ComponentTypes_CategoryId" ON "ComponentTypes" ("CategoryId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Suppliers_Code" ON "Suppliers" ("Code");
CREATE INDEX IF NOT EXISTS "IX_Components_ComponentTypeId" ON "Components" ("ComponentTypeId");
CREATE INDEX IF NOT EXISTS "IX_Components_SupplierId" ON "Components" ("SupplierId");
CREATE INDEX IF NOT EXISTS "IX_Components_BatchCode" ON "Components" ("BatchCode");
CREATE INDEX IF NOT EXISTS "IX_StockLocations_ComponentId" ON "StockLocations" ("ComponentId");
CREATE INDEX IF NOT EXISTS "IX_StockLocations_LocationId" ON "StockLocations" ("LocationId");
CREATE INDEX IF NOT EXISTS "IX_WarehouseShelves_AreaId" ON "WarehouseShelves" ("AreaId");
CREATE INDEX IF NOT EXISTS "IX_WarehouseLocations_ShelfId" ON "WarehouseLocations" ("ShelfId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");

INSERT INTO "ComponentCategories" ("Id", "Name", "ParentId", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT :'legacy_category_id'::uuid, 'Legacy Imported', NULL, 'Fallback category for repaired legacy component types.', TRUE, NOW(), :'legacy_category_id'::uuid, NOW(), :'legacy_category_id'::uuid
WHERE EXISTS (SELECT 1 FROM componenttype)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "ComponentCategories" ("Id", "Name", "ParentId", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT id, name, parentid, description, isactive, createdat, createdby, modifiedat, modifiedby
FROM componentcategory
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "ComponentTypes" ("Id", "CategoryId", "Kind", "Value", "Footprint", "Type", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT id, :'legacy_category_id'::uuid, name, name, NULL, type, description, isactive, createdat, createdby, modifiedat, modifiedby
FROM componenttype
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Suppliers" ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT id, code, name, isactive, createdat, createdby, modifiedat, modifiedby
FROM supplier
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "WarehouseAreas" ("Id", "Name", "Code", "ZoneType", "FloorLevel", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT id, name, code, zonetype, floorlevel, description, isactive, createdat, createdby, modifiedat, modifiedby
FROM warehousearea
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "WarehouseShelves" ("Id", "AreaId", "Name", "Code", "WeightLimitKg", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT id, areaid, name, code, weightlimitkg, description, isactive, createdat, createdby, modifiedat, modifiedby
FROM warehouseshelf
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "WarehouseLocations" ("Id", "ShelfId", "Name", "Code", "Description", "BinX", "BinY", "Depth", "Width", "Height", "Volume", "IsReserved", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT id, shelfid, name, code, NULL, binx, biny, NULL, NULL, NULL, NULL, FALSE, isactive, createdat, createdby, modifiedat, modifiedby
FROM warehouselocation
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Components" ("Id", "ComponentTypeId", "ComponentTypeName", "PartNumber", "BatchCode", "QuantityOnHand", "QuantityReserved", "QuantityCommitted", "MinimumStockLevel", "MaximumStockLevel", "ReorderPoint", "UnitCost", "SupplierId", "SupplierCode", "SupplierName", "SupplierPartNumber", "LastPriceChange", "SupplierLeadTime", "LastPurchaseDate", "LastReceivedDate", "LastSoldDate", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT id, componenttypeid, componenttypename, partnumber, batchcode, quantityonhand, quantityreserved, quantitycommitted, minimumstocklevel, maximumstocklevel, reorderpoint, COALESCE(unitcost, 0), supplierid, suppliercode, suppliername, supplierpartnumber, lastpricechange, COALESCE(supplierleadtime, 0), lastpurchasedate, lastreceiveddate, lastsolddate, isactive, createdat, createdby, modifiedat, modifiedby
FROM component
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "StockLocations" ("Id", "ComponentId", "LocationId", "BinX", "BinY", "Quantity", "BatchCode", "ExpiryDate", "LastUpdated", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT id, componentid, locationid, binx, biny, quantity, batchcode, expirydate, lastupdated, isactive, createdat, createdby, modifiedat, modifiedby
FROM stocklocation
ON CONFLICT ("Id") DO NOTHING;

COMMIT;
SQL

echo "Legacy schema repair completed."