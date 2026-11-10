# Testing strategy

## Summary

The tests protect three things officers depend on: a business rule is never skipped, an overdue
task is never missed, and a refused action is explained on the screen.

A real project tests the rules on their own, the API on the database engine it runs on, and the
screen in a browser. This exercise uses the same three levels at a smaller size: the business
rules in isolation, the HTTP API with a real SQL database behind it, and the screen in a simulated
browser. Every test runs with a fixed date, so the results do not depend on the day you run them.


## The three levels

| Level | What it is | What it checks | Tests |
|---|---|---|---|
| **Domain unit tests** | Calls the `Grant` and `GrantTask` classes directly. No web server, no database. | Every business rule and its edge cases, such as "due today is allowed, due yesterday is not". | 21 |
| **API integration tests** | Starts the real API in memory and sends real HTTP requests. The database is SQLite running in memory. | What unit tests cannot see: status codes, the error body, saving to the database, the seed data and sort order. | 29 |
| **Front-end tests** | Renders the React components in a simulated browser (jsdom). The API is replaced with a stub. | Form validation, how API errors are shown, the "server cannot be reached" banner, and the activity report panel. | 16 |

**Why split it this way.** The business rules live in the domain classes, so that is the cheapest
place to test every boundary. The API tests use one example per rule to show the rule is wired
through to HTTP.

**Why a real database engine in the API tests.** Sorting, date comparison and constraints differ
between engines, so integration tests should run on the engine the system uses. This exercise runs
on SQLite, so its tests use SQLite too, held in memory to stay fast. The EF Core in-memory provider
is not a real database and cannot show that a query translates to SQL.

## How the tests stay reliable

- **A fixed date.** Time comes from .NET's `TimeProvider`. Tests swap in a `FakeTimeProvider`
  pinned to 15 March 2026, and some move it forward to check the overdue logic.
- **A fresh database for every test.** Each test starts its own API with its own in-memory
  database and seed data, so the order the tests run in does not matter.
- **Known starting data.** The API tests start from the sample grants in the brief. A real system
  has no sample data, so each test there would create the grants it needs.

## What each business rule is tested by

| Rule | Domain test | API test | Front end |
|---|---|---|---|
| BR-1 No task on a closed grant | `AddTask_OnClosedGrant` | `AddTask_RuleViolation_Returns422WithCode` | The API error is shown in the form |
| BR-2 Due date not in the past, not before the application date | `AddTask_DueDateRelativeToToday_RejectsPastDates`, `AddTask_DueBeforeApplicationDate_Throws` | `AddTask_RuleViolation_Returns422WithCode` | `validate` tests |
| BR-3 Title required, 100 characters or fewer | `AddTask_MissingTitle_Throws`, `AddTask_TitleLength_AllowsAtMost100Characters` | `AddTask_RuleViolation_Returns422WithCode` | `validate` tests |
| BR-4 Completion date is recorded once and never changed | `Complete_RecordsTodayAsCompletionDate`, `Complete_OnAlreadyCompletedTask_KeepsOriginalDate` | `Complete_RecordsTodayAndRepeatOnLaterDayKeepsOriginalDate` | — |
| BR-5 Cannot close with a pending High task | `Close_WithPendingHighTask_Throws`, `Close_WithCompletedHighAndPendingMediumTasks_Closes` | `Close_WithPendingHighTask_Returns422WithCode` | The refusal reason is shown |
| BR-6 Overdue means pending and due before today | `IsOverdue_PendingAndDueBeforeToday` | `Overdue_MovesWithTheClock`, `Overdue_ExcludesTaskDueToday` | Overdue rows are highlighted |

## Tests for the operations risks

- **OP-1, several copies restarted at any time.** No test runs several copies. The tests cover the
  properties that make several copies safe: the database enforces the rule that spans copies
  (OP-2), a repeated request changes nothing (OP-3), and a copy without its database reports that
  it is not ready (OP-5).
