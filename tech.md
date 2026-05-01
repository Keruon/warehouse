# Technology Stack

  ## Overview

  This document outlines the technology stack for the warehouse stock management application, designed for self-hosted deployment on Debian Linux.

  ## 1. Backend (API Layer)

  ### Core Framework
  - **Framework**: ASP.NET Core 8.0+ (Latest LTS)
  - **Language**: C# 12+
  - **Hosting**: Self-hosted on Debian Linux (systemd/Windows Service as appropriate)
  - **Protocol**: HTTPS (production), HTTP (development)

  ### Key Dependencies
  - **ASP.NET Core Web API**: RESTful API foundation
  - **Minimal APIs**: For lightweight endpoints
  - **MediatR**: CQRS pattern for clean architecture
  - **FluentValidation**: Request validation
  - **AutoMapper**: Object-mapping
  - **Entity Framework Core**: ORM with code-first approach
  - **Serilog**: Structured logging to PostgreSQL
  - **BCrypt.Net**: Password hashing
  - **JWT (System.Text.Json + Microsoft.IdentityModel.Tokens)**: Token-based auth
  - **Npgsql**: PostgreSQL driver

  ### Architecture Pattern
  - Clean Architecture / Onion Architecture
  - Dependency Injection via built-in DI container
  - Repository Pattern with EF Core
  - Unit-of-Work pattern for transactional operations

  ## 2. Frontend (Web Interface)

  ### Core Framework
  - **Framework**: React 18+
  - **TypeScript**: Type-safe development
  - **Build Tool**: Vite (faster builds, modern tooling)
  - **Routing**: React Router v6+
  - **State Management**: Context API + useReducer (for simplicity)

  ### Styling & UI
  - **CSS Framework**: Tailwind CSS (utility-first)
  - **Component Library**: Radix UI primitives or Shadcn/ui (if using npm packages)
  - **Icons**: Lucide React
  - **Responsive**: Mobile-first design (for handheld scanners)

  ### Key Dependencies
  - **axios**: HTTP client
  - **zustand**: Lightweight state management (optional, for complex state)
  - **html2canvas**: QR code label generation
  - **qrcode.react**: QR code generation for labels
  - **react-hot-toast**: Notifications
  - **react-hook-form**: Form handling with validation
  - **date-fns**: Date/time utilities

  ### Browser Support
  - Modern browsers (Chrome, Firefox, Edge, Safari)
  - No IE support
  - Minimum: Chrome 90+

  ## 3. Database Layer

  ### Primary Database
  - **Type**: PostgreSQL 15+
  - **Driver**: Npgsql.EntityFrameworkCore.PostgreSQL
  - **Migrations**: EF Core Migrations

  ### Alternative Storage (Future)
  - Redis for session caching (optional)
  - In-memory cache for read-heavy operations

  ### Database Design
  - Normalized schema (3NF)
  - Soft deletes (IsDeleted flag)
  - Audit tables for operations tracking
  - Configurable retention policies via triggers or application logic

  ## 4. Infrastructure & DevOps

  ### Operating System
  - **OS**: Debian 12 (Bookworm) or Ubuntu 22.04 LTS
  - **Runtime**: .NET 8.0 Runtime
  - **Package Manager**: apt

  ### Deployment Configuration
  - **Reverse Proxy**: Nginx
  - **Process Manager**: systemd
  - **SSL**: Let's Encrypt (certbot) or custom certificates
  - **Environment Variables**: .env files per environment

  ### Docker Support (Optional)
  - Multi-stage Dockerfiles for production
  - Docker Compose for local development

  ### Health Checks
  - `/health` endpoint for monitoring
  - Prometheus-compatible metrics (optional)

  ## 5. Security

  ### Authentication Strategy
  - **JWT Access Tokens**: Short-lived (15-30 min)
  - **Refresh Tokens**: Stored in PostgreSQL, rotatable
  - **Password Hashing**: BCrypt (work factor 12)
  - **Token Storage**: Hybrid (stateless JWT + stateful refresh store)

  ### Token Security
  - JWT tokens hashed in database (additional layer)
  - Device-bound sessions
  - Explicit revocation support

  ### Session Management
  - Refresh token rotation on use
  - Device fingerprinting for binding
  - Automatic logout on account lockout

  ### Audit Logging
  - All sensitive operations logged
  - Retention configurable (1-10 years)
  - Separate audit table with indexes for performance

  ## 6. Development Tools

  ### Version Control
  - Git (master/main branches)
  - Pre-commit hooks (commit-msg)
  - Branch protection rules

  ### Build & Test
  - **Backend**: dotnet test, dotnet build
  - **Frontend**: npm run build, npm run test
  - **Linting**: ESLint, ESLint-plugin-import, Prettier
  - **Formatting**: prettier, prettier-plugin-tailwindcss

  ### CI/CD (Future)
  - GitHub Actions or GitLab CI
  - Automated testing, build, deploy

  ## 7. API Design

  ### RESTful Conventions
  - **Nouns**: Plural nouns in paths (`/items`, `/locations`)
  - **Verbs**: Standard HTTP verbs (GET, POST, PUT, PATCH, DELETE)
  - **Status Codes**: Semantic HTTP codes
  - **Pagination**: Query parameters (`?page=1&pageSize=50`)
  - **Filtering**: `$filter` (OData) or custom query params

  ### Versioning
  - URL-based versioning (`/api/v1/`)
  - Breaking changes to new major versions

  ### Error Handling
  - Standardized error response format
  - CORS configuration
  - Rate limiting (minimal, as per requirements)

  ## 8. Performance Considerations

  ### Caching Strategy
  - **Redis** (optional): For frequently accessed data
  - **Memory Cache**: For transient data
  - **EF Core**: Query caching via SqlProvider

  ### Database Optimization
  - Indexes on foreign keys
  - Computed columns for frequently queried fields
  - Partitioning for audit logs (future)

  ## 9. Monitoring & Observability

  ### Logging
  - **Backend**: Serilog to PostgreSQL/Console
  - **Frontend**: Browser console (development only)
  - **Structured Logging**: JSON format for aggregation

  ### Metrics (Optional)
  - Prometheus exporters
  - Grafana dashboards (future)
  - Health check endpoints

  ## 10. Future Considerations

  ### Potential Enhancements
  - Kubernetes/Helm charts for orchestration
  - Horizontal scaling for API
  - GraphQL subgraph for complex queries
  - Real-time updates via WebSockets/SSE
  - Offline mode with service worker

  ### Compliance
  - GDPR data minimization
  - Audit trail retention policies
  - Data encryption at rest (PostgreSQL TDE or external KMS)