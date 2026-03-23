# Iteration 6 Plan: Authentication

## Current State (End of Iteration 5)

- `QuestionType`: `SingleChoice = 0`, `MultipleChoice = 1`, `Ordering = 2`, `BuildList = 3`, `Matching = 4`. ✅
- All question admin flows (Create, Edit, Delete, List) and exam session are fully functional. ✅
- No authentication or authorization — all pages are publicly accessible. ❌
- 37 tests (30 unit + 7 functional), all passing. ✅
- Staging deploy complete. ✅

## Goals

1. Wire up ASP.NET Core Identity (users, roles, password hashing, cookie auth).
2. Seed a dev admin account automatically; assign admin role in prod/staging via SQL script.
3. Protect admin pages (Admin role required) and exam pages (any authenticated user).
4. Add login/logout/register links to the nav; remove placeholder Counter and Weather pages.
5. Cover the new access-control behavior with functional tests.

## Key Decisions

### Access Control Model

| Page area | Required |
|---|---|
| Landing page (`/`) | Anonymous |
| Register / Login | Anonymous |
| Exam session (`/exams/...`) | Authenticated (any role) |
| Questions admin (`/questions/...`) | Authenticated + Admin role |
| ExamProfiles admin (`/exam-profiles/...`) | Authenticated + Admin role |

### Identity Setup

- **Package**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (already available in .NET 10).
- **User class**: `ApplicationUser : IdentityUser` — empty class now; avoids a future migration when custom properties are added.
- **DbContext**: `ExamSimulatorDbContext` extends `IdentityDbContext<ApplicationUser>`.
- **New EF migration**: `AddIdentity` — adds all ASP.NET Identity tables.
- **Cookie auth**: configured via `AddDefaultIdentity` / `AddRoles<IdentityRole>` in `Program.cs`.

### Login / Register UI

- Use built-in Identity Razor Pages (`/Identity/Account/Login`, `/Identity/Account/Register`).
- Scaffold minimally: only the pages needed (Login, Register, Logout).
- No custom Blazor auth UI this iteration.

### Dev Admin Seeding

- `DbSeeder.cs` seeds the `Admin` role and an `admin@examsimulator.local` account **only when `IsDevelopment()`**.
- Password read from `appsettings.Development.json` under key `Seeding:AdminPassword`.
- No seeding code runs in staging or production.

### Prod / Staging Admin Role Assignment

- User self-registers through the normal Register page.
- An admin then runs the following idempotent SQL script once to promote the account:

```sql
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u
CROSS JOIN AspNetRoles r
WHERE u.Email = 'your@email.com'
  AND r.Name = 'Admin'
  AND NOT EXISTS (
      SELECT 1 FROM AspNetUserRoles ur
      WHERE ur.UserId = u.Id AND ur.RoleId = r.Id
  );
```

### Registration

- Open registration — anyone can create a learner account.
- No email confirmation this iteration.

### Removed Pages

- `Counter.razor` and `Weather.razor` removed (placeholder scaffolding, not needed).
- Corresponding nav links removed.

## Target State (End of Iteration 6)

```
src/
  ExamSimulator.Web/
    Domain/
      Identity/
        ApplicationUser.cs         ← empty class extending IdentityUser
    Infrastructure/
      ExamSimulatorDbContext.cs    ← extends IdentityDbContext<ApplicationUser>
      DbSeeder.cs                  ← seeds Admin role + admin account in Development
      Migrations/
        ..._AddIdentity.cs         ← new migration
    Components/
      Pages/
        Counter.razor              ← removed
        Weather.razor              ← removed
      Layout/
        NavMenu.razor              ← admin links wrapped in AuthorizeView; login/logout links; no Counter/Weather
        App.razor                  ← AuthorizeRouteView + CascadingAuthenticationState
      Features/
        Exams/
          ExamSession.razor        ← @attribute [Authorize]
        Questions/
          *.razor                  ← @attribute [Authorize(Roles = "Admin")]
        ExamProfiles/
          *.razor                  ← @attribute [Authorize(Roles = "Admin")]
    Areas/
      Identity/
        Pages/
          Account/
            Login.cshtml           ← scaffolded Identity Razor Page
            Register.cshtml        ← scaffolded Identity Razor Page
            Logout.cshtml          ← scaffolded Identity Razor Page
    Program.cs                     ← AddIdentity, MapRazorPages
    appsettings.Development.json   ← Seeding:AdminPassword
tests/
  ExamSimulator.Web.FunctionalTests/
    QuestionAdminTests.cs          ← updated: admin endpoints redirect unauthenticated
    AuthorizationTests.cs          ← new: exam + admin access control scenarios
```

