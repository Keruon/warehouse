#!/usr/bin/env bash

set -euo pipefail

DB_CONTAINER="${DB_CONTAINER:-storage-postgres}"
DB_NAME="${POSTGRES_DB:-storage}"
DB_USER="${POSTGRES_USER:-storage}"

# Set RESET=true to wipe current app data before inserting sample rows.
RESET="${RESET:-false}"

docker exec -i "$DB_CONTAINER" psql -v ON_ERROR_STOP=1 -U "$DB_USER" -d "$DB_NAME" <<SQL
BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO \
\$\$
BEGIN
    IF lower('${RESET}') = 'true' THEN
        TRUNCATE TABLE
            "AuditLogs",
            "RefreshTokens",
            "StockLocations",
            "Components",
            "WarehouseLocations",
            "WarehouseShelves",
            "WarehouseAreas",
            "ComponentTypes",
            "ComponentCategories",
            "Suppliers",
            "Users"
        RESTART IDENTITY CASCADE;
    END IF;
END
\$\$;

-- Users (3)
INSERT INTO "Users" (
    "Id", "Username", "Email", "PasswordHash", "Role", "FirstName", "LastName",
    "LastLoginAt", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy"
)
SELECT gen_random_uuid(), 'admin', 'admin@example.local', crypt('admin', gen_salt('bf', 12)),
       'Admin', 'System', 'Admin', NOW(), true, NOW(), gen_random_uuid(), NOW(), gen_random_uuid()
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'admin');

INSERT INTO "Users" (
    "Id", "Username", "Email", "PasswordHash", "Role", "FirstName", "LastName",
    "LastLoginAt", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy"
)
SELECT gen_random_uuid(), 'warehouse', 'warehouse@example.local', crypt('warehouse', gen_salt('bf', 12)),
       'User', 'Wally', 'Worker', NOW(), true, NOW(), gen_random_uuid(), NOW(), gen_random_uuid()
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'warehouse');

INSERT INTO "Users" (
    "Id", "Username", "Email", "PasswordHash", "Role", "FirstName", "LastName",
    "LastLoginAt", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy"
)
SELECT gen_random_uuid(), 'readonly', 'readonly@example.local', crypt('readonly', gen_salt('bf', 12)),
       'ReadOnly', 'Rita', 'Reader', NOW(), true, NOW(), gen_random_uuid(), NOW(), gen_random_uuid()
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'readonly');

UPDATE "Users"
SET "Email" = 'admin@example.local',
    "PasswordHash" = crypt('admin', gen_salt('bf', 12)),
    "Role" = 'Admin',
    "FirstName" = 'System',
    "LastName" = 'Admin',
    "LastLoginAt" = NOW(),
    "IsActive" = true,
    "ModifiedAt" = NOW()
WHERE "Username" = 'admin';

UPDATE "Users"
SET "Email" = 'warehouse@example.local',
    "PasswordHash" = crypt('warehouse', gen_salt('bf', 12)),
    "Role" = 'User',
    "FirstName" = 'Wally',
    "LastName" = 'Worker',
    "LastLoginAt" = NOW(),
    "IsActive" = true,
    "ModifiedAt" = NOW()
WHERE "Username" = 'warehouse';

UPDATE "Users"
SET "Email" = 'readonly@example.local',
    "PasswordHash" = crypt('readonly', gen_salt('bf', 12)),
    "Role" = 'ReadOnly',
    "FirstName" = 'Rita',
    "LastName" = 'Reader',
    "LastLoginAt" = NOW(),
    "IsActive" = true,
    "ModifiedAt" = NOW()
WHERE "Username" = 'readonly';

UPDATE "Users" u
SET "CreatedBy" = admin."Id",
    "ModifiedBy" = admin."Id"
FROM "Users" admin
WHERE admin."Username" = 'admin'
  AND u."Username" IN ('admin', 'warehouse', 'readonly');

-- Component categories (3)
WITH admin AS (
    SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1
)
INSERT INTO "ComponentCategories" ("Id", "Name", "ParentId", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), 'Resistors', NULL, 'Resistor families', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin
WHERE NOT EXISTS (SELECT 1 FROM "ComponentCategories" WHERE "Name" = 'Resistors');

