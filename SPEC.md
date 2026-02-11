# Clinic POS - Technical Specification

## Sections Covered

- **A (mandatory)**: Core slice - Create/List Patients
- **B (mandatory)**: Authorization & User Management
- **C (optional)**: Appointment + Messaging (C1, C2, C3)
- **D (optional)**: Caching & Data Access (D1, D2, D3)
- **E2**: Tenant Isolation Strategy (in README)

---

## 1. Database Schema

### tenants

| Column     | Type         | Constraints              |
|------------|--------------|--------------------------|
| id         | UUID         | PK, DEFAULT gen_random_uuid() |
| name       | VARCHAR(200) | NOT NULL                 |
| created_at | TIMESTAMPTZ  | NOT NULL DEFAULT NOW()   |

### branches

| Column     | Type         | Constraints                    |
|------------|--------------|--------------------------------|
| id         | UUID         | PK, DEFAULT gen_random_uuid()  |
| name       | VARCHAR(200) | NOT NULL                       |
| tenant_id  | UUID         | FK -> tenants(id), NOT NULL    |
| created_at | TIMESTAMPTZ  | NOT NULL DEFAULT NOW()         |

- INDEX: `ix_branches_tenant_id` ON (tenant_id)

### users

| Column        | Type         | Constraints                    |
|---------------|--------------|--------------------------------|
| id            | UUID         | PK, DEFAULT gen_random_uuid()  |
| username      | VARCHAR(100) | NOT NULL, UNIQUE               |
| password_hash | VARCHAR(256) | NOT NULL                       |
| role          | VARCHAR(20)  | NOT NULL, CHECK IN (Admin, User, Viewer) |
| tenant_id     | UUID         | FK -> tenants(id), NOT NULL    |
| created_at    | TIMESTAMPTZ  | NOT NULL DEFAULT NOW()         |

### user_branches

| Column    | Type | Constraints                   |
|-----------|------|-------------------------------|
| user_id   | UUID | FK -> users(id), NOT NULL     |
| branch_id | UUID | FK -> branches(id), NOT NULL  |

- PRIMARY KEY: (user_id, branch_id)

### patients

| Column            | Type         | Constraints                    |
|-------------------|--------------|--------------------------------|
| id                | UUID         | PK, DEFAULT gen_random_uuid()  |
| first_name        | VARCHAR(100) | NOT NULL                       |
| last_name         | VARCHAR(100) | NOT NULL                       |
| phone_number      | VARCHAR(20)  | NOT NULL                       |
| tenant_id         | UUID         | FK -> tenants(id), NOT NULL    |
| primary_branch_id | UUID         | FK -> branches(id), NULLABLE   |
| created_at        | TIMESTAMPTZ  | NOT NULL DEFAULT NOW()         |

- **UNIQUE**: `uq_patients_tenant_phone` ON (tenant_id, phone_number) -- A2: per-tenant phone uniqueness
- INDEX: `ix_patients_tenant_created` ON (tenant_id, created_at DESC)

### appointments

| Column     | Type        | Constraints                    |
|------------|-------------|--------------------------------|
| id         | UUID        | PK, DEFAULT gen_random_uuid()  |
| tenant_id  | UUID        | FK -> tenants(id), NOT NULL    |
| branch_id  | UUID        | FK -> branches(id), NOT NULL   |
| patient_id | UUID        | FK -> patients(id), NOT NULL   |
| start_at   | TIMESTAMPTZ | NOT NULL                       |
| created_at | TIMESTAMPTZ | NOT NULL DEFAULT NOW()         |

- **UNIQUE**: `uq_appointments_tenant_patient_branch_start` ON (tenant_id, patient_id, branch_id, start_at) -- C2: prevent duplicate booking
- INDEX: `ix_appointments_tenant_branch` ON (tenant_id, branch_id)

---

## 2. API Specification

### Base URL: `http://localhost:5000/api`

### Authentication

All endpoints (except login) require `Authorization: Bearer {jwt_token}` header.

JWT Claims:
- `sub`: user_id
- `tenant_id`: tenant UUID
- `role`: Admin | User | Viewer
- `branch_ids`: comma-separated branch UUIDs

---

### Auth Endpoints

#### POST /api/auth/login

Login and receive JWT token.

**Request:**
```json
{
  "username": "admin",
  "password": "password123"
}
```

**Response 200:**
```json
{
  "token": "eyJhbG...",
  "user": {
    "id": "uuid",
    "username": "admin",
    "role": "Admin",
    "tenantId": "uuid",
    "branchIds": ["uuid1", "uuid2"]
  }
}
```

**Response 401:** Invalid credentials

---

### User Endpoints (Admin only)

#### POST /api/users

Create a new user. TenantId from JWT.

**Authorization:** Admin only

