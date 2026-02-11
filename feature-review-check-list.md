# Feature Review Checklist

Cross-reference of assignment requirements vs current implementation status.

**Legend**: DONE = implemented | GAP = missing/incomplete | N/A = not chosen

---

## Mandatory: Section A - Core Slice

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| A1 | Working thin slice (backend + frontend) | DONE | Full E2E: Create Patient, List Patients, Create/List Appointments |
| A1 | REST API with request validation | DONE | FluentValidation on all Create endpoints |
| A1 | Consistent error responses | DONE | `ExceptionHandlingMiddleware` returns structured `{ error, message }` |
| A1 | Persistence in PostgreSQL (migrations required) | DONE | EF Core migration `InitialCreate.cs`, auto-applies on startup via `MigrateAsync()` |
| A1 | Tenant-safe filtering on all reads/writes | DONE | Global Query Filter + explicit `tenantId` param validated against JWT |
| A2 | Create Patient: FirstName, LastName, PhoneNumber, TenantId, CreatedAt | DONE | `POST /api/patients` |
| A2 | PhoneNumber unique within same Tenant | DONE | DB unique index `(TenantId, PhoneNumber)` + pre-check + friendly 409 error |
| A2 | (Optional) PrimaryBranchId | DONE | Patient has `PrimaryBranchId` field |
| A3 | List Patients: Required filter TenantId | DONE | `GET /api/patients?tenantId=` mandatory param |
| A3 | List Patients: Optional filter BranchId | DONE | `?branchId=` optional param |
| A3 | Sorted by CreatedAt DESC | DONE | `.OrderByDescending(p => p.CreatedAt)` |
| A3 | Pagination | DONE | `?page=&pageSize=` with total count response |
| A3 | Explain PrimaryBranchId vs mapping table choice | **GAP** | README mentions PrimaryBranchId exists but no explicit design rationale |
| - | Frontend: Create patient form | DONE | `/patients` page with modal form |
| - | Frontend: List patients | DONE | Table with pagination |
| - | Frontend: Filter by Branch | DONE | `BranchFilter` component dropdown |

## Mandatory: Section B - Authorization & User Management

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| B1 | Roles: Admin, User, Viewer | DONE | JWT `role` claim, 3 authorization policies |
| B2 | Permission: Creating patients | DONE | `[Authorize(Policy = "CanCreatePatient")]` - Admin + User |
| B2 | Permission: Viewing patients | DONE | `[Authorize]` - all authenticated users |
| B2 | Permission: Creating appointments | DONE | `[Authorize(Policy = "CanCreateAppointment")]` - Admin + User |
| B3 | API: Create User | DONE | `POST /api/users` (AdminOnly) |
| B3 | API: Assign Role | DONE | `PUT /api/users/{id}/role` (AdminOnly) |
| B3 | API: Associate User with Tenant + Branches | DONE | CreateUser accepts `branchIds[]`, auto-inherits `TenantId` |
| B3 | Authenticated identity (JWT/cookie/token) | DONE | JWT with `sub`, `tenant_id`, `role`, `branch_ids` claims |
| B3 | Authorization enforced server-side | DONE | ASP.NET Core authorization policies + `[Authorize]` attributes |
| B3 | Viewer cannot create patients | DONE | Policy enforced + integration test proves 403 |
| B4 | Seeder: 1 Tenant | DONE | "Clinic Siam" |
| B4 | Seeder: 2 Branches | DONE | "Branch Sukhumvit", "Branch Silom" |
| B4 | Seeder: Users for each role | DONE | admin (Admin), user1 (User), viewer1 (Viewer) |
| B4 | Correct tenant/branch associations | DONE | All users under Clinic Siam, admin both branches |
| B4 | Runnable via one command | DONE | Auto-runs in `Program.cs` on startup |

## Chosen: Section C - Appointment + Messaging

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| C1 | Create Appointment: TenantId, BranchId, PatientId, StartAt, CreatedAt | DONE | `POST /api/appointments` |
| C1 | List Appointments (bonus, not required) | DONE | `GET /api/appointments?tenantId=&branchId=` |
| C2 | Prevent duplicate: same Patient + StartAt + Branch within Tenant | DONE | DB unique index `(TenantId, PatientId, BranchId, StartAt)` |
| C2 | Concurrency-safe (DB constraint) | DONE | Unique constraint + `DbUpdateException` catch for 23505 |
| C2 | Friendly error | DONE | Returns 409 with `DuplicateAppointment` error type |
| C3 | Publish event to RabbitMQ | DONE | `appointment.created` routing key |
| C3 | Payload includes TenantId | DONE | `AppointmentCreatedEvent` record with TenantId field |
| C3 | Consumer optional | N/A | No consumer (acceptable per spec) |