WITH admin AS (
    SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1
)
INSERT INTO "ComponentCategories" ("Id", "Name", "ParentId", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), 'Capacitors', NULL, 'Capacitor families', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin
WHERE NOT EXISTS (SELECT 1 FROM "ComponentCategories" WHERE "Name" = 'Capacitors');

WITH admin AS (
    SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1
), parent AS (
    SELECT "Id" AS parent_id FROM "ComponentCategories" WHERE "Name" = 'Resistors' LIMIT 1
)
INSERT INTO "ComponentCategories" ("Id", "Name", "ParentId", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), 'SMD Resistors', parent.parent_id, '0603/0805 etc.', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, parent
WHERE NOT EXISTS (SELECT 1 FROM "ComponentCategories" WHERE "Name" = 'SMD Resistors');

-- Component types (3)
WITH admin AS (
    SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1
), cat AS (
    SELECT "Id" AS category_id FROM "ComponentCategories" WHERE "Name" = 'SMD Resistors' LIMIT 1
)
INSERT INTO "ComponentTypes" ("Id", "CategoryId", "Kind", "Value", "Footprint", "Type", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), cat.category_id, 'Resistor', '10k', '0603', 'SMD', 'General purpose resistor', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, cat
WHERE NOT EXISTS (SELECT 1 FROM "ComponentTypes" WHERE "Kind" = 'Resistor' AND "Value" = '10k' AND "Footprint" = '0603');

WITH cat AS (
    SELECT "Id" AS category_id FROM "ComponentCategories" WHERE "Name" = 'SMD Resistors' LIMIT 1
)
UPDATE "ComponentTypes" ct
SET "CategoryId" = cat.category_id,
    "Kind" = 'Resistor',
    "Value" = '10k',
    "Footprint" = '0603',
    "Type" = 'SMD',
    "Description" = 'General purpose resistor',
    "IsActive" = true,
    "ModifiedAt" = NOW()
FROM cat
WHERE ct."Kind" = 'Resistor' AND ct."Value" = '10k' AND ct."Footprint" = '0603';

WITH admin AS (
    SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1
), cat AS (
    SELECT "Id" AS category_id FROM "ComponentCategories" WHERE "Name" = 'Capacitors' LIMIT 1
)
INSERT INTO "ComponentTypes" ("Id", "CategoryId", "Kind", "Value", "Footprint", "Type", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), cat.category_id, 'Capacitor', '100nF', 'Radial', 'ThroughHole', 'Ceramic capacitor', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, cat
WHERE NOT EXISTS (SELECT 1 FROM "ComponentTypes" WHERE "Kind" = 'Capacitor' AND "Value" = '100nF' AND "Footprint" = 'Radial');

WITH cat AS (
    SELECT "Id" AS category_id FROM "ComponentCategories" WHERE "Name" = 'Capacitors' LIMIT 1
)
UPDATE "ComponentTypes" ct
SET "CategoryId" = cat.category_id,
    "Kind" = 'Capacitor',
    "Value" = '100nF',
    "Footprint" = 'Radial',
    "Type" = 'ThroughHole',
    "Description" = 'Ceramic capacitor',
    "IsActive" = true,
    "ModifiedAt" = NOW()
FROM cat
WHERE ct."Kind" = 'Capacitor' AND ct."Value" = '100nF' AND ct."Footprint" = 'Radial';

WITH admin AS (
    SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1
), cat AS (
    SELECT "Id" AS category_id FROM "ComponentCategories" WHERE "Name" = 'Resistors' LIMIT 1
)
INSERT INTO "ComponentTypes" ("Id", "CategoryId", "Kind", "Value", "Footprint", "Type", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), cat.category_id, 'Resistor', '1k', 'Axial', 'DIP', 'Lead resistor', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, cat
WHERE NOT EXISTS (SELECT 1 FROM "ComponentTypes" WHERE "Kind" = 'Resistor' AND "Value" = '1k' AND "Footprint" = 'Axial');