**Request:**
```json
{
  "username": "newuser",
  "password": "password123",
  "role": "User",
  "branchIds": ["uuid1"]
}
```

**Response 201:**
```json
{
  "id": "uuid",
  "username": "newuser",
  "role": "User",
  "tenantId": "uuid",
  "branchIds": ["uuid1"]
}
```

#### PUT /api/users/{id}/role

Assign role to user.

**Authorization:** Admin only

**Request:**
```json
{
  "role": "Viewer"
}
```

**Response 200:** Updated user object

---

### Patient Endpoints

#### POST /api/patients

Create a new patient. TenantId derived from JWT (NOT from request body).

**Authorization:** Admin, User (Viewer CANNOT create)

**Request:**
```json
{
  "firstName": "Somchai",
  "lastName": "Jaidee",
  "phoneNumber": "0812345678",
  "primaryBranchId": "uuid-or-null"
}
```

**Response 201:**
```json
{
  "id": "uuid",
  "firstName": "Somchai",
  "lastName": "Jaidee",
  "phoneNumber": "0812345678",
  "tenantId": "uuid",
  "primaryBranchId": "uuid",
  "createdAt": "2026-02-11T10:00:00Z"
}
```

**Response 409 (Duplicate phone within tenant):**
```json
{
  "error": "DuplicatePhoneNumber",
  "message": "Phone number already exists within this tenant",
  "detail": "0812345678"
}
```

**Response 400 (Validation):**
```json
{
  "error": "ValidationFailed",
  "message": "One or more validation errors occurred",
  "errors": {
    "firstName": ["First name is required"],
    "phoneNumber": ["Phone number is required"]
  }
}
```

**Side effects:**
- Invalidate Redis cache: `tenant:{tenantId}:patients:*` (D3)

#### GET /api/patients?tenantId={required}&branchId={optional}&page=1&pageSize=20

List patients for current tenant.

**Authorization:** Admin, User, Viewer (all roles)

**Query Parameters:**
- `tenantId` (**required**): must match JWT claims, server validates and returns 403 if mismatch
- `branchId` (optional): filter by PrimaryBranchId
- `page` (default: 1)
- `pageSize` (default: 20)

**Two-layer tenant safety:**
1. Explicit tenantId parameter validated against JWT claims
2. EF Core Global Query Filter as defense-in-depth

