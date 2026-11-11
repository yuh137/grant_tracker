# GrantTracker

A small Grant Task Tracker for grants officers. ASP.NET Core Web API +
EF Core (SQLite) + React/TypeScript. Assessed on judgement, correctness, tests and how the AI
was used, not on volume. A small, correct, well-tested change beats a large one.

## Layout

Repository root holds the solution folder `GrantTracker/`, the front end `GrantTrackerApp/`,
`test.sh`, README.md and TESTING.md.

| Project | Owns | Depends on |
|---|---|---|
| `GrantTracker.Domain` | `Entities/` (with the business rules), `Enums/`, exceptions at the root | nothing |
| `GrantTracker.Data` | `GrantTrackerDbContext`, EF configuration, seed data | Domain, EF Core |
| `GrantTracker.Api` | Controllers, DTOs (`DTO/`), orchestration services, ProblemDetails mapping, health checks (`Health/`) | Domain, Data |
| `GrantTracker.Tests` | xUnit: `Domain/` unit tests, `Api/` integration tests on in-memory SQLite + `FakeTimeProvider` | all |
| `GrantTrackerApp/` | Vite + React + TypeScript single screen, Vitest. Proxies `/grants`, `/tasks` and `/reports` to the API | API over HTTP |

## Rules that must hold

- Business rules live in `Grant` and `GrantTask` methods, nowhere else.
  Services load, call the domain method, save, map. Controllers only route.
- Never call `DateTime.Now`, `DateTime.Today`, `DateTimeOffset.Now` or `TimeProvider.System`
  in Domain, Data or services. Time comes from the injected `TimeProvider`, whose local zone
  is the office's (`OfficeTimeZone`). Domain methods take `today` or `now` as a parameter.
- Dates from people (application, due, completed) are `DateOnly`. Timestamps from the system
  (`CreatedOn`) are `DateTimeOffset`. Never `DateTime` on entities.
- A rule violation throws `BusinessRuleException(BusinessRuleErrorCode, message)`. Add new codes to the
  `BusinessRuleErrorCode` enum, never as strings. `ApiExceptionHandler` maps it to 422 ProblemDetails with a
  `code` field holding the enum's name. The name is part of the API contract: renaming a value breaks clients.
  Unknown id: `NotFoundException` -> 404. `DbUpdateConcurrencyException` -> 409.
  Shape errors are 400 from model validation. Do not add other error shapes.
- Any method that changes a grant or its task list bumps `Grant.Version`. That is the
  concurrency guard for adding a task and closing a grant.
- Completing and closing are idempotent: repeating returns 200 with unchanged state.
- Every rule and every bug fix gets a test. Tests pin the date with `FakeTimeProvider` and must
  not depend on the real date or on test order.
- Seed data is the spec's section 6, dates relative to the office's today, ids fixed in
  `DatabaseSeeder`. Seeds bypass domain rules on purpose (they contain overdue tasks).

## Conventions

- British spelling in domain names (`OrganisationName`), matching the spec.
- `GrantTask`, not `Task`, to avoid clashing with `System.Threading.Tasks.Task`.
- Enums stored as strings. Keep it that way, because the support team reads rows directly.
- No new NuGet packages without saying why. `nuget.config` pins restore to nuget.org.
- Front end: plain `fetch` in `src/api.ts`, no data library. `ConnectionError` means the API could not be
  reached (rejected fetch, or a 5xx without a ProblemDetails body from a proxy). `ApiError` carries ProblemDetails.

## Commands

```
dotnet build GrantTracker/GrantTracker.sln                 # whole solution
dotnet run --project GrantTracker/GrantTracker.Api         # http://localhost:5180, recreates + seeds the DB
dotnet test GrantTracker/GrantTracker.sln                  # back-end tests
npm --prefix GrantTrackerApp run dev                       # http://localhost:5173, proxies to the API
npm --prefix GrantTrackerApp test                          # front-end tests
./test.sh                                                  # every test, one command (Git Bash on Windows)
```

Health: `GET /health/live` (process only) and `GET /health/ready` (queries the grants table, 503 when it fails).
Swagger UI: `/swagger` in Development, over the OpenAPI document at `GET /openapi/v1.json`.
Enums are text in both JSON settings (`AddJsonOptions` and `ConfigureHttpJsonOptions`). The OpenAPI document reads the second. Requests to try: `GrantTracker/GrantTracker.Api/GrantTracker.Api.http`.

## Working with the AI

- Plan before building. State assumptions in the README, not in code comments.
- Verify output by running it. "It compiles" is not "it works".
- When a test is written with AI help, break the code on purpose once and watch it fail.
- Record what the AI got wrong in `AI_USAGE.md` as it happens, not at the end.