WITH cat AS (
    SELECT "Id" AS category_id FROM "ComponentCategories" WHERE "Name" = 'Resistors' LIMIT 1
)
UPDATE "ComponentTypes" ct
SET "CategoryId" = cat.category_id,
    "Kind" = 'Resistor',
    "Value" = '1k',
    "Footprint" = 'Axial',
    "Type" = 'DIP',
    "Description" = 'Lead resistor',
    "IsActive" = true,
    "ModifiedAt" = NOW()
FROM cat
WHERE ct."Kind" = 'Resistor' AND ct."Value" = '1k' AND ct."Footprint" = 'Axial';

-- Suppliers (3)
WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1)
INSERT INTO "Suppliers" ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), 'SUP-ALPHA', 'Alpha Components', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin
WHERE NOT EXISTS (SELECT 1 FROM "Suppliers" WHERE "Code" = 'SUP-ALPHA');

UPDATE "Suppliers"
SET "Name" = 'Alpha Components',
    "IsActive" = true,
    "ModifiedAt" = NOW()
WHERE "Code" = 'SUP-ALPHA';

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1)
INSERT INTO "Suppliers" ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), 'SUP-BETA', 'Beta Electronics', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin
WHERE NOT EXISTS (SELECT 1 FROM "Suppliers" WHERE "Code" = 'SUP-BETA');

UPDATE "Suppliers"
SET "Name" = 'Beta Electronics',
    "IsActive" = true,
    "ModifiedAt" = NOW()
WHERE "Code" = 'SUP-BETA';

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1)
INSERT INTO "Suppliers" ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), 'SUP-GAMMA', 'Gamma Supplies', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin
WHERE NOT EXISTS (SELECT 1 FROM "Suppliers" WHERE "Code" = 'SUP-GAMMA');

UPDATE "Suppliers"
SET "Name" = 'Gamma Supplies',
    "IsActive" = true,
    "ModifiedAt" = NOW()
WHERE "Code" = 'SUP-GAMMA';

-- Warehouse areas (2)
WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1)
INSERT INTO "WarehouseAreas" ("Id", "Name", "Code", "ZoneType", "FloorLevel", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), 'Main Storage', 'A1', 'Storage', 1, 'Primary storage area', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin
WHERE NOT EXISTS (
    SELECT 1 FROM "WarehouseAreas"
    WHERE "Name" = 'Main Storage' AND "Code" = 'A1' AND "ZoneType" = 'Storage' AND "FloorLevel" = 1
);

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1)
INSERT INTO "WarehouseAreas" ("Id", "Name", "Code", "ZoneType", "FloorLevel", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), 'Production', 'P1', 'Production', 1, 'Production staging', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin
WHERE NOT EXISTS (
    SELECT 1 FROM "WarehouseAreas"
    WHERE "Name" = 'Production' AND "Code" = 'P1' AND "ZoneType" = 'Production' AND "FloorLevel" = 1
);

-- Warehouse shelves (3)
WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), area AS (
    SELECT "Id" AS area_id FROM "WarehouseAreas" WHERE "Code" = 'A1' LIMIT 1
)
INSERT INTO "WarehouseShelves" ("Id", "AreaId", "Name", "Code", "WeightLimitKg", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), area.area_id, 'Shelf A', 'A-01', 100.0, 'Main shelf A', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, area
WHERE NOT EXISTS (
    SELECT 1 FROM "WarehouseShelves" s
    WHERE s."AreaId" = area.area_id AND s."Name" = 'Shelf A' AND s."Code" = 'A-01'
);

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), area AS (
    SELECT "Id" AS area_id FROM "WarehouseAreas" WHERE "Code" = 'A1' LIMIT 1
)
INSERT INTO "WarehouseShelves" ("Id", "AreaId", "Name", "Code", "WeightLimitKg", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), area.area_id, 'Shelf B', 'A-02', 100.0, 'Main shelf B', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, area
WHERE NOT EXISTS (
    SELECT 1 FROM "WarehouseShelves" s
    WHERE s."AreaId" = area.area_id AND s."Name" = 'Shelf B' AND s."Code" = 'A-02'
);

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), area AS (
    SELECT "Id" AS area_id FROM "WarehouseAreas" WHERE "Code" = 'P1' LIMIT 1
)
INSERT INTO "WarehouseShelves" ("Id", "AreaId", "Name", "Code", "WeightLimitKg", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), area.area_id, 'Shelf P', 'P-01', 120.0, 'Production shelf', true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, area
WHERE NOT EXISTS (
    SELECT 1 FROM "WarehouseShelves" s
    WHERE s."AreaId" = area.area_id AND s."Name" = 'Shelf P' AND s."Code" = 'P-01'
);