**Response 200:**
```json
{
  "items": [
    {
      "id": "uuid",
      "firstName": "Somchai",
      "lastName": "Jaidee",
      "phoneNumber": "0812345678",
      "tenantId": "uuid",
      "primaryBranchId": "uuid",
      "createdAt": "2026-02-11T10:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

**Cache (D1/D2):**
- Key: `tenant:{tenantId}:patients:list:branch:{branchId|all}:page:{page}:size:{pageSize}`
- TTL: 5 minutes

---

### Appointment Endpoints (Section C)

#### POST /api/appointments

Create a new appointment. TenantId from JWT.

**Authorization:** Admin, User (Viewer CANNOT create)

**Request:**
```json
{
  "branchId": "uuid",
  "patientId": "uuid",
  "startAt": "2026-02-15T09:00:00Z"
}
```

**Response 201:**
```json
{
  "id": "uuid",
  "tenantId": "uuid",
  "branchId": "uuid",
  "patientId": "uuid",
  "startAt": "2026-02-15T09:00:00Z",
  "createdAt": "2026-02-11T10:00:00Z"
}
```

**Response 409 (Duplicate booking - C2):**
```json
{
  "error": "DuplicateAppointment",
  "message": "An appointment already exists for this patient at the same time and branch"
}
```

**Side effects:**
- Publish `AppointmentCreated` event to RabbitMQ (C3):
  ```json
  {
    "eventType": "AppointmentCreated",
    "tenantId": "uuid",
    "appointmentId": "uuid",
    "branchId": "uuid",
    "patientId": "uuid",
    "startAt": "2026-02-15T09:00:00Z",
    "occurredAt": "2026-02-11T10:00:00Z"
  }
  ```
- Invalidate Redis cache: `tenant:{tenantId}:appointments:*` (D3)

---

### Branch Endpoints (read-only, for dropdowns)

#### GET /api/branches

List branches for current tenant.

**Authorization:** All roles

**Response 200:**
```json
[
  { "id": "uuid", "name": "Branch Sukhumvit" },
  { "id": "uuid", "name": "Branch Silom" }
]
```

---

## 3. Screen Mapping

### Page 1: Login (`/`)

```
+------------------------------------------+
|         Clinic POS - Login               |
|                                          |
|  Username: [___________________]         |
|  Password: [___________________]         |
|                                          |
|         [  Login  ]                      |
|                                          |
|  Error message area                      |
+------------------------------------------+
```

- POST /api/auth/login
- Store JWT in localStorage/cookie
- Redirect to /patients on success

### Page 2: Patient List (`/patients`)

```
+----------------------------------------------------------+
|  Clinic POS    |  User: admin (Admin)  |  [Logout]       |
|----------------------------------------------------------+
|                                                          |
|  Patients                                                |
|                                                          |
|  Branch: [ All Branches  v ]     [+ Create Patient]     |
|                                                          |
|  +------+----------+-----------+-------------+----------+|
|  | Name | Phone    | Branch    | Created     | Actions  ||
|  +------+----------+-----------+-------------+----------+|
|  | Somchai J. | 081-234-5678 | Sukhumvit | 2026-02-11 ||
|  | Narin P.   | 089-876-5432 | Silom     | 2026-02-10 ||
|  +------+----------+-----------+-------------+----------+|
|                                                          |
|  < 1 2 3 >  (pagination)                                |
+----------------------------------------------------------+
```

**Data flow:**
- GET /api/branches → populate Branch dropdown
- GET /api/patients?branchId=&page=1 → populate table
- Branch dropdown change → re-fetch with branchId filter
- [+ Create Patient] button hidden if role === "Viewer"

### Page 3: Create Patient (Modal)

```
+------------------------------------------+
|  Create Patient                    [X]   |
|                                          |
|  First Name*: [___________________]      |
|  Last Name*:  [___________________]      |
|  Phone*:      [___________________]      |
|  Branch:      [ Select Branch  v  ]      |
|                                          |
|  [Cancel]              [Save Patient]    |
|                                          |
|  Error: Phone number already exists      |
+------------------------------------------+
```

**Data flow:**
- POST /api/patients
- On 409: show duplicate phone error
- On 400: show field validation errors
- On 201: close modal, refresh patient list

### Page 4: Appointments (`/appointments`) — Section C

```
+----------------------------------------------------------+
|  Clinic POS    |  User: admin (Admin)  |  [Logout]       |
|----------------------------------------------------------+
|                                                          |
|  Appointments                                            |
|                                                          |
|  [+ Create Appointment]                                  |
|                                                          |
|  Create Appointment Form (inline):                       |
|  Branch*:  [ Select Branch  v ]                          |
|  Patient*: [ Select Patient v ]                          |
|  Date/Time*: [___________________]                       |
|  [Create]                                                |
|                                                          |
|  Error: Duplicate appointment                            |
+----------------------------------------------------------+
```

**Data flow:**
- GET /api/branches → dropdown
- GET /api/patients → dropdown
- POST /api/appointments
- On 409: show duplicate error
- On 201: success message

---

## 4. Cache Strategy (Section D)

### D1: Cached Read Paths

| Endpoint | Cache Key Pattern | TTL |
|----------|------------------|-----|
| GET /api/patients | `tenant:{tenantId}:patients:branch:{branchId\|all}:p:{page}:s:{size}` | 5 min |
| GET /api/branches | `tenant:{tenantId}:branches` | 10 min |

### D2: Tenant-Scoped Key Pattern

All cache keys MUST be prefixed with `tenant:{tenantId}:` to ensure tenant isolation.

```
tenant:550e8400-...:patients:branch:all:p:1:s:20
tenant:550e8400-...:patients:branch:660e8400-...:p:1:s:20
tenant:550e8400-...:branches
```

### D3: Invalidation Strategy

| Event | Keys Invalidated |
|-------|-----------------|
| Create Patient | `tenant:{tenantId}:patients:*` (wildcard delete) |
| Create Appointment | `tenant:{tenantId}:appointments:*` (wildcard delete) |

Implementation: Use Redis SCAN + DEL pattern (not KEYS command in production).

---

## 5. Messaging Strategy (Section C3)

### RabbitMQ Configuration

- Exchange: `clinic-pos.events` (topic exchange)
- Routing key: `appointment.created`
- Queue: `appointment-notifications` (optional consumer)

### Event Schema: AppointmentCreated

```json
{
  "eventType": "AppointmentCreated",
  "eventId": "uuid",
  "tenantId": "uuid",
  "appointmentId": "uuid",
  "branchId": "uuid",
  "patientId": "uuid",
  "startAt": "ISO8601",
  "occurredAt": "ISO8601"
}
```

- Published after successful DB commit (at-least-once delivery)
- Consumer is optional per spec

---

## 6. Seeder Data (B4)

### Tenant
| Name | ID (generated) |
|------|----------------|
| Clinic Siam | auto-generated |

### Branches
| Name | Tenant |
|------|--------|
| Branch Sukhumvit | Clinic Siam |
| Branch Silom | Clinic Siam |

### Users
| Username | Password | Role | Branches |
|----------|----------|------|----------|
| admin | password123 | Admin | Sukhumvit, Silom |
| user1 | password123 | User | Sukhumvit |
| viewer1 | password123 | Viewer | Silom |

### Sample Patients (optional seeder)
| Name | Phone | Branch |
|------|-------|--------|
| Somchai Jaidee | 0812345678 | Sukhumvit |
| Narin Pongpat | 0898765432 | Silom |

---

## 7. Multi-Tenant Safety Checklist

| # | Requirement | Implementation |
|---|-------------|----------------|
| 1 | TenantId not from request body | Derived from JWT claims via ITenantContext |
| 2 | All queries filtered by tenant | EF Core Global Query Filter |
| 3 | Phone unique per tenant | DB UNIQUE(tenant_id, phone_number) |
| 4 | Appointment duplicate per tenant | DB UNIQUE(tenant_id, patient_id, branch_id, start_at) |
| 5 | Cache tenant-isolated | Key prefix: tenant:{id}: |
| 6 | RabbitMQ events include tenant | TenantId in event payload |
| 7 | No cross-tenant data exposure | Global filter + test verification |
| 8 | Role enforcement server-side | Authorization policies/attributes |

---

## 8. Requirements Coverage Checklist

### Section A - Core Slice
- [x] A1: REST API with validation + consistent errors
- [x] A1: PostgreSQL with migrations
- [x] A1: Tenant-safe filtering all reads/writes
- [x] A1: Next.js UI - create patient
- [x] A1: Next.js UI - list patients
- [x] A1: Next.js UI - filter by branch (optional)
- [x] A2: Patient fields (FirstName, LastName, PhoneNumber, TenantId, CreatedAt, PrimaryBranchId)
- [x] A2: PhoneNumber unique within tenant
- [x] A2: Safe error on duplicate
- [x] A3: Required filter TenantId
- [x] A3: Optional filter BranchId
- [x] A3: Sorted CreatedAt DESC

### Section B - Auth & User Management
- [x] B1: Roles - Admin, User, Viewer
- [x] B2: Permission - creating patients (Admin, User)
- [x] B2: Permission - viewing patients (all roles)
- [x] B2: Permission - creating appointments (Admin, User)
- [x] B3: API - Create User
- [x] B3: API - Assign Role
- [x] B3: API - Associate User with Tenant + Branches
- [x] B-Enforce: JWT authentication
- [x] B-Enforce: Server-side authorization
- [x] B-Enforce: Viewer cannot create patients
- [x] B4: Seeder - 1 Tenant, 2 Branches, 3 Users, associations
- [x] B4: Seeder runnable via one command

### Section C - Appointment + Messaging
- [x] C1: Create Appointment (TenantId, BranchId, PatientId, StartAt, CreatedAt)
- [x] C2: Prevent duplicate (DB unique constraint + friendly error)
- [x] C3: Publish AppointmentCreated to RabbitMQ with TenantId

### Section D - Caching
- [x] D1: Cache List Patients
- [x] D2: Tenant-scoped cache keys
- [x] D3: Invalidation on Create Patient + Create Appointment

### Mandatory Execution
- [x] docker compose up --build (PostgreSQL, Redis, RabbitMQ, backend, frontend)
- [x] Auto migrations on startup
- [x] Test: tenant scoping enforced
- [x] Test: duplicate phone prevented
- [x] Test: frontend/integration smoke test
- [x] README.md (architecture, assumptions, run, env, seeded users, curl, tests)
- [x] AI_PROMPTS.md

---

## 9. Error Response Format (Consistent)

All errors follow this structure:

```json
{
  "error": "ErrorCode",
  "message": "Human-readable message",
  "errors": {}
}
```

| HTTP Status | Error Code | When |
|-------------|-----------|------|
| 400 | ValidationFailed | Request validation fails |
| 401 | Unauthorized | No/invalid JWT token |
| 403 | Forbidden | Insufficient role/permission |
| 404 | NotFound | Resource not found |
| 409 | DuplicatePhoneNumber | Phone exists in tenant |
| 409 | DuplicateAppointment | Same booking exists |
| 500 | InternalError | Unexpected server error |

---

## 10. Test Plan

| # | Test Name | Type | Validates |
|---|-----------|------|-----------|
| 1 | TenantScopingEnforced | Backend Integration | Create patient in Tenant A, query from Tenant B returns empty |
| 2 | DuplicatePhonePrevented | Backend Integration | Same phone in same tenant returns 409 |
| 3 | SmokeTest | Frontend E2E / Integration | Create + List patient flow works |
| 4 | ViewerCannotCreatePatient | Backend Unit | Viewer role gets 403 on POST /api/patients |
| 5 | DuplicateAppointmentPrevented | Backend Integration | Same booking returns 409 |
| 6 | CacheInvalidatedOnCreate | Backend Integration | Cache cleared after patient creation |
