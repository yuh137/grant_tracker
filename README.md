# Grant Task Tracker

A small tool for grants officers: see grants, add tasks, complete tasks and spot overdue ones.

- **Back end:** ASP.NET Core Web API (.NET 10), Entity Framework Core, SQLite
- **Front end:** React and TypeScript, built with Vite
- **Tests:** xUnit for the back end, Vitest for the front end

## How to run

You need the .NET 10 SDK and Node.js 20 or later.

### 1. Start the API

```
cd GrantTracker
dotnet run --project GrantTracker.Api
```

The API listens on http://localhost:5180. Each time it starts in Development it recreates the
database and loads the seed data. Due dates are set relative to today, so there are always overdue
tasks to look at.

- Requests to try: `GrantTracker/GrantTracker.Api/GrantTracker.Api.http`
- OpenAPI document: http://localhost:5180/openapi/v1.json
- Health: http://localhost:5180/health/live and http://localhost:5180/health/ready
- Daily activity report: http://localhost:5180/reports/task-activity?days=14

### 2. Start the front end

In a second terminal, from this folder:

```
npm --prefix GrantTrackerApp install
npm --prefix GrantTrackerApp run dev
```

Open http://localhost:5173. The front end forwards its API calls to port 5180, so start the API
first.

### 3. Run the tests

From this folder:

```
./test.sh         # macOS / Linux, or Git Bash on Windows
```

This runs 50 back-end tests and 16 front-end tests. The testing strategy is in
[TESTING.md](TESTING.md).

## How the code is organised

| Project | What it holds |
|---|---|
| `GrantTracker.Domain` | The `Grant` and `GrantTask` classes, including the business rules. No database code. |
| `GrantTracker.Data` | The EF Core database context, table configuration and seed data. |
| `GrantTracker.Api` | Controllers, request and response classes (`DTO/`), services, error mapping and health checks. |
| `GrantTracker.Tests` | Domain unit tests and API integration tests. |
| `GrantTrackerApp` | The React front end. |

Each business rule is a method on `Grant` or `GrantTask`, for example `grant.Close()`. That keeps
a rule in one place and lets it be tested without a web server or a database. Services load, call
the rule, save and map the result. Controllers only route.

## Design decisions and trade-offs

**Data model.** One table for grants and one for tasks. A grant has many tasks.

- A task stores `CompletedOn`, a date, instead of a yes/no flag. BR-4 needs the date, and
  "completed" means the date is filled in.
- "Overdue" is not stored. It is calculated from the due date and today.
- `Version` on the grant guards against two officers changing the same grant at once (OP-2).
- Dates that people choose are `DateOnly`. The moment a task was created is a `DateTimeOffset`.
- Status and priority are saved as text, so the database is easy to read.

**Endpoints.** These follow the brief. Completing and closing use `PUT`, because doing them twice
has the same result as doing them once.

**Errors.** Every error uses the standard ProblemDetails format:

| Status | When |
|---|---|
| 400 | The request is badly formed, for example a missing or unknown priority. |
| 404 | The grant or task does not exist. |
| 409 | Another officer changed the grant at the same moment. Reload and try again. |
| 422 | A business rule was broken. The body has a `code` such as `GrantClosed` or `InvalidDueDate`. |

**Completing a task that is already completed (BR-4).** The API returns 200 with the task
unchanged, keeping the original completion date. If the first response is lost and the officer
clicks again, the second click should look like success (OP-3). Closing a closed grant works the
same way.

**"Today".** Business logic never reads the system clock. Time comes from .NET's `TimeProvider`,
which tests replace with a fixed date. "Today" is calculated in the office's time zone
(`OfficeTimeZone` in `appsettings.json`, currently `Australia/Sydney`), so a server running in UTC
does not move the date.

## Assumptions

- A task due today is allowed, and is not overdue until tomorrow.
- A due date equal to the grant's application date is allowed.
- Titles are trimmed before checking. A title of only spaces counts as missing.
- All offices use one time zone for "today".
- Tasks on a closed grant can still be completed. The brief only blocks adding tasks.
- There is no login. The exercise does not ask for one.

## Operations needs

I built OP-2, OP-3, OP-5 and OP-6. OP-2 and OP-3 keep officers' work correct when several people
use one grant or the connection is poor. OP-5 is small, and a platform needs it before it can run
several copies. OP-6 answers the program manager's questions from data the system already records.

OP-1 is listed first because it is a property of the whole design, not a feature, and the others
depend on it.

**OP-1: several copies, restarted at any time.** For an officer this means every copy shows the
same data, a copy that stops mid-request leaves the data fully changed or unchanged, and a restart
costs at most one retry.

*In a real deployment:*

- The API keeps no state, so any copy can stop and another can take the next request.
- Every copy uses one shared database server, such as PostgreSQL or SQL Server. A SQLite file
  cannot be shared between servers.
- Rules that span copies are enforced in the database, as the OP-2 version check is.
- Each request is one transaction and is safe to repeat, because a retry may reach another copy.
- A copy receives requests only while `/health/ready` passes (OP-5), and finishes the requests it
  has accepted before it stops.