## Phases

---

### Phase 1: Identity Wiring

**Closes:** #47

**Changes:**
- Add NuGet package `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.
- Create `ApplicationUser : IdentityUser` in `Domain/Identity/`.
- Change `ExamSimulatorDbContext` base class to `IdentityDbContext<ApplicationUser>`.
- In `Program.cs`:
  - Replace `AddDbContext<ExamSimulatorDbContext>` call (unchanged options, just ensuring order).
  - Add `builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => { options.SignIn.RequireConfirmedAccount = false; }).AddEntityFrameworkStores<ExamSimulatorDbContext>().AddDefaultTokenProviders();`
  - Add `app.MapRazorPages();`
- Add EF migration `AddIdentity`.
- Update `App.razor` to use `<CascadingAuthenticationState>` and `<AuthorizeRouteView>`.

**Definition of Done:** App builds and runs; Identity tables created by migration; `/Identity/Account/Login` is reachable.

---

### Phase 2: Dev Admin Seeding

**Closes:** #51

**Changes:**
- Add `"Seeding": { "AdminPassword": "Dev@dmin1!" }` to `appsettings.Development.json`.
- In `DbSeeder.cs`, inject `UserManager<ApplicationUser>`, `RoleManager<IdentityRole>`, `IWebHostEnvironment`.
- When `IsDevelopment()`:
  - Ensure role `Admin` exists via `roleManager.CreateAsync`.
  - Ensure user `admin@examsimulator.local` exists via `userManager.CreateAsync`.
  - Ensure user is in `Admin` role via `userManager.AddToRoleAsync`.
- Idempotent (check before create).

**Definition of Done:** Running the app in Development auto-creates the admin account; re-running is safe.

---

### Phase 3: Protect Routes

**Closes:** #48

**Changes:**
- Add `@attribute [Authorize]` to all exam session Razor components.
- Add `@attribute [Authorize(Roles = "Admin")]` to all Questions and ExamProfiles Razor components.
- Unauthenticated users hitting a protected route are redirected to `/Identity/Account/Login`.

**Definition of Done:** Navigating to `/questions` while logged out redirects to login. Navigating to `/exams/...` while logged out redirects to login. Logged-in non-Admin user can reach `/exams/...` but is redirected away from `/questions`.

---

### Phase 4: Nav and UI Cleanup

**Closes:** #50

**Changes:**
- Remove `Counter.razor` and `Weather.razor`.
- Update `NavMenu.razor`:
  - Remove Counter and Weather links.
  - Wrap Questions + ExamProfiles links in `<AuthorizeView Roles="Admin">`.
  - Wrap Exams link in `<AuthorizeView>` (any authenticated user).
  - Add login link (visible when not authenticated): `<a href="/Identity/Account/Login">Log in</a>`.
  - Add logout form (visible when authenticated): posts to `/Identity/Account/Logout`.
  - Add register link (visible when not authenticated): `<a href="/Identity/Account/Register">Register</a>`.

**Definition of Done:** Nav correctly shows/hides links based on auth state. Login/logout works end-to-end.

---

### Phase 5: Functional Tests

**Closes:** #49

**Changes:**
- Update existing `QuestionAdminTests` HTTP tests:
  - `ListQuestions_ReturnsSuccessStatusCode` now expects `302` redirect (not `200`), since the route is protected.
- Add new `AuthorizationTests.cs`:
  - `AdminPages_WhenUnauthenticated_RedirectToLogin` — GET `/questions` returns `302` to login.
  - `ExamPages_WhenUnauthenticated_RedirectToLogin` — GET `/exams` (or any exam route) returns `302` to login.
  - `LearnerPages_WhenAuthenticated_ReturnOk` — authenticated non-Admin user can access exam pages.
  - `AdminPages_WhenAuthenticatedAsLearner_Forbidden` — authenticated non-Admin user cannot access `/questions` (redirected or 403).

**Definition of Done:** All tests pass. Access-control matrix is fully covered.
