# AI usage

An honest account of how AI was used on this exercise. Written as the work happened.

## Tools and models

- Claude Code (desktop app) running Claude Fable 5.1, for all design discussion, code, and tests.
- JetBrains Rider for project scaffolding, reading diffs and running the solution.

## Agent setup

- A workspace-level CLAUDE.md above this repository (not committed) sets the working
  relationship: the AI acts as tech lead, expected to challenge designs and flag risk;
  scope decisions stay with me.
- This repository's `CLAUDE.md` holds the project rules: layering, where business rules live,
  the clock rule, error mapping, test expectations. It was written once the layout was decided
  and is updated whenever a convention is settled.
- Permission settings: the desktop app's auto mode approves edits and commands inside the
  project folder; a workspace-level `settings.json` allow-lists read-only git and file
  commands. This repository has no `.claude/settings.json` of its own; everything else ran
  on defaults.

## Workflow

1. Read the brief and summarise it back (requirements, rules, weights) before any design.
2. Design review: I drew the data model, the AI reviewed it against the rules.
3. Scaffold in Rider, AI reviewed the template options.
4. Build Domain, Data, API. Verified by running the API and calling every endpoint with curl.
5. Tests, then operations needs, then the front end.

## Key prompts

> "Here is the schema that I made, can you verify if this correctly describes the business
> model of this task."

> "In this design with only 2 models, how do different officers interact with one same grant?
> Is it assumed that security is not a factor in this task?"

> "Implement the Api and Data domains, then seed the data in the pdf file. Hold off on
> creating a git repo, as I want to include the frontend as well."

## Where the AI was wrong, incomplete or over-engineered

- **EF Core marked a new task as Modified, not Added (2026-11-06).** The AI wrote `grant.AddTask(...)` to
  append the task to the tracked grant and relied on change detection to insert it. Because the
  domain assigns the Guid key, EF assumed the task already existed and issued an UPDATE that matched
  no rows, which surfaced as a 409 on a plain, uncontested POST. Caught by running every endpoint
  with curl straight after the build, not by reading the code. Fix: an explicit `db.Tasks.Add(task)`
  in the service. Lesson kept in CLAUDE.md: "it compiles" is not "it works".

- **Over-engineered: a hand-rolled `IClock` (2026-11-06).** To make "today" testable, the AI
  introduced its own `IClock` interface and a `SystemClock` implementation. It compiled and the
  rules worked, so nothing flagged it. I asked whether the wrapper earned its place. It did not:
  .NET has shipped `TimeProvider` since .NET 8, with `FakeTimeProvider` for tests, so the AI had
  added an abstraction, a class and a registration to re-create something the framework already
  provides. A reader would also have had to learn a custom type where a standard one exists.
  Fix: removed `IClock` and `SystemClock`, injected `TimeProvider`, and added a one-line subclass
  that pins the local zone to the office's (`OfficeTimeZone`) so a server running in UTC does not
  shift "today". Tests use `FakeTimeProvider`. The same review found `CreatedOn` typed as
  `DateOnly`; it is a system timestamp, now `DateTimeOffset`.
  Lessons kept in CLAUDE.md: time comes only from the injected `TimeProvider`; dates from people
  are `DateOnly`, timestamps are `DateTimeOffset`. For next time: before the AI adds a wrapper or
  an interface, ask what the framework already offers and what the wrapper does that it cannot.

- **Incomplete: business-rule codes were free-typed strings (2026-11-09).** The AI designed the
  422 error contract well at the edge: every rule violation returns ProblemDetails with a `code`
  that clients can branch on. Inside the code, though, each `throw` typed that code by hand
  (`new BusinessRuleException("InvalidDueDate", ...)`), and so did the test assertions. Nothing
  checked those strings. A typo such as `"InvalidDuedate"` would compile, pass the domain test
  that repeated the same typo, and quietly break any client matching on the code. There was also
  no single list of the codes a client can receive. All tests were green, so nothing flagged it.
  Found in my own review of the code, not by a test: I asked whether the codes should be an enum
  or data rather than strings. Fix: a `BusinessRuleCode` enum in the Domain project. `BusinessRuleException`
  now takes the enum, so it cannot be thrown with an unknown code, and the domain tests compare
  against the enum. `ApiExceptionHandler` writes the enum's name, so the JSON is unchanged
  (`"code": "InvalidDueDate"`) and the front end did not change. The API tests deliberately keep
  the literal strings: they check what clients receive, so renaming an enum value fails them and
  warns that the rename breaks clients. Rejected alternatives: a class of `const string` values
  (stops typos but still accepts any string), sending the enum as a number (unreadable, and
  reordering the enum would change the meaning), and codes in a table or config file (each code
  belongs to a rule written in code). Lesson kept in CLAUDE.md: a rule violation takes a
  `BusinessRuleCode`, never a string.

- The AI wrote the add-task form with inputs nested inside `<label>` elements. Its own Vitest
  test then failed: `getByLabelText('Title')` could not find the field because the label's text
  included the character counter. Fixed by giving each input an `id` and the label a `htmlFor`,
  which is also what a screen reader needs. The test caught an accessibility bug.
- The front end treated "API unreachable" as a rejected `fetch` only. Verified in the browser
  with the API stopped: through the Vite dev proxy the browser gets a 500 with no body, so the
  screen said "Internal Server Error" instead of showing the connection banner. A reverse proxy
  in production returns 502 the same way. Fixed in `api.ts` (5xx without a ProblemDetails body
  is a `ConnectionError`) and covered by a test.
- Completing a task reloaded the grant but not the grant list, so the drop-down's pending count
  went stale. Seen in the browser, not by any test. Fixed by reloading both.

## Where I pushed back or rewrote

- My first schema had a `GrantMaster` (officer with username/password) entity and an
  `IsCompleted` flag on tasks. Review showed the flag cannot satisfy BR-4 (the completion date
  must be recorded and never changed) and the officer entity was out of scope. Replaced with
  `CompletedOn: DateOnly?` and dropped the officer entity; noted auth as a before-production item.

## Log

- 2026-11-05: read the brief, summarised requirements and assessment weights.
- 2026-11-06: schema review; Rider scaffold review; Domain, Data and API implemented and
  verified by hand against every endpoint and rule. No git repo yet by choice: first commit
  will include the front end scaffold. Later: challenged `DateOnly` and the `IClock` wrapper.
  Kept `DateOnly` for people's dates; replaced `IClock` with `TimeProvider` and an office zone;
  `CreatedOn` became `DateTimeOffset`. Decided the "what if a due date needs a time?" answer:
  an optional `DueTime: TimeOnly?` in the office zone, prepared but not built.
- 2026-11-07: tests and front end. 41 xUnit tests (domain unit, API integration on in-memory
  SQLite with `FakeTimeProvider`, one OP-2 race test). Break-a-rule check: BR-2 `<` to `<=`
  failed 2 tests, BR-5 check removed failed 2 (unit + API), BR-6 `<` to `<=` failed 1 (unit only,
  no seed task is due today) so an API test for due-today was added. React app in
  `GrantTrackerApp` with 13 Vitest tests; verified in the browser against the running API,
  including the API-stopped case, which found the proxy 500 bug above.
- 2026-11-09: replaced the free-typed business-rule code strings with a `BusinessRuleCode` enum
  (see above). The wire format is unchanged; 41 xUnit and 13 Vitest tests pass.
