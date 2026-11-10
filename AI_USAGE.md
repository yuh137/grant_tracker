# AI usage

How AI was used on this exercise, written as the work happened.

## Tools and models

- Claude Code (desktop app) running Claude Fable 5.1, Opus 5.5 and Sonnet 5.5, for all design discussion, code, and tests.
- JetBrains Rider for project scaffolding, reading diffs and running the solution.

## Agent setup

- This repository's `CLAUDE.md` holds the project rules: layering, where business rules live, the
  clock rule, error mapping and test expectations. It is updated whenever a convention is settled.
  `AGENTS.md` points other agents to it.
- A workspace-level CLAUDE.md above this repository (not committed) sets the working
  relationship: the AI acts as tech lead and is expected to challenge designs and flag risk.

## Workflow

1. Read the brief and summarise the requirement and rules before any design.
2. Design review: I designed the data model, Claude reviewed it against the rules.
3. Scaffold in Rider. Claude reviewed the template options.
4. Build Domain, Data and API. Verified by running the API and calling every endpoint with curl.
5. Tests, then operations needs, then the front end.
6. From the OP-1 review on, each operations need is described in the same order: what it means
   for an officer, how a real deployment handles it, and what this exercise does.

## Key prompts

> "Here is the schema that I made, verify if this correctly describes the business
> model of this task."

> "In this design with only 2 models, how do different officers interact with one same grant?
> Is it assumed that security is not a factor in this task?"

> "Implement the Api and Data domains, then seed the data in the pdf file."

## Where the AI was wrong, incomplete or over-engineered



- **Over-engineered: a hand-rolled `IClock` (2026-11-06).** The AI wrote its own clock interface
  to make "today" testable. .NET already provides `TimeProvider` and `FakeTimeProvider`, so I had
  it replaced. For next time: ask what the framework offers before accepting a wrapper.

- **Incomplete: rule codes typed by hand (2026-11-09).** Every `throw` and every test typed the
  error `code` as a string, so a typo would compile and break clients. All tests were green. I
  found it reading the code. Fix: a `BusinessRuleErrorCode` enum. The JSON is unchanged.

- **Over-engineered: hardening the setup conveniences (2026-11-10).** For OP-1 the AI proposed
  guards on the recreate-database flag, the seed loader and the shutdown timeout. I rejected
  them. The first two exist only so the exercise runs with one command, and the timeout was
  already the .NET default. For next time: ask whether the thing being fixed would exist in the
  real system.

- **Wrong: a new task was saved as an UPDATE (2026-11-06).** Claude added the task to the tracked
grant and let EF Core detect it. Because the domain sets the Guid key, EF treated the task as
existing, and an ordinary POST returned 409. Found by calling every endpoint with curl. Fix: an
explicit `db.Tasks.Add(task)`.

- **Front-end bugs (2026-11-07).**
  - Inputs nested inside `<label>` broke the AI's own test. Fixed with `id` and `htmlFor`, which
    a screen reader also needs.
  - With the API stopped, the Vite proxy returns a 500 with no body, and the screen said
    "Internal Server Error" instead of the connection banner. Fixed in `api.ts`, with a test.
  - Completing a task did not reload the grant list, so the pending count went stale. Seen in
    the browser, not by a test.
