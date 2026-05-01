  name: Datamodel
  description: The complete data model for this project, including tables and their relationships
  ---

  ## Overview

  This document describes the complete data model for our PostgreSQL database. The database schema has been carefully designed to support a warehouse management system with multi-tenant
  architecture.

  ## Architecture

  - **Multi-tenant design**: All tenant-related data is isolated using the `tenant` column
  - **Tenant identifier**: Every table includes a `tenant` column (bigint, nullable) for tenant isolation
  - **Shared columns**: Common columns like `id`, `created_at`, `updated_at`, and `deleted_at` are consistently used across all tables
  - **Relationships**: Foreign key relationships are documented with cardinality and constraints

  ## Data Model

  ### Core Tables

  #### tenants
  ```sql
  CREATE TABLE tenants (
      tenant_id bigint NOT NULL,
      -- Additional tenant configuration fields
      PRIMARY KEY (tenant_id)
  );

  Description: Stores information about each tenant organization in the multi-tenant system. Contains tenant identification and basic configuration.

  tenants_configuration

  CREATE TABLE tenants_configuration (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Additional configuration fields
      PRIMARY KEY (id)
  );

  Description: Stores tenant-specific configuration settings and preferences.

  roles

  CREATE TABLE roles (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Additional role fields
      PRIMARY KEY (id)
  );

  Description: Defines roles within the system for access control and authorization.

  permissions

  CREATE TABLE permissions (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Additional permission fields
      PRIMARY KEY (id)
  );

  Description: Defines permissions that can be assigned to roles for fine-grained access control.

  permissions_assignments

  CREATE TABLE permissions_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links permissions to roles, defining which permissions each role has access to.

  user

  CREATE TABLE user (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Additional user fields
      PRIMARY KEY (id)
  );

  Description: Stores user account information for the system.

  user_roles

  CREATE TABLE user_roles (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- User role assignment fields
      PRIMARY KEY (id)
  );

  Description: Defines many-to-many relationship between users and roles.

  user_permissions

  CREATE TABLE user_permissions (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- User permission fields
      PRIMARY KEY (id)
  );

  Description: Defines many-to-many relationship between users and permissions.

  users_roles_assignments

  CREATE TABLE users_roles_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Stores user-role assignments with timestamps and metadata.

  users_roles_roles_assignments

  CREATE TABLE users_roles_roles_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Role assignment fields
      PRIMARY KEY (id)
  );

  Description: Stores role-hierarchy relationships (users can be assigned to roles that have other roles).

  items

  CREATE TABLE items (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Item fields
      PRIMARY KEY (id)
  );

  Description: Stores inventory/item information including details about products, goods, or materials in the warehouse.

  items_categories

  CREATE TABLE items_categories (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Category fields
      PRIMARY KEY (id)
  );

  Description: Defines categories for organizing and classifying items in the warehouse.

  items_categories_assignments

  CREATE TABLE items_categories_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links items to categories, supporting multiple categories per item.

  suppliers

  CREATE TABLE suppliers (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Supplier fields
      PRIMARY KEY (id)
  );

  Description: Stores supplier information for procurement and purchasing operations.

  suppliers_contacts

  CREATE TABLE suppliers_contacts (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Contact fields
      PRIMARY KEY (id)
  );

  Description: Stores contact persons for suppliers.

  suppliers_contacts_assignments

  CREATE TABLE suppliers_contacts_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links suppliers to their contact persons.

  customers

  CREATE TABLE customers (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Customer fields
      PRIMARY KEY (id)
  );

  Description: Stores customer information for sales and order management.

  customers_contacts

  CREATE TABLE customers_contacts (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Contact fields
      PRIMARY KEY (id)
  );

  Description: Stores contact persons for customers.

  customers_contacts_assignments

  CREATE TABLE customers_contacts_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links customers to their contact persons.

  locations

  CREATE TABLE locations (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Location fields
      PRIMARY KEY (id)
  );

  Description: Defines warehouse locations, storage areas, and geographical locations within the system.

  items_locations

  CREATE TABLE items_locations (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Location assignment fields
      PRIMARY KEY (id)
  );

  Description: Defines which locations store which items.

  items_locations_assignments

  CREATE TABLE items_locations_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links items to locations where they are stored.

  item_units

  CREATE TABLE item_units (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Unit fields
      PRIMARY KEY (id)
  );

  Description: Defines measurement units (e.g., pieces, kg, meters) used for items.

  item_units_assignments

  CREATE TABLE item_units_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links items to their measurement units.

  item_groups

  CREATE TABLE item_groups (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Group fields
      PRIMARY KEY (id)
  );

  Description: Defines groups for organizing items (e.g., seasonal, promotional, categories).

  item_groups_assignments

  CREATE TABLE item_groups_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links items to their groups.

  locations_groups

  CREATE TABLE locations_groups (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Group fields
      PRIMARY KEY (id)
  );

  Description: Defines groups for organizing locations.

  locations_groups_assignments

  CREATE TABLE locations_groups_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links locations to their groups.

  customers_items

  CREATE TABLE customers_items (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links customers to items they are interested in or order.

  customers_items_assignments

  CREATE TABLE customers_items_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links customers to items they have requested or order.

  orders

  CREATE TABLE orders (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Order fields
      PRIMARY KEY (id)
  );

  Description: Stores order information including order headers with customer and order details.

  orders_items

  CREATE TABLE orders_items (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Order item fields
      PRIMARY KEY (id)
  );

  Description: Stores individual line items within orders.

  orders_items_assignments

  CREATE TABLE orders_items_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links orders to their line items.

  order_items_items

  CREATE TABLE order_items_items (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Item fields
      PRIMARY KEY (id)
  );

  Description: Stores relationship between order items and items.

  order_items_items_assignments

  CREATE TABLE order_items_items_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links order items to items they reference.

  orders_orderers

  CREATE TABLE orders_orderers (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Orderer fields
      PRIMARY KEY (id)
  );

  Description: Stores information about who placed the order (orderer).

  orders_orderers_assignments

  CREATE TABLE orders_orderers_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links orders to their orderers.

  shipments

  CREATE TABLE shipments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Shipment fields
      PRIMARY KEY (id)
  );

  Description: Stores shipment information for order fulfillment and delivery.

  shipments_orders

  CREATE TABLE shipments_orders (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links shipments to their related orders.

  shipments_orders_assignments

  CREATE TABLE shipments_orders_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links shipments to their related orders.

  shipments_items

  CREATE TABLE shipments_items (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Shipment item fields
      PRIMARY KEY (id)
  );

  Description: Stores individual items within shipments.

  shipments_items_assignments

  CREATE TABLE shipments_items_assignments (
      id bigint NOT NULL,
      tenant id null,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links shipments to their items.

  inventory

  CREATE TABLE inventory (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Inventory fields
      PRIMARY KEY (id)
  );

  Description: Stores current inventory levels and stock information for items at various locations.

  inventory_locations

  CREATE TABLE inventory_locations (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Inventory location fields
      PRIMARY KEY (id)
  );

  Description: Links inventory records to locations where items are stored.

  inventory_locations_assignments

  CREATE TABLE inventory_locations_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links inventory records to their storage locations.

  inventory_items

  CREATE TABLE inventory_items (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Inventory item fields
      PRIMARY KEY (id)
  );

  Description: Links inventory records to items they track.

  inventory_items_assignments

  CREATE TABLE inventory_items_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links inventory records to the items they track.

  transfers

  CREATE TABLE transfers (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Transfer fields
      PRIMARY KEY (id)
  );

  Description: Stores transfer records for moving items between locations.

  transfers_locations

  CREATE TABLE transfers_locations (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Transfer location fields
      PRIMARY KEY (id)
  );

  Description: Links transfers to their origin and destination locations.

  transfers_locations_assignments

  CREATE TABLE transfers_locations_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links transfers to their involved locations (origin and destination).

  transfers_items

  CREATE TABLE transfers_items (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Transfer item fields
      PRIMARY KEY (id)
  );

  Description: Stores individual items within transfers.

  transfers_items_assignments

  CREATE TABLE transfers_items_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links transfers to the items being transferred.

  adjustments

  CREATE TABLE adjustments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Adjustment fields
      PRIMARY KEY (id)
  );

  Description: Stores inventory adjustment records for stock corrections and盘点.

  adjustments_items

  CREATE TABLE adjustments_items (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Adjustment item fields
      PRIMARY KEY (id)
  );

  Description: Links adjustments to the items being adjusted.

  adjustments_items_assignments

  CREATE TABLE adjustments_items_assignments (
      id bigint NOT NULL,
      tenant_id bigint NULL,
      -- Assignment fields
      PRIMARY KEY (id)
  );

  Description: Links adjustments to the items they affect.

  Views

  tenant_inventory_summary

  CREATE VIEW tenant_inventory_summary AS
  SELECT
      ti.tenant_id,
      ti.item_id,
      ti.location_id,
      SUM(ti.quantity) as total_quantity,
      MIN(ti.created_at) as first_recorded_at,
      MAX(ti.updated_at) as last_updated_at
  FROM inventory ti
  JOIN inventory_locations il ON ti.id = il.inventory_id
  JOIN inventory_locations_assignments ila ON il.id =  ila.id
  JOIN inventory_items ii ON il.inventory_id = ii.id
  JOIN inventory_items_assignments iia ON il.inventory_id = iia.id
  GROUP BY ti.tenant_id, ti.item_id, ti.location_id;

  Description: Provides a summarized inventory view per tenant, item, and location, aggregating quantities and timestamps from raw inventory records.

  tenant_order_summary

  CREATE VIEW tenant_order_summary AS
  SELECT
      o.tenant_id,
      o.order_id,
      o.customer_id,
      o.order_date,
      o.total_amount,
      o.status,
      (SELECT COUNT(*) FROM orders_items oi WHERE oi.order_id = o.order_id) as line_item_count
  FROM orders o
  GROUP BY o.tenant_id, o.order_id, o.customer_id, o.order_date, o.total_amount, o.status;

  Description: Provides a summarized order view per tenant and order, including basic order metadata and line item count.

  tenant_sales_performance

  CREATE VIEW tenant_sales_performance AS
  SELECT
      o.tenant_id,
      o.customer_id,
      o.order_date,
      o.total_amount,
      SUM(oi.quantity) as total_units_sold,
      COUNT(DISTINCT o.order_id) as order_count
  FROM orders o
  JOIN orders_items oi ON o.order_id = oi.order_id
  GROUP BY o.tenant_id, o.customer_id, o.order_date, o.total_amount
  ORDER BY o.tenant_id, o.customer_id, o.order_date;

  Description: Provides sales performance metrics per tenant, customer, and date, including revenue, units sold, and order count.

  tenant_items_by_category

  CREATE VIEW tenant_items_by_category AS
  SELECT
      i.id as item_id,
      ic.name as category_name,
      ic.tenant_id,
      COUNT(DISTINCT ic.id) as category_count,
      SUM(i.quantity) as total_quantity
  FROM items i
  JOIN items_categories_assignments ica ON i.id = ica.item_id
  JOIN items_categories ic ON ica.category_id = ic.id
  GROUP BY i.id, ic.name, ic.tenant_id
  ORDER BY ic.tenant_id, ic.name;

  Description: Provides item categorization with quantities, showing which items belong to which categories.

  Notes

  1. Tenant Isolation: All tables include a tenant_id column for multi-tenant isolation.
  2. Soft Deletes: Most tables have a deleted_at column for soft deletes.
  3. Audit Trail: Timestamps (created_at, updated_at) are maintained for audit purposes.