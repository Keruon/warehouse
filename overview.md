  # Project Overview: Warehouse Stock Management System

  1. Executive Summary

  The Warehouse Stock Management System (WSMS) is a web-based application designed to manage inventory for electronic components in a warehouse environment. The system provides
  comprehensive tracking and management of components across a hierarchical storage structure (Areas → Shelves → Locations), supporting operations for receiving, gathering, transferring,
  and inventory monitoring.

  The application is built for self-hosted deployment on Debian Linux and is designed for use with handheld scanning devices and standard HTML5 browsers.

  2. Project Goals

  - Enable efficient tracking and management of electronic component inventory
  - Provide a hierarchical storage structure (Areas → Shelves → Locations)
  - Support stock level tracking with optional batch/lot tracking
  - Facilitate warehouse operations via web interface and QR code scanning
  - Maintain audit trails for all inventory operations
  - Support role-based access control (Admin and User roles)
  - Enable self-hosted deployment with PostgreSQL database

  3. Scope & Boundaries

  In Scope

  - Component inventory management (receiving, gathering, transfers)
  - Hierarchical warehouse structure (Areas → Shelves → Locations)
  - Stock level tracking by component type
  - Optional lot/batch tracking
  - QR code-based item identification
  - User authentication and authorization
  - Audit logging
  - Search and filtering capabilities
  - Bulk transfer operations
  - Role-based access control

  Out of Scope (Initial Release)

  - External system integrations
  - Offline mode for handheld units
  - Advanced reporting and analytics
  - Mobile applications
  - Rate limiting on API endpoints

  4. Technical Requirements

  Platform Requirements

  - Frontend: React web application
  - Backend: ASP.NET Core API
  - Database: PostgreSQL
  - Deployment: Self-hosted on Debian Linux
  - Browser Support: Standard HTML5 browsers
  - Authentication: Hybrid JWT + PostgreSQL session store

  Infrastructure

  - No rate limiting (handful of users)
  - Online-only operation for handheld scanning
  - Device-bound session management
  - Configurable audit retention (1–10 years, default 1 year)

  5. Success Criteria

  - System enables users to scan QR codes and view/update inventory
  - Admin users can manage warehouse structure (areas, shelves, locations)
  - All inventory operations are audited with user, timestamp, and details
  - Users can search and filter inventory by component attributes
  - Stock levels can be tracked and managed across the warehouse hierarchy
  - System is stable with no data loss or corruption
  - Authentication and authorization work correctly across all operations

  ---
  Status: Draft - Pending detailed implementation planning
  Last Updated: 2026-03-19
  Version: 0.1.0