-- Warehouse locations (4)
WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), shelf AS (
    SELECT "Id" AS shelf_id FROM "WarehouseShelves" WHERE "Code" = 'A-01' LIMIT 1
)
INSERT INTO "WarehouseLocations" ("Id", "ShelfId", "Name", "Code", "Description", "BinX", "BinY", "Depth", "Width", "Height", "Volume", "IsReserved", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), shelf.shelf_id, 'A-01-01', 'BIN-A-01-01', 'Top-left bin', 1, 1, 30, 20, 15, 9000, false, true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, shelf
WHERE NOT EXISTS (
    SELECT 1 FROM "WarehouseLocations" l
    WHERE l."ShelfId" = shelf.shelf_id AND l."Name" = 'A-01-01' AND l."Code" = 'BIN-A-01-01'
);

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), shelf AS (
    SELECT "Id" AS shelf_id FROM "WarehouseShelves" WHERE "Code" = 'A-01' LIMIT 1
)
INSERT INTO "WarehouseLocations" ("Id", "ShelfId", "Name", "Code", "Description", "BinX", "BinY", "Depth", "Width", "Height", "Volume", "IsReserved", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), shelf.shelf_id, 'A-01-02', 'BIN-A-01-02', 'Top-right bin', 2, 1, 30, 20, 15, 9000, false, true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, shelf
WHERE NOT EXISTS (
    SELECT 1 FROM "WarehouseLocations" l
    WHERE l."ShelfId" = shelf.shelf_id AND l."Name" = 'A-01-02' AND l."Code" = 'BIN-A-01-02'
);

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), shelf AS (
    SELECT "Id" AS shelf_id FROM "WarehouseShelves" WHERE "Code" = 'A-02' LIMIT 1
)
INSERT INTO "WarehouseLocations" ("Id", "ShelfId", "Name", "Code", "Description", "BinX", "BinY", "Depth", "Width", "Height", "Volume", "IsReserved", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), shelf.shelf_id, 'A-02-01', 'BIN-A-02-01', 'Secondary shelf', 1, 1, 25, 20, 15, 7500, false, true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, shelf
WHERE NOT EXISTS (
    SELECT 1 FROM "WarehouseLocations" l
    WHERE l."ShelfId" = shelf.shelf_id AND l."Name" = 'A-02-01' AND l."Code" = 'BIN-A-02-01'
);

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), shelf AS (
    SELECT "Id" AS shelf_id FROM "WarehouseShelves" WHERE "Code" = 'P-01' LIMIT 1
)
INSERT INTO "WarehouseLocations" ("Id", "ShelfId", "Name", "Code", "Description", "BinX", "BinY", "Depth", "Width", "Height", "Volume", "IsReserved", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), shelf.shelf_id, 'P-01-01', 'BIN-P-01-01', 'Production bin', 1, 1, 20, 20, 10, 4000, true, true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, shelf
WHERE NOT EXISTS (
    SELECT 1 FROM "WarehouseLocations" l
    WHERE l."ShelfId" = shelf.shelf_id AND l."Name" = 'P-01-01' AND l."Code" = 'BIN-P-01-01'
);