- **OP-2, officers working on the same grant at once.**
  `Close_WithStaleGrant_AfterAnotherOfficerAddedHighTask_IsRejected` replays a race: one officer
  adds a High task while another closes the grant from an out-of-date copy. The close must be
  rejected and the grant must stay open. The test uses two database sessions, not two HTTP
  requests, so the timing is the same on every run.
- **OP-3, repeated requests and lost connections.** Completing a task twice and closing a grant
  twice both return 200 and change nothing the second time. On the front end, a failed request
  shows a banner with a Retry button.
- **OP-5, health checks.** `HealthEndpointTests` checks that `/health/live` answers 200 without
  running any check, that `/health/ready` answers 200 with the database check listed, and that
  `/health/ready` answers 503 while `/health/live` still answers 200 once the database is gone.
  The last case closes the in-memory SQLite connection, which destroys the database.
- **OP-6, daily activity and overdue-list speed.** `ReportsEndpointTests` checks that tasks are
  counted on the right days, that completing the same task twice counts once, and that days
  follow the office's calendar: at 22:30 UTC on 15 March it is already 16 March in Sydney.
  `OverdueQueryPlanTests` fails if the overdue query reads every row instead of using its index.
  `ActivityReport.test.tsx` checks that the panel requests nothing until opened, lists days newest
  first with totals, and reports an unreachable server.

## How I checked that the tests catch bugs

Tests written with AI help can pass without testing anything. So I broke the code on purpose and
checked that tests failed:

| Change made to the code | Result |
|---|---|
| BR-2: `<` changed to `<=`, so a task due today is rejected | 3 tests failed (two unit, one API) |
| BR-5: the High-priority check removed | 2 tests failed (one unit, one API) |
| BR-6: `<` changed to `<=` in the overdue query, so a task due today counts as overdue | 2 tests failed (one unit, one API) |
| OP-6: the index on completion date and due date removed | `OverdueQueryPlanTests` failed |
| OP-6: created tasks counted by their UTC date, not the office's date | `TaskActivity_FollowsTheOfficeCalendarNotUtc` failed |

The first BR-6 check failed only one unit test, because no seeded task is due today. I added the
API test `Overdue_ExcludesTaskDueToday` to close that gap. The table shows the current results.

## What is not tested, and the risk

| Not tested | Risk |
|---|---|
| Sending "add task" twice | A retry after a lost response creates a duplicate task. |
| The 409 response over HTTP | The race is tested, but not the step that turns it into a 409 or the message on the screen. |
| The office time zone | Tests run in UTC. A wrong office-zone setting would shift "today" near midnight unnoticed. |
| Front end against the real API | Front-end tests use a stubbed API, so a change to the JSON shape on one side would not be caught. Checked by hand in the browser. |
| The real date in front-end tests | Front-end validation reads the computer's date. A run that crosses midnight could fail. |
| Several API copies on one database server (OP-1) | Nothing runs two copies together, stops one mid-request, or runs the queries on a database server, where sorting and dates can differ from SQLite. |
| Schema changes during a rollout (OP-1) | There are no migrations, so nothing checks a change while old and new copies run side by side. |
| Speed as data grows (OP-6) | The overdue query is shown to use its index, but it has never been timed with a realistic amount of data. The daily counts read every task's creation time on SQLite, and that cost is not measured. |

## Before going live, I would add

1. An idempotency key on "add task", with a test.
2. An API test for the 409 response and a front-end test for its message.
3. A test that sets the office time zone and checks "today" just before and after midnight.
4. A fixed date in the front-end tests (`vi.setSystemTime`).
5. One end-to-end smoke test with Playwright: the real front end against the real API.
6. The API tests run on the production database engine, started in a container.
7. A release pipeline step that applies each migration and runs the previous version's API tests
   on the new schema.
8. A load test of the overdue list with a realistic amount of data.
