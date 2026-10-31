# Student Grade Management System - Final Project

Two ASP.NET Core (net10.0) projects against one SQL Server database:

- **StudentGradeManagement.API** - Web API (JWT auth, ASP.NET Core Identity, EF Core, SQL Server). Runs on `https://localhost:7001`.
- **StudentGradeManagement.MVC** - MVC frontend (login + Faculty/Student dashboards, JavaScript Fetch API). Runs on `https://localhost:7298`.

---

## 1. How to run

1. Make sure SQL Server is running and the connection string in
   `StudentGradeManagement.API/appsettings.json` points to your instance
   (default: `Server=localhost;Database=StudentGradeManagementDB;Trusted_Connection=True;...`).
2. Trust the dev HTTPS certificate once (so the browser and the MVC->API call work):
   ```
   dotnet dev-certs https --trust
   ```
3. In Visual Studio, set **both** projects to start (the solution is already
   configured for multi-project startup), or run each manually:
   ```
   dotnet run --project StudentGradeManagement.API
   dotnet run --project StudentGradeManagement.MVC
   ```
4. On first API start, the app automatically:
   - applies EF Core migrations (`Database.Migrate()`),
   - seeds roles, login accounts, matching Student/Faculty domain rows, and a
     few sample grades (only if the grade table is empty).
   Existing data is **never** overwritten or deleted.
5. Open the MVC site (`https://localhost:7298`) and log in.
6. API docs/testing: Swagger UI is available at `https://localhost:7001/swagger`.

> If your API runs on a different port, update:
> - `StudentGradeManagement.MVC/appsettings.json` -> `ApiBaseUrl`
> - the `API_URL` constant at the top of `Views/Faculty/Index.cshtml` and `Views/Student/Index.cshtml`
> - `{{baseUrl}}` in the Postman collection.

## 2. Login accounts (password for all: `Password123!`)

| Role    | Username     | Name           |
|---------|--------------|----------------|
| Faculty | `prof_smith` | Andrea Garcia  |
| Faculty | `FAC-002`    | Michael Ramos  |
| Student | `2026-0001`  | Juan Dela Cruz |
| Student | `2026-0002`  | Maria Santos   |
| Student | `2026-0003`  | Pedro Reyes    |
| Student | `202310123`  | Juan Dela Cruz |

(Any accounts that already existed keep their original passwords.)

## 3. Grading scale (computed server-side only)

| Grade  | Remarks     |
|--------|-------------|
| 95-100 | CMS         |
| 90-94  | SP          |
| 85-89  | SAP         |
| 80-84  | GP          |
| 75-79  | EP          |
| 0-74   | BEP/FAILED  |

Remarks sent by the client are ignored; the API recalculates them on every
create/update. Grades must be **whole numbers 0-100** - null, missing,
decimal, and out-of-range values are rejected (400).

## 4. API endpoints

| Method | Route                   | Role    | Success | Notes |
|--------|-------------------------|---------|---------|-------|
| POST   | /api/auth/login         | public  | 200     | returns JWT + role + name |
| GET    | /api/grades             | Faculty | 200     | optional ?studentNumber=&subject=&semester=&schoolYear=&remarks= |
| GET    | /api/grades/{id}        | any     | 200/404 | student only own, else 404 |
| POST   | /api/grades             | Faculty | 201     | validates grade, computes remarks |
| PUT    | /api/grades/{id}        | Faculty | 200     | recomputes remarks |
| DELETE | /api/grades/{id}        | Faculty | 204     | |
| GET    | /api/student/grades     | Student | 200     | StudentId from token only |

Status codes: 200 OK, 201 Created, 204 No Content, 400 Bad Request,
401 Unauthenticated, 403 Wrong role, 404 Not found / concealed, 409 Duplicate.

## 5. Security / data ownership

- Authorization is enforced on the **API** with `[Authorize]` and
  `[Authorize(Roles = "...")]`, not just by hiding buttons.
- A student's identity is taken **only** from the JWT (`StudentNumber` claim).
  Query strings, route ids, and request bodies cannot be used to read another
  student's data. `GET /api/grades/{id}` returns **404** when a student asks
  for a record that is not theirs (existence is concealed).
- Passwords are hashed by ASP.NET Core Identity. No plaintext, no hardcoded
  credentials in the auth path.

## 6. Postman

Import `docs/StudentGradeManagement.postman_collection.json` and run the
folder top-to-bottom (Collection Runner works too). It logs in, captures the
tokens automatically, and asserts the status codes for every faculty and
student test, including: grade 105, grade -1, decimal grade, fake remarks,
cross-student access, student write attempts, and unauthenticated access.

## 7. Verification checklist (run in Visual Studio)

- [ ] Solution builds with no errors.
- [ ] Faculty login redirects to the Faculty dashboard; name/ID shown correctly.
- [ ] Student login redirects to the Student dashboard; real name + student number shown.
- [ ] Faculty: add / edit / delete grade works; table refreshes; remarks correct.
- [ ] Faculty: search + subject/semester/school-year/remarks filters work.
- [ ] Add grade 105 / -1 / 85.5 -> rejected with a message.
- [ ] Fake remarks on POST -> stored remark matches the grade, not the fake value.
- [ ] Student dashboard loads grades via Fetch API (loading + empty states show).
- [ ] Student has no add/edit/delete controls.
- [ ] Run the Postman collection -> all tests green.
- [ ] Restart the app -> data persists (grades remain).
- [ ] Browser console has no errors.