-- Components (4)
WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), ct AS (
    SELECT "Id" AS ct_id FROM "ComponentTypes" WHERE "Kind" = 'Resistor' AND "Value" = '10k' AND "Footprint" = '0603' LIMIT 1
), sup AS (
    SELECT "Id" AS sup_id, "Code" AS sup_code, "Name" AS sup_name FROM "Suppliers" WHERE "Code" = 'SUP-ALPHA' LIMIT 1
)
INSERT INTO "Components" ("Id", "ComponentTypeId", "ComponentTypeName", "PartNumber", "BatchCode", "QuantityOnHand", "QuantityReserved", "QuantityCommitted", "MinimumStockLevel", "MaximumStockLevel", "ReorderPoint", "UnitCost", "SupplierId", "SupplierCode", "SupplierName", "SupplierPartNumber", "LastPriceChange", "SupplierLeadTime", "LastPurchaseDate", "LastReceivedDate", "LastSoldDate", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), ct.ct_id, 'Resistor 10k 0603', 'R-10K-0603', 'BATCH-R1', 1500, 100, 25, 300, 5000, 600, 0.0100,
       sup.sup_id, sup.sup_code, sup.sup_name, 'A-R10K-0603', NOW(), 14, NOW() - INTERVAL '30 days', NOW() - INTERVAL '7 days', NOW() - INTERVAL '2 days',
       true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, ct, sup
WHERE NOT EXISTS (SELECT 1 FROM "Components" WHERE "PartNumber" = 'R-10K-0603');

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), ct AS (
    SELECT "Id" AS ct_id FROM "ComponentTypes" WHERE "Kind" = 'Capacitor' AND "Value" = '100nF' AND "Footprint" = 'Radial' LIMIT 1
), sup AS (
    SELECT "Id" AS sup_id, "Code" AS sup_code, "Name" AS sup_name FROM "Suppliers" WHERE "Code" = 'SUP-BETA' LIMIT 1
)
INSERT INTO "Components" ("Id", "ComponentTypeId", "ComponentTypeName", "PartNumber", "BatchCode", "QuantityOnHand", "QuantityReserved", "QuantityCommitted", "MinimumStockLevel", "MaximumStockLevel", "ReorderPoint", "UnitCost", "SupplierId", "SupplierCode", "SupplierName", "SupplierPartNumber", "LastPriceChange", "SupplierLeadTime", "LastPurchaseDate", "LastReceivedDate", "LastSoldDate", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), ct.ct_id, 'Capacitor 100nF Radial', 'C-100NF-THT', 'BATCH-C1', 800, 50, 10, 200, 3000, 400, 0.0200,
       sup.sup_id, sup.sup_code, sup.sup_name, 'B-C100NF', NOW(), 21, NOW() - INTERVAL '40 days', NOW() - INTERVAL '10 days', NOW() - INTERVAL '3 days',
       true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, ct, sup
WHERE NOT EXISTS (SELECT 1 FROM "Components" WHERE "PartNumber" = 'C-100NF-THT');

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), ct AS (
    SELECT "Id" AS ct_id FROM "ComponentTypes" WHERE "Kind" = 'Resistor' AND "Value" = '1k' AND "Footprint" = 'Axial' LIMIT 1
), sup AS (
    SELECT "Id" AS sup_id, "Code" AS sup_code, "Name" AS sup_name FROM "Suppliers" WHERE "Code" = 'SUP-GAMMA' LIMIT 1
)
INSERT INTO "Components" ("Id", "ComponentTypeId", "ComponentTypeName", "PartNumber", "BatchCode", "QuantityOnHand", "QuantityReserved", "QuantityCommitted", "MinimumStockLevel", "MaximumStockLevel", "ReorderPoint", "UnitCost", "SupplierId", "SupplierCode", "SupplierName", "SupplierPartNumber", "LastPriceChange", "SupplierLeadTime", "LastPurchaseDate", "LastReceivedDate", "LastSoldDate", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), ct.ct_id, 'Resistor 1k Axial', 'R-1K-THT', 'BATCH-R2', 1200, 120, 30, 250, 4000, 500, 0.0150,
       sup.sup_id, sup.sup_code, sup.sup_name, 'G-R1K-THT', NOW(), 10, NOW() - INTERVAL '25 days', NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 day',
       true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, ct, sup
