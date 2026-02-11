# AI Prompts Log

This document records the AI-assisted development process for the Clinic POS assignment.

## Tool Used

Claude Code (Claude Opus 4.6) - CLI-based AI coding assistant

## Development Approach

The project was developed using a multi-agent team approach:
- **Team Lead**: Orchestrated overall architecture, created SPEC.md, coordinated agents, handled integration
- **backend-dev**: .NET 10 Clean Architecture implementation (Domain, Application, Infrastructure, API layers)
- **frontend-dev**: Next.js 15 frontend with TypeScript and Tailwind CSS
- **infra-dev**: Docker Compose, Dockerfiles, and infrastructure configuration

## Prompt Sequence

### 1. Requirements Analysis
**Prompt**: Read the assignment PDF and summarize requirements
**Output**: Identified mandatory sections (A, B) and optional sections. Selected C (Appointments + Messaging) and D (Caching).
**Accepted**: Yes

### 2. Architecture Design
**Prompt**: Design with Clean Architecture, pay attention to multi-tenant safety
**Output**: Four-layer Clean Architecture (Domain -> Application -> Infrastructure -> API), two-layer tenant isolation strategy
**Accepted**: Yes, with modification - user specified tenantId must be explicit query parameter, not just derived from JWT

### 3. Technical Specification
**Prompt**: Write SPEC.md with database schema, API endpoints, screen mapping
**Output**: Complete technical specification covering all chosen sections
**Accepted**: Yes, revised API design after user feedback on tenant ID handling

### 4. API Design Correction (Key Iteration)
**Prompt**: User feedback - "tenantId จะต้องอยู่ใน filter เลย" (tenantId must be in the filter directly)
**Before**: GET /api/patients relied solely on JWT tenant_id claim via EF Core Global Query Filter
**After**: GET /api/patients requires explicit `tenantId` query parameter, validated against JWT claim in service layer
**Accepted**: Yes - this was a critical design correction for multi-tenant safety

### 5. Backend Implementation
**Prompt**: Implement .NET 10 backend with Clean Architecture
**Output**: Full implementation across 5 projects (Domain, Application, Infrastructure, API, Tests)
**Key decisions**:
- Used EF Core Global Query Filters for automatic tenant scoping
- BCrypt for password hashing
- FluentValidation for request validation
- RabbitMQ with graceful degradation (app works without RabbitMQ)
- Redis with tenant-scoped cache keys
**Accepted**: Yes

### 6. Frontend Implementation
**Prompt**: Implement Next.js frontend with App Router
**Output**: 15 TypeScript/TSX files (pages, components, lib)
**Key decisions**:
- Client-side auth with localStorage token management
- getTenantId() helper reads from stored user object
- BranchFilter component for patient list filtering
- Role-based UI (Viewer cannot see create buttons)
**Accepted**: Yes

### 7. Infrastructure Setup
**Prompt**: Create Docker Compose with health checks
**Output**: docker-compose.yml with 5 services, multi-stage Dockerfiles
**Accepted**: Yes, with config key fixes (Redis connection string, JWT key naming)

### 8. Integration Tests
**Prompt**: Write integration tests for critical paths
**Output**: 5 xUnit tests using WebApplicationFactory with InMemory database
**Tests cover**: Tenant isolation, tenant ID mismatch (403), duplicate phone (409), viewer authorization (403), duplicate appointment (409)
**Accepted**: Yes

### 9. Seeder Implementation
**Prompt**: B4 requirement - seed 1 Tenant, 2 Branches, users for each role
**Output**: DataSeeder.cs with idempotent seeding, auto-runs on startup via Program.cs
**Accepted**: Yes

## Rejected/Modified Outputs

1. **Initial API design**: GET /api/patients without tenantId query param was rejected. User required explicit tenant filtering.
2. **Docker config keys**: Initial `Redis__ConnectionString` and `Jwt__Secret` env vars didn't match Program.cs config reading patterns. Fixed to `ConnectionStrings__Redis` and `Jwt__Key`.
3. **Section E selection**: Initial suggestion included Section E, but user chose Section C + D instead.

## Iterations Summary

| Area | Iterations | Reason |
|------|-----------|--------|
| API Design (tenantId) | 2 | User correction on multi-tenant filtering |
| Docker Config | 2 | Config key naming mismatch |
| Integration Tests | 2 | Test factory updated for proper test isolation |
| Frontend files | 1 | Created directly from agent output |
| Backend layers | 1 | Clean implementation on first pass |

## Time Spent

Total development time: ~45 minutes with AI assistance
- Spec writing: ~10 min
- Backend implementation: ~15 min (parallel with frontend)
- Frontend implementation: ~10 min (parallel with backend)
- Infrastructure + Testing: ~10 min
