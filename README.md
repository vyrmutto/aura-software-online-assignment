# Clinic POS - Multi-Tenant Clinic Management Platform

A multi-tenant, multi-branch B2B clinic management system built with .NET 10 and Next.js 15.

## Architecture

```
src/
  backend/                    # .NET 10 Clean Architecture
    ClinicPOS.Domain/         # Entities, Interfaces (zero dependencies)
    ClinicPOS.Application/    # Services, DTOs, Validators
    ClinicPOS.Infrastructure/ # EF Core, Redis, RabbitMQ, JWT
    ClinicPOS.API/            # Controllers, Middleware, Seeder
    ClinicPOS.Tests/          # Integration tests (xUnit)
  frontend/                   # Next.js 15 (App Router, TypeScript, Tailwind)
docker-compose.yml            # One-command startup
```

### Clean Architecture Layers

- **Domain**: Entities (`Tenant`, `Branch`, `Patient`, `User`, `Appointment`), interfaces (`ITenantContext`, `IAppDbContext`)
- **Application**: Business logic services, DTOs, FluentValidation validators. No infrastructure dependencies.
- **Infrastructure**: EF Core with PostgreSQL, Redis caching, RabbitMQ messaging, JWT auth
- **API**: ASP.NET Core controllers, middleware (exception handling, tenant context), seeder

### Multi-Tenant Isolation Strategy

Two-layer defense-in-depth approach:

1. **EF Core Global Query Filter**: All entities with `TenantId` are automatically filtered via `builder.HasQueryFilter(e => e.TenantId == tenantId)` in `AppDbContext`. This ensures no cross-tenant data leakage at the database query level.

2. **Explicit `tenantId` Query Parameter**: The `GET /api/patients` endpoint requires `tenantId` as a mandatory query parameter. The service layer validates this against the JWT's `tenant_id` claim. If they don't match, a `403 Forbidden` is returned.

This prevents both accidental data leakage (via query filter) and deliberate tenant spoofing (via parameter validation).

## Quick Start

### Prerequisites
- Docker & Docker Compose

### One-Command Startup

```bash
docker compose up --build
```

This starts all 5 services:

| Service   | Port  | Description                         |
|-----------|-------|-------------------------------------|
| Frontend  | 3000  | Next.js UI                          |
| Backend   | 5001  | .NET 10 API                         |
| PostgreSQL| 5432  | Database                            |
| Redis     | 6379  | Cache                               |
| RabbitMQ  | 5672  | Message broker (UI at 15672)        |

The backend automatically runs EF Core migrations and seeds data on startup.

## Seeded Data (B4)

| Entity   | Name             | Details                          |
|----------|------------------|----------------------------------|
| Tenant   | Clinic Siam      | -                                |
| Branch   | Branch Sukhumvit | Tenant: Clinic Siam              |
| Branch   | Branch Silom     | Tenant: Clinic Siam              |
| User     | admin            | Role: Admin, Branches: both      |
| User     | user1            | Role: User, Branch: Sukhumvit    |
| User     | viewer1          | Role: Viewer, Branch: Silom      |
| Patient  | Somchai Jaidee   | Branch: Sukhumvit                |
| Patient  | Narin Pongpat    | Branch: Silom                    |

All users have password: `password123`

## API Endpoints

### Authentication
```
POST /api/auth/login
Body: { "username": "admin", "password": "password123" }
Response: { "token": "...", "user": { "id", "username", "role", "tenantId", "branchIds" } }
```

### Patients
```
GET  /api/patients?tenantId={id}&branchId={id}&page=1&pageSize=20
POST /api/patients
Body: { "firstName": "...", "lastName": "...", "phoneNumber": "...", "primaryBranchId": "..." }
```

### Appointments (Section C)
```
POST /api/appointments
Body: { "branchId": "...", "patientId": "...", "startAt": "2026-02-15T10:00:00Z" }
```

### Branches
```
GET /api/branches
```

### Users (Admin only)
```
POST /api/users
PUT  /api/users/{id}/role
```

All endpoints (except login) require `Authorization: Bearer <token>` header.

## Sections Implemented

### A - Core Slice (Mandatory)
- A1: Create Patient (POST /api/patients)
- A2: Create Patient screen (frontend form with validation)
- A3: List Patients with pagination and branch filter
- A4: Patient list screen with pagination

### B - Auth & User Management (Mandatory)
- B1: JWT login with role-based claims
- B2: RBAC (Admin, User, Viewer) with authorization policies
- B3: Tenant-scoped queries via Global Query Filter + explicit validation
- B4: Seeder with 1 Tenant, 2 Branches, 3 Users (Admin/User/Viewer)

### C - Appointments + Messaging (Optional)
- C1: Create Appointment endpoint
- C2: Duplicate booking prevention (unique constraint on branch+patient+startAt per tenant)
- C3: RabbitMQ event publish on appointment creation (`appointment.created` routing key)

### D - Caching (Optional)
- D1: Redis cache on patient list read path (5-minute TTL)
- D2: Tenant-scoped cache keys (`tenant:{id}:patients:branch:{id}:p:{page}:s:{size}`)
- D3: Cache invalidation on patient creation (removes all cached pages for tenant)

