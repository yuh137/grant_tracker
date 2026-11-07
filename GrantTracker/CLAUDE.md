# GrantTracker

Take-home exercise: a small Grant Task Tracker for grants officers. ASP.NET Core Web API +
EF Core (SQLite) + React/TypeScript. Assessed on judgement, correctness, tests and how the AI
was used, not on volume. A small, correct, well-tested change beats a large one.

## Layout

| Project | Owns | Depends on |
|---|---|---|
| `GrantTracker.Domain` | `Entities/` (with the business rules), `Enums/`, exceptions at the root | nothing |
| `GrantTracker.Data` | `GrantTrackerDbContext`, EF configuration, seed data | Domain, EF Core |
| `GrantTracker.Api` | Controllers, DTOs (`Contracts/`), orchestration services, ProblemDetails mapping | Domain, Data |
| `GrantTracker.Tests` | xUnit: domain unit tests + API integration tests | all |
| `client/` | Vite + React + TypeScript single screen | API over HTTP |

## Rules that must hold

- Business rules BR-1 to BR-6 live in `Grant` and `GrantTask` methods, nowhere else.
  Services load, call the domain method, save, map. Controllers only route.
- Never call `DateTime.Now`, `DateTime.Today`, `DateTimeOffset.Now` or `TimeProvider.System`
  in Domain, Data or services. Time comes from the injected `TimeProvider`, whose local zone
  is the office's (`Office:TimeZone`). Domain methods take `today` or `now` as a parameter.
- Dates from people (application, due, completed) are `DateOnly`. Timestamps from the system
  (`CreatedOn`) are `DateTimeOffset`. Never `DateTime` on entities.
- A rule violation throws `BusinessRuleException(code, message)` with the spec's code
  (`"BR-1"` ...). `ApiExceptionHandler` maps it to 422 ProblemDetails with a `code` field.
  Unknown id: `NotFoundException` -> 404. `DbUpdateConcurrencyException` -> 409.
  Shape errors are 400 from model validation. Do not add other error shapes.
- Any method that changes a grant or its task list bumps `Grant.Version`. That is the
  concurrency guard for BR-1 and BR-5.
- Completing and closing are idempotent: repeating returns 200 with unchanged state.
- Every rule and every bug fix gets a test. Tests pin the date with `FakeTimeProvider` and must
  not depend on the real date or on test order.
- Seed data is the spec's section 6, dates relative to the office's today, ids fixed in
  `DatabaseSeeder`. Seeds bypass domain rules on purpose (they contain overdue tasks).

## Conventions

- No code comments. Names and structure carry the meaning; explanations go in README or AI_USAGE.md.

- British spelling in domain names (`OrganisationName`), matching the spec.
- `GrantTask`, not `Task`, to avoid clashing with `System.Threading.Tasks.Task`.
- Enums stored as strings. Keep it that way; the support team reads rows directly.
- No new NuGet packages without saying why. `nuget.config` pins restore to nuget.org.
- Comments explain a decision or a rule reference, not what the code does.

## Commands

```
dotnet build                                   # whole solution
dotnet run --project GrantTracker.Api          # http://localhost:5180, recreates + seeds the DB
dotnet test                                    # all tests, one command
```

OpenAPI document: `GET /openapi/v1.json` in Development. Requests to try: `GrantTracker.Api.http`.

## Working with the AI

- Plan before building. State assumptions in the README, not in code comments.
- Verify output by running it. "It compiles" is not "it works".
- When a test is written with AI help, break the code on purpose once and watch it fail.
- Record what the AI got wrong in `AI_USAGE.md` as it happens, not at the end.