WHERE NOT EXISTS (SELECT 1 FROM "Components" WHERE "PartNumber" = 'R-1K-THT');

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), ct AS (
    SELECT "Id" AS ct_id FROM "ComponentTypes" WHERE "Kind" = 'Resistor' AND "Value" = '10k' AND "Footprint" = '0603' LIMIT 1
), sup AS (
    SELECT "Id" AS sup_id, "Code" AS sup_code, "Name" AS sup_name FROM "Suppliers" WHERE "Code" = 'SUP-ALPHA' LIMIT 1
)
INSERT INTO "Components" ("Id", "ComponentTypeId", "ComponentTypeName", "PartNumber", "BatchCode", "QuantityOnHand", "QuantityReserved", "QuantityCommitted", "MinimumStockLevel", "MaximumStockLevel", "ReorderPoint", "UnitCost", "SupplierId", "SupplierCode", "SupplierName", "SupplierPartNumber", "LastPriceChange", "SupplierLeadTime", "LastPurchaseDate", "LastReceivedDate", "LastSoldDate", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), ct.ct_id, 'Resistor 10k 0603', 'R-10K-ALT', 'BATCH-R3', 450, 20, 5, 100, 1500, 200, 0.0110,
       sup.sup_id, sup.sup_code, sup.sup_name, 'A-R10K-ALT', NOW(), 14, NOW() - INTERVAL '18 days', NOW() - INTERVAL '6 days', NOW() - INTERVAL '2 days',
       true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, ct, sup
WHERE NOT EXISTS (SELECT 1 FROM "Components" WHERE "PartNumber" = 'R-10K-ALT');

-- Stock locations (4)
WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), comp AS (
    SELECT "Id" AS component_id FROM "Components" WHERE "PartNumber" = 'R-10K-0603' LIMIT 1
), loc AS (
    SELECT "Id" AS location_id FROM "WarehouseLocations" WHERE "Code" = 'BIN-A-01-01' LIMIT 1
)
INSERT INTO "StockLocations" ("Id", "ComponentId", "LocationId", "BinX", "BinY", "Quantity", "BatchCode", "ExpiryDate", "LastUpdated", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), comp.component_id, loc.location_id, 1, 1, 900, 'BATCH-R1', NULL, NOW(), true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, comp, loc
WHERE NOT EXISTS (
    SELECT 1 FROM "StockLocations" s
    WHERE s."ComponentId" = comp.component_id AND s."LocationId" = loc.location_id AND s."BatchCode" = 'BATCH-R1'
);

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), comp AS (
    SELECT "Id" AS component_id FROM "Components" WHERE "PartNumber" = 'C-100NF-THT' LIMIT 1
), loc AS (
    SELECT "Id" AS location_id FROM "WarehouseLocations" WHERE "Code" = 'BIN-A-01-02' LIMIT 1
)
INSERT INTO "StockLocations" ("Id", "ComponentId", "LocationId", "BinX", "BinY", "Quantity", "BatchCode", "ExpiryDate", "LastUpdated", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), comp.component_id, loc.location_id, 2, 1, 600, 'BATCH-C1', NULL, NOW(), true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, comp, loc
WHERE NOT EXISTS (
    SELECT 1 FROM "StockLocations" s
    WHERE s."ComponentId" = comp.component_id AND s."LocationId" = loc.location_id AND s."BatchCode" = 'BATCH-C1'
);

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), comp AS (
    SELECT "Id" AS component_id FROM "Components" WHERE "PartNumber" = 'R-1K-THT' LIMIT 1
), loc AS (
    SELECT "Id" AS location_id FROM "WarehouseLocations" WHERE "Code" = 'BIN-A-02-01' LIMIT 1
)
INSERT INTO "StockLocations" ("Id", "ComponentId", "LocationId", "BinX", "BinY", "Quantity", "BatchCode", "ExpiryDate", "LastUpdated", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), comp.component_id, loc.location_id, 1, 1, 700, 'BATCH-R2', NULL, NOW(), true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, comp, loc
WHERE NOT EXISTS (
    SELECT 1 FROM "StockLocations" s
    WHERE s."ComponentId" = comp.component_id AND s."LocationId" = loc.location_id AND s."BatchCode" = 'BATCH-R2'
);