### E - Architecture & Evolution (Bonus — E1 + E2)

#### E1: Domain Event — `AppointmentCreatedEvent`

**Event definition** (`ClinicPOS.Domain/Events/AppointmentCreatedEvent.cs`):
```csharp
public record AppointmentCreatedEvent(
    Guid EventId, string EventType, Guid TenantId,
    Guid AppointmentId, Guid BranchId, Guid PatientId,
    DateTime StartAt, DateTime OccurredAt);
```

**Where it's emitted**: `AppointmentService.CreateAsync()` — after `SaveChangesAsync()` persists the appointment to PostgreSQL and cache is invalidated.

**Delivery**: Published to RabbitMQ with routing key `appointment.created` via `IEventPublisher.PublishAsync()`. The publisher uses a dedicated channel per call with `BasicPublishAsync()`.

**Guarantees**:
- **At-most-once delivery**: The event is published *after* the DB transaction commits but *outside* the transaction boundary. If RabbitMQ is unavailable, the appointment is still created (graceful degradation) — the publish failure is logged but does not roll back the DB write.
- **Not outbox-based**: This is a pragmatic choice for v1. For stronger guarantees (exactly-once / at-least-once), an outbox pattern with a polling publisher or transactional outbox via PostgreSQL `LISTEN/NOTIFY` would be the next evolution.
- **Idempotent consumers**: The event includes a unique `EventId` (GUID), allowing consumers to deduplicate if the same event were ever published twice.

**Trade-off**: We chose simplicity over guaranteed delivery. For a Clinic POS v1, losing an occasional event (e.g., notification) is acceptable. Critical business state lives in PostgreSQL, not in the event stream.

#### E2: Tenant Isolation Strategy

**How TenantId is derived**:
1. User logs in via `POST /api/auth/login` → JWT issued with `tenant_id` claim (from `User.TenantId` in DB)
2. On each request, `TenantContextMiddleware` extracts `tenant_id` from the JWT claims and sets it on the scoped `TenantContext` service
3. `TenantContext` implements `ITenantContext` interface (Domain layer), making tenant identity available to all Application/Infrastructure layers without coupling to HTTP

**How it's enforced in the data access layer** (defense-in-depth):

| Layer | Mechanism | Protects Against |
|-------|-----------|-----------------|
| **EF Core Global Query Filter** | `HasQueryFilter(e => e.TenantId == tenantId)` on Patient, Appointment, User, Branch entities | Accidental cross-tenant reads — impossible to forget WHERE clause |
| **Service-layer validation** | `if (tenantId != _tenant.TenantId) throw ForbiddenException` | Deliberate tenant spoofing — user cannot query another tenant's data even by guessing their tenantId |
| **DB unique constraints** | `HasIndex(p => new { p.TenantId, p.PhoneNumber }).IsUnique()` | Cross-tenant uniqueness collision — phone numbers are unique per-tenant, not globally |

**How we prevent accidental missing filters**:
- Global Query Filters are registered once in `AppDbContext.OnModelCreating()` and apply automatically to **every** LINQ query — developers cannot accidentally skip them
- The `ITenantContext` is injected via DI into every service; there is no way to construct a service without a tenant context in the running application
- Integration tests explicitly verify isolation: `TenantScopingEnforced` test creates data in Tenant A and asserts it's invisible from Tenant B's context
- Write operations (`CreateAsync`) always set `TenantId = _tenant.TenantId` from the JWT — the client cannot override it via the request body

## Running Tests

```bash
cd src/backend
dotnet test --logger "console;verbosity=detailed"
```

### Test Coverage (5 tests)

| Test | Description |
|------|-------------|
| TenantScopingEnforced | Patient in Tenant A not visible from Tenant B |
| TenantIdMismatch_Returns403 | Query param tenantId must match JWT tenant |
| DuplicatePhonePrevented_Returns409 | Same phone number in same tenant rejected |
| ViewerCannotCreatePatient_Returns403 | Viewer role cannot create patients |
| DuplicateAppointmentPrevented_Returns409 | Same appointment slot rejected |

Tests use InMemory database with no-op cache/event publisher replacements.

## Local Development (without Docker)

### Backend
```bash
cd src/backend
dotnet run --project ClinicPOS.API
```
Requires: .NET 10 SDK, PostgreSQL, Redis, RabbitMQ running locally.

### Frontend
```bash
cd src/frontend
npm install
npm run dev
```
Requires: Node.js 22+

## Technology Stack

| Component       | Technology                    |
|----------------|-------------------------------|
| Backend        | .NET 10 / C# / ASP.NET Core  |
| Frontend       | Next.js 15 / React 19 / TypeScript |
| Database       | PostgreSQL 16                 |
| ORM            | Entity Framework Core 10      |
| Cache          | Redis 7 (StackExchange.Redis) |
| Message Broker | RabbitMQ 3 (RabbitMQ.Client)  |
| Auth           | JWT (BCrypt password hashing) |
| Validation     | FluentValidation              |
| Testing        | xUnit + WebApplicationFactory |
| Styling        | Tailwind CSS 4                |
| Containerization | Docker Compose              |
