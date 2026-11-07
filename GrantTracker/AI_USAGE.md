# AI usage

An honest account of how AI was used on this exercise. Written as the work happened.

## Tools and models

- Claude Code (desktop app) running Claude Fable 5.1, for all design discussion, code, and tests.
- JetBrains Rider for project scaffolding, reading diffs and running the solution.

## Agent setup

- A workspace-level CLAUDE.md (personal, not committed) defines the working relationship:
  Claude as tech lead, Huy as product owner who decides scope and challenges designs.
- This repository's `CLAUDE.md` holds the project rules: layering, where business rules live,
  the clock rule, error mapping, test expectations. It was written once the layout was decided
  and is updated whenever a convention is settled.
- Permission mode: tool calls auto-approved inside the project folder; anything touching git,
  the network or files outside the folder is reviewed.

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

- The AI wrapped the clock in its own `IClock` interface plus a `SystemClock` class. I asked
  whether the wrapper earned its place. It did not: .NET has shipped `TimeProvider` since .NET 8
  with a `FakeTimeProvider` for tests. Replaced with the framework type, plus a one-line subclass
  that pins the local zone to the office's (`Office:TimeZone`) so a server running in UTC does not
  shift "today". Also found `CreatedOn` typed as `DateOnly`; it is a system timestamp, now
  `DateTimeOffset`. Rule recorded: dates from people are `DateOnly`, timestamps are `DateTimeOffset`.

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