## Chosen: Section D - Caching & Data Access

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| D1 | Cache at least one read path (List Patients) | DONE | `PatientService.ListAsync()` uses `ICacheService` |
| D2 | Tenant-scoped cache keys | DONE | `tenant:{tenantId}:patients:branch:{branchId}:p:{page}:s:{size}` |
| D3 | Invalidation on Create Patient | DONE | `RemoveByPrefixAsync("tenant:{tenantId}:patients:")` |
| D3 | Invalidation on Create Appointment | DONE | `RemoveByPrefixAsync("tenant:{tenantId}:appointments:")` |

## Not Chosen: Section E - Architecture & Evolution

| # | Requirement | Status | Notes |
|---|-------------|--------|-------|
| E1 | Define one domain event | N/A | Not chosen (but `AppointmentCreatedEvent` exists from C3) |
| E2 | Tenant isolation strategy in README | **PARTIAL** | README has "Multi-Tenant Isolation Strategy" section already - could count as E2 |
| E3 | Microservice-ready | N/A | Not chosen |

## Mandatory: Execution Requirements

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| - | `docker compose up --build` starts all 5 services | DONE | PostgreSQL, Redis, RabbitMQ, backend, frontend |
| - | Migrations apply automatically | DONE | `db.Database.MigrateAsync()` in `Program.cs` |
| - | Test 1: tenant scoping enforced | DONE | `TenantScopingEnforced_PatientInTenantA_NotVisibleFromTenantB` |
| - | Test 2: duplicate phone prevented | DONE | `DuplicatePhonePrevented_Returns409` |
| - | Test 3: frontend or integration smoke test | **GAP** | Only 5 backend tests, no frontend/smoke test |

## Mandatory: Deliverables

| # | Requirement | Status | Notes |
|---|-------------|--------|-------|
| - | `/src/backend` | DONE | .NET 10 Clean Architecture (5 projects) |
| - | `/src/frontend` | DONE | Next.js 15 + TypeScript + Tailwind |
| - | `README.md` | **GAP** | See gaps below |
| - | `AI_PROMPTS.md` | DONE | Covers prompts, iterations, accepted/rejected outputs |

### README.md Detail Check

| Requirement | Status | Notes |
|-------------|--------|-------|
| Architecture overview (tenant safety) | DONE | "Multi-Tenant Isolation Strategy" section |
| Assumptions and tradeoffs | **GAP** | Missing section |
| How to run (one command) | DONE | `docker compose up --build` |
| Environment variables (.env.example) | **GAP** | File exists but has stale port `5000` (should be `5001`) |
| Seeded users and how to login | DONE | Table with users + password |
| API examples (curl) | **GAP** | Only shows endpoint signatures, no curl examples |
| How to run tests | DONE | `cd src/backend && dotnet test` with test table |
| GET /api/appointments documented | **GAP** | Only POST shown, missing GET endpoint |
| GET /api/users documented | **GAP** | Only POST/PUT shown, missing GET endpoint |

---

## Summary

### Completed (score: strong)
- **Section A**: 100% - all core slice requirements met
- **Section B**: 100% - auth, RBAC, user management, seeder all working
- **Section C**: 100% - appointments, duplicate prevention, RabbitMQ event
- **Section D**: 100% - Redis cache with tenant-scoped keys and invalidation
- **Tests**: 5 backend integration tests (exceeds 2 required backend tests)
- **Docker**: One-command startup with health checks

### Gaps to Fix (priority order)

| Priority | Gap | Effort | Impact |
|----------|-----|--------|--------|
| **HIGH** | Add 1 frontend/integration smoke test | 10 min | Required by spec (3rd mandatory test) |
| **HIGH** | README: Add curl examples for all endpoints | 10 min | Explicitly required |
| **HIGH** | README: Add "Assumptions and Tradeoffs" section | 5 min | Explicitly required |
| **MED** | README: Add GET /api/appointments + GET /api/users docs | 3 min | API docs completeness |
| **MED** | README: Add PrimaryBranchId design rationale | 2 min | Spec says "explain your choice" |
| **LOW** | .env.example: Fix port 5000 -> 5001 | 1 min | Correctness |
| **LOW** | Remove unused `CreateAppointmentForm.tsx` | 1 min | Cleanup dead code |