WITH admin AS (SELECT "Id" AS admin_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1), comp AS (
    SELECT "Id" AS component_id FROM "Components" WHERE "PartNumber" = 'R-10K-ALT' LIMIT 1
), loc AS (
    SELECT "Id" AS location_id FROM "WarehouseLocations" WHERE "Code" = 'BIN-P-01-01' LIMIT 1
)
INSERT INTO "StockLocations" ("Id", "ComponentId", "LocationId", "BinX", "BinY", "Quantity", "BatchCode", "ExpiryDate", "LastUpdated", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT gen_random_uuid(), comp.component_id, loc.location_id, 1, 1, 300, 'BATCH-R3', NULL, NOW(), true, NOW(), admin.admin_id, NOW(), admin.admin_id
FROM admin, comp, loc
WHERE NOT EXISTS (
    SELECT 1 FROM "StockLocations" s
    WHERE s."ComponentId" = comp.component_id AND s."LocationId" = loc.location_id AND s."BatchCode" = 'BATCH-R3'
);

-- Refresh tokens (2)
INSERT INTO "RefreshTokens" ("Id", "UserId", "Token", "DeviceFingerprint", "ExpiresAt", "CreatedAt", "RevokedAt", "IsRevoked")
SELECT gen_random_uuid(), u."Id", encode(gen_random_bytes(48), 'base64'), 'seed-script', NOW() + INTERVAL '30 days', NOW(), NULL, false
FROM "Users" u
WHERE u."Username" IN ('admin', 'warehouse')
  AND NOT EXISTS (
      SELECT 1 FROM "RefreshTokens" rt
      WHERE rt."UserId" = u."Id" AND rt."DeviceFingerprint" = 'seed-script' AND rt."IsRevoked" = false
  );

-- Audit logs (3)
INSERT INTO "AuditLogs" ("UserId", "Action", "EntityType", "EntityId", "OldValues", "NewValues", "IpAddress", "UserAgent", "Timestamp")
SELECT u."Id", 'Seed', 'Component', c."Id", NULL, '{"partNumber":"R-10K-0603"}', '127.0.0.1', 'seed-sample-data.sh', NOW()
FROM "Users" u
JOIN "Components" c ON c."PartNumber" = 'R-10K-0603'
WHERE u."Username" = 'admin'
  AND NOT EXISTS (
      SELECT 1 FROM "AuditLogs" a
      WHERE a."UserId" = u."Id" AND a."Action" = 'Seed' AND a."EntityType" = 'Component' AND a."EntityId" = c."Id"
  );

INSERT INTO "AuditLogs" ("UserId", "Action", "EntityType", "EntityId", "OldValues", "NewValues", "IpAddress", "UserAgent", "Timestamp")
SELECT u."Id", 'Receive', 'Stock', s."Id", NULL, '{"quantity":600}', '127.0.0.1', 'seed-sample-data.sh', NOW()
FROM "Users" u
JOIN "StockLocations" s ON s."BatchCode" = 'BATCH-C1'
WHERE u."Username" = 'warehouse'
  AND NOT EXISTS (
      SELECT 1 FROM "AuditLogs" a
      WHERE a."UserId" = u."Id" AND a."Action" = 'Receive' AND a."EntityType" = 'Stock' AND a."EntityId" = s."Id"
  );

INSERT INTO "AuditLogs" ("UserId", "Action", "EntityType", "EntityId", "OldValues", "NewValues", "IpAddress", "UserAgent", "Timestamp")
SELECT u."Id", 'View', 'Dashboard', wa."Id", NULL, '{"area":"Main Storage"}', '127.0.0.1', 'seed-sample-data.sh', NOW()
FROM "Users" u
JOIN "WarehouseAreas" wa ON wa."Code" = 'A1'
WHERE u."Username" = 'readonly'
  AND NOT EXISTS (
      SELECT 1 FROM "AuditLogs" a
      WHERE a."UserId" = u."Id" AND a."Action" = 'View' AND a."EntityType" = 'Dashboard' AND a."EntityId" = wa."Id"
  );

COMMIT;
SQL

echo "Sample data seeding complete."
