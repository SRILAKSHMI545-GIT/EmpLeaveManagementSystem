# Employee Leave Management System

A full-stack web application for managing employee leave requests, balances, and approvals.

**Backend:** ASP.NET Core 8 Web API &nbsp;|&nbsp; **Frontend:** Angular 18 SPA &nbsp;|&nbsp; **Auth:** JWT + Refresh Tokens &nbsp;|&nbsp; **DB:** SQL Server / In-Memory

---

## Table of Contents

- [Project Overview](#project-overview)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Run the Backend](#run-the-backend)
  - [Run the Frontend](#run-the-frontend)
- [Default Test Accounts](#default-test-accounts)
- [How to Use](#how-to-use)
- [API Endpoints](#api-endpoints)
- [Project Structure](#project-structure)
- [Key Classes](#key-classes)
- [Leave Types & Quotas](#leave-types--quotas)
- [Environment Configuration](#environment-configuration)
- [Business Rules](#business-rules)

---

## Project Overview

The Employee Leave Management System lets employees apply for leave and allows managers/admins to approve or reject those requests. It tracks per-employee, per-type, per-year leave balances and automatically deducts days when leave is approved.

| Role         | Capabilities |
|--------------|-------------|
| **Employee** | View own balances, submit leave requests, cancel own requests |
| **Manager**  | All employee actions + review/approve/reject team requests, view team calendar & balances |
| **Admin**    | Full access — all requests, all users, all balances system-wide |

---

## Architecture

```
┌─────────────────┐      HTTP/REST      ┌──────────────────────────────────────────┐
│  Angular 18 SPA │ ──────────────────▶ │           ASP.NET Core 8 API             │
│  (port 4200)    │ ◀────────────────── │           (port 5000)                    │
└─────────────────┘      JSON + JWT     └──────────┬───────────────────────────────┘
                                                   │
                                    ┌──────────────▼──────────────┐
                                    │     LeaveManagement.Core    │
                                    │  Entities · DTOs · Interfaces│
                                    │  Validators · Enums          │
                                    └──────────────┬──────────────┘
                                                   │
                                    ┌──────────────▼──────────────┐
                                    │ LeaveManagement.Infrastructure│
                                    │  AppDbContext · Services      │
                                    │  Repositories · DbInitializer │
                                    └──────────────┬──────────────┘
                                                   │
                                    ┌──────────────▼──────────────┐
                                    │   SQL Server / In-Memory DB  │
                                    └─────────────────────────────┘
```

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ | Run the backend API |
| [Node.js](https://nodejs.org/) | 18.x+ | Run the Angular frontend |
| npm | 9.x+ | Install frontend packages |
| SQL Server *(optional)* | Any | Persistent DB — not needed for quick start |

---

## Getting Started

> **Tip:** The backend supports an **in-memory database** — no SQL Server installation needed for a quick demo run.

### Run the Backend

**Option A — In-Memory Database (recommended for quick start)**

```bash
cd backend/LeaveManagement.Api
dotnet run --UseInMemoryDatabase=true --urls "http://localhost:5000"
```

**Option B — SQL Server**

1. Update the connection string in `backend/LeaveManagement.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=LeaveManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

2. Run the API:

```bash
cd backend/LeaveManagement.Api
dotnet run --urls "http://localhost:5000"
```

The database schema is created automatically on first run. Seed data (users, departments, leave types, balances) is inserted if the tables are empty.

**Verify the backend is running:**  
Open `http://localhost:5000/swagger` — you should see the interactive Swagger UI.

---

### Run the Frontend

```bash
cd frontend/leave-management-ui

# First time only — install dependencies
npm install

# Start the dev server
npm start
```

The Angular app runs at **http://localhost:4200** and communicates with the backend at `http://localhost:5000`.

> **Note:** Start the backend first, then the frontend.

---

## Default Test Accounts

All accounts use the same password: **`Password123!`**

| Name | Email | Role |
|------|-------|------|
| Admin User | admin@company.com | Admin |
| Sarah Connor | manager@company.com | Manager |
| John Doe | employee@company.com | Employee |
| Alice Smith | alice@company.com | Employee |
| Bob Johnson | bob@company.com | Employee |

John Doe, Alice Smith, and Bob Johnson all report to **Sarah Connor** (Engineering department).  
Sample leave requests (approved + pending) are pre-seeded so dashboards are populated immediately.

---

## How to Use

### As an Employee
1. Log in at `http://localhost:4200`
2. View your **leave balances** on the dashboard
3. **Submit a leave request** — choose type, dates, and reason
4. **Track** your requests (Pending / Approved / Rejected / Cancelled)
5. **Cancel** a pending or approved request at any time

### As a Manager
1. Log in with `manager@company.com`
2. View **team leave requests** (pending and historical)
3. **Approve or Reject** pending requests with an optional comment
4. View **team balances** and the **team calendar** for upcoming leaves

---

## API Endpoints

All endpoints (except `register` and `login`) require a `Bearer` JWT token in the `Authorization` header.  
Full interactive docs: **http://localhost:5000/swagger**

### Authentication — `/api/auth`

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/auth/register` | Public | Register a new user |
| POST | `/api/auth/login` | Public | Login — returns JWT + refresh token |
| POST | `/api/auth/refresh-token` | Public | Exchange refresh token for new JWT |
| POST | `/api/auth/revoke-token` | Authenticated | Logout / invalidate refresh token |
| GET | `/api/auth/profile` | Authenticated | Get current user profile |
| GET | `/api/auth/team-members` | Manager / Admin | Get direct reports |
| GET | `/api/auth/users` | Admin only | Get all users |

### Leave Requests — `/api/leave-requests`

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/leave-requests/my` | Authenticated | My leave requests |
| POST | `/api/leave-requests` | Authenticated | Submit a new leave request |
| PUT | `/api/leave-requests/{id}/cancel` | Authenticated | Cancel own request |
| GET | `/api/leave-requests/team` | Manager / Admin | Team requests (filter by `?status=`) |
| PUT | `/api/leave-requests/{id}/approve` | Manager / Admin | Approve a pending request |
| PUT | `/api/leave-requests/{id}/reject` | Manager / Admin | Reject a pending request |
| GET | `/api/leave-requests/team/calendar` | Manager / Admin | Team calendar view |
| GET | `/api/leave-requests/all` | Admin only | All requests in the system |

### Leave Balances — `/api/leavebalances`

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/leavebalances/my?year=2025` | Authenticated | My balances for a year |
| GET | `/api/leavebalances/team?year=2025` | Manager / Admin | Team balances |
| GET | `/api/leavebalances/all?year=2025` | Admin only | All employee balances |

---

## Project Structure

```
Employee-Leave-Management-System/
├── backend/
│   ├── LeaveManagement.Api/                  ← ASP.NET Core Web API (entry point)
│   │   ├── Controllers/                      ← HTTP endpoints (Auth, LeaveRequests, LeaveBalances, Metadata)
│   │   ├── Middleware/                       ← Global exception handler
│   │   ├── Program.cs                        ← App bootstrap, DI registration, JWT config
│   │   └── appsettings.json                  ← Connection string, JWT settings
│   ├── LeaveManagement.Core/                 ← Business rules & contracts (no dependencies)
│   │   ├── Entities/                         ← User, LeaveRequest, LeaveBalance, LeaveType, Department
│   │   ├── Interfaces/                       ← IAuthService, ILeaveService, IRepository<T>
│   │   ├── DTOs/                             ← Request/response data transfer objects
│   │   ├── Enums/                            ← UserRole, LeaveRequestStatus
│   │   ├── Validators/                       ← FluentValidation rules
│   │   └── Exceptions/                       ← NotFoundException, ForbiddenException, etc.
│   └── LeaveManagement.Infrastructure/       ← Data access & service implementations
│       ├── Data/
│       │   ├── AppDbContext.cs               ← EF Core database context
│       │   └── DbInitializer.cs              ← Seed data on startup
│       ├── Repositories/
│       │   └── Repository.cs                 ← Generic repository pattern
│       └── Services/
│           ├── AuthService.cs                ← Registration, login, JWT + refresh token logic
│           ├── LeaveService.cs               ← All leave business logic
│           └── JwtTokenGenerator.cs          ← JWT token creation
├── frontend/
│   └── leave-management-ui/                  ← Angular 18 SPA
│       └── src/app/
│           ├── components/
│           │   ├── login/                    ← Login form
│           │   ├── employee-dashboard/       ← Employee view
│           │   ├── manager-dashboard/        ← Manager view
│           │   └── navbar/                   ← Navigation bar
│           └── core/
│               ├── services/                 ← AuthService, LeaveService (Angular)
│               ├── guards/                   ← authGuard, managerGuard
│               ├── interceptors/             ← Attaches JWT to every HTTP request
│               └── models/                   ← TypeScript interfaces
├── Commands.txt                              ← Quick reference run commands
└── README.md
```

---

## Key Classes

### Entities (`LeaveManagement.Core/Entities/`)

| Class | Description |
|-------|-------------|
| `User` | Any person in the system. Self-referencing hierarchy via `ManagerId` / `DirectReports`. Has `Role`, `DepartmentId`, `IsActive`. |
| `LeaveRequest` | One leave application. Tracks `StartDate`, `EndDate`, `DaysRequested`, `Status`, `ManagerComment`, and who decided it. |
| `LeaveBalance` | Per-user, per-type, per-year balance record. `RemainingDays = TotalDays - UsedDays`. Auto-provisioned when first accessed. |
| `LeaveType` | Master data for each leave type with `DefaultAnnualQuota`. |
| `Department` | Organisational department (Engineering, HR, Sales, Finance). |
| `RefreshToken` | Stores hashed refresh tokens with expiry and revocation state. |

### Services (`LeaveManagement.Infrastructure/Services/`)

| Class | Responsibility |
|-------|---------------|
| `LeaveService` | **Core business logic.** Creates requests (overlap check, balance check, working day calculation), approves (deducts balance), rejects, cancels (restores balance if previously approved), team calendar. |
| `AuthService` | Registration (BCrypt hashing), login, JWT issuance, refresh token rotation, profile retrieval. |
| `JwtTokenGenerator` | Generates signed JWT access tokens (60 min) and random refresh tokens (7 days). |

### Infrastructure (`LeaveManagement.Infrastructure/Data/`)

| Class | Responsibility |
|-------|---------------|
| `AppDbContext` | EF Core context — defines all DbSet tables, supports SQL Server and In-Memory. |
| `DbInitializer` | Seeds departments, leave types, users, balances, and sample requests on startup (runs only when tables are empty). |

### Angular (`frontend/src/app/core/`)

| File | Responsibility |
|------|---------------|
| `auth.interceptor.ts` | Automatically attaches `Authorization: Bearer <token>` header to all outgoing HTTP requests. |
| `auth.guard.ts` | `authGuard` — redirects unauthenticated users to `/login`. `managerGuard` — blocks non-managers from `/manager`. |
| `auth.service.ts` | Login, logout, token storage, user role helpers. |
| `leave.service.ts` | Angular wrapper for all leave API calls (requests, balances, types). |

---

## Leave Types & Quotas

| Leave Type | Annual Days | Notes |
|------------|------------|-------|
| Annual Leave | 20 | Standard paid time off |
| Sick Leave | 10 | Medical illness and recovery |
| Casual Leave | 7 | Short personal or urgent leave |
| Maternity / Paternity | 60 | Parental leave |
| Unpaid Leave | 30 | Leave without pay |

---

## Environment Configuration

**`backend/LeaveManagement.Api/appsettings.json`**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\ProjectModels;Database=LeaveManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "SecretKey": "YourSecretKey...",
    "Issuer": "LeaveManagementApi",
    "Audience": "LeaveManagementClient",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "UseInMemoryDatabase": false
}
```

To switch to in-memory DB without editing the file, pass the flag at runtime:

```bash
dotnet run --UseInMemoryDatabase=true
```

---

## Business Rules

- **Overlapping requests** (Pending or Approved) are not allowed for the same employee.
- **Weekend-only date ranges** are rejected — only working days (Mon–Fri) count.
- You **cannot request more days** than your remaining balance for that leave type and year.
- Only the employee's **direct manager** (or an Admin) can approve or reject their requests.
- **Cancelling an approved request** automatically restores the leave balance.
- Leave balances for a new year are **auto-provisioned** on first access — no manual setup needed.
- JWT access tokens expire in **60 minutes**; refresh tokens last **7 days**.