- Migrations run once from the release pipeline, and each schema change works with both old and
  new copies during a rollout.

*In this exercise:* the API keeps no state, the version check is in the database, each request is
one transaction, and the readiness endpoint exists. Completing and closing are safe to repeat.
Adding a task is not yet (see OP-3). "Today" comes from the office's time zone, so copies on
different hosts agree on which tasks are overdue.


**OP-2: several officers on the same grant.** Without protection, one officer could add a High
task while another closes the grant, and both would succeed, which breaks BR-5. Each grant has a
`Version` number. Every change to a grant or its task list increases it, and the database saves a
change only if the version is still the one that was loaded. The second officer gets a 409, and
the screen reloads the grant and explains what happened.

**OP-3: unreliable connections.** Completing a task and closing a grant are safe to repeat. If the
server cannot be reached, the screen shows a banner with a Retry button. This includes a proxy
answering 500 or 502 with no error body. *Not done:* adding a task twice creates two tasks. The
fix is an idempotency key: the browser sends a unique id with each new task, and the server ignores
a second request with the same id.

**OP-5: automatic health check.** The operations team needs to stop sending officers to a copy
that is not working or cannot reach its data. A copy can be stopped, running but cut off from its
data, or healthy.

*In a real deployment* the check is made from outside the copy. An endpoint cannot report that
its own process has stopped. The endpoints are the question, and something else has to ask it:

- The load balancer or orchestrator calls every copy every few seconds. No answer counts as a
  failure, which is how a stopped or hung copy is found. After a few failures the copy is taken
  out of rotation and restarted.
- A copy that is running but has lost its database looks fine from outside. Only the copy knows,
  so it has to be asked. This is the one part that belongs in the application code.
- A monitor outside the network calls the public address. It catches what per-copy checks cannot:
  every copy down, the load balancer, DNS or an expired certificate. It alerts a person.
- An alert on the rate of failed requests catches a copy that passes its check but fails officers.

*In this exercise:* only the application's part is built. `GET /health/live` answers 200 while
the process is running and checks nothing else, so a database problem does not get a working
process restarted. `GET /health/ready` queries the grants table and answers 503 when the database
does not answer. Both return JSON with each check's name, status and duration. Nothing in the
exercise calls them. The load balancer, the outside monitor and the alert are platform
configuration and are not built.

**OP-6: daily activity and overdue-list speed.** The program manager wants to know how many tasks
were created and completed each day, and whether the overdue list slows down as data grows.

*In a real deployment:* counts come from the saved data, because a counter in memory is lost on
restart and split across copies. The manager reads them on a dashboard such as Power BI or
Metabase, over a read-only copy of the database. Request times per route go to a monitoring
service through OpenTelemetry, with an alert on the overdue list. A load test with realistic data
runs before each release.

*In this exercise:* `GET /reports/task-activity?days=14` returns the tasks created and completed
per office day, plus the total and overdue task counts and how long the overdue query took. `days`
accepts 1 to 366. A **Task activity** panel at the bottom of the screen shows the report as a
table and loads nothing until it is opened. `OverdueQueryPlanTests` fails if the overdue query
stops using its index. Unlike a timing check, that gives the same result on any machine.

Limits: SQLite cannot compare `DateTimeOffset` values in SQL, so the API groups created tasks by
day itself. A server database would do it in one `GROUP BY`. The timing is one sample per request.
The dashboard, the OpenTelemetry export and the load test are not built.

**OP-4: support looking into problems days later.** Not built. I would add structured logs with a
request id on every log line and in every error response, kept in a central log store, and record
which officer made each change once there is a login.

**Missing from the list:**

- **Login and permissions.** Anyone who can reach the API can change any grant.
- **Several time zones.** Regional offices may not share one time zone.
- **Backups.** Nothing backs up the database.

## Problems I expect in daily use

| Problem | What I did, or would do |
|---|---|
| Two officers change the same grant at once | Done: version check, 409, and the screen reloads the grant. |
| A click is sent twice on a bad connection | Done for complete and close. Adding a task needs an idempotency key. |
| The API is unreachable | Done: a banner and a Retry button. |
| A copy stops, or loses its database | Application part done: the two health endpoints. The platform must call them and take the copy out of rotation. |
| The date changes at midnight in the office | Done for one time zone. Several zones need a zone per office. |
| The overdue list slows down as data grows | Done: an index, and a test on the query plan. Would add a timing alert and pagination. |
| An officer reports a problem days later | Would add request ids and searchable logs. |

## What I would do next

1. An idempotency key for adding a task.
2. The missing tests listed in [TESTING.md](TESTING.md): the 409 over HTTP, the office time zone,
   and one end-to-end Playwright test.
3. Error responses described in the OpenAPI document.

## AI usage

See [AI_USAGE.md](AI_USAGE.md). The project rules given to the AI are in [CLAUDE.md](CLAUDE.md).

## Time spent

_To fill in._
