# 0014. Testing strategy

## Context

Handlers use EF Core directly ([0002](0002-backend-internal-style.md)), so mocking the database tells us little. Much of the risk sits in rules that span the codebase: module boundaries, team scoping, candidate versus staff access, task immutability ([0001](0001-modular-monolith.md), [0009](0009-authorisation-model.md), [0013](0013-task-module-contract.md)). Nothing is deployed until MVP 1 ([0010](0010-hosting-and-environments.md)), so CI is the only gate before go-live.

## Decision

**.NET stack.**
- xUnit v3, Shouldly for assertions, NSubstitute for the few mocks.
- Mocks only at real boundaries: the LLM provider, the clock (`FakeTimeProvider`).
- No FluentAssertions (commercial licence) and no Moq.

**Backend test levels.** Integration-heavy:

| Level | Covers |
|---|---|
| Integration (most tests) | Each module's real HTTP endpoints through `WebApplicationFactory`, against real PostgreSQL and Azurite |
| Unit | Dense logic only: aggregates and state machines, rubric scoring, task loading |
| Architecture | ADR rules: module references, `internal`, `IgnoreQueryFilters` only in the migration worker and system jobs, no SignalR in modules |
| Contract | The candidate OpenAPI document contains no staff-only fields |

**Integration infrastructure.**
- Testcontainers for PostgreSQL and Azurite, one set of containers per test assembly.
- Respawn resets data between tests.
- E2E starts the whole app by running the AppHost (`dotnet run`) from Playwright's `webServer`; Aspire.Hosting.Testing isn't used.

**Auth sweep.** A test enumerates every mapped endpoint and asserts:
- Anonymous requests get 401.
- The candidate cookie is rejected on staff endpoints, and the staff cookie on candidate endpoints.
- Another team's resources return 404.
- The admin gets nothing from candidate-data endpoints.

New endpoints are covered without anyone writing a test.

**Test projects:**

| Project | Contents |
|---|---|
| `HireLab.Modules.<Name>.Tests` | Unit and integration tests of one module |
| `HireLab.Tasks.Tests` | Every task loads and validates; the `tasks.lock.json` guard |
| `HireLab.Api.Tests` | OpenAPI contract tests, the auth sweep |
| `HireLab.ArchitectureTests` | ADR rules |
| `tests/e2e` | Playwright suite |

**Test data.** Hand-written test data builders per aggregate, with obviously fake values (`example.com`, "Jane Doe"). No AutoFixture or Bogus.

**Frontend.** Vitest through the Angular CLI unit-test builder, with Angular Testing Library for components. Tests cover stores, services, the app shell's autosave and sync, and task type components' behaviour, not every presentational component.

**E2E.** Playwright (TypeScript) in `tests/e2e`, run against the app started through Aspire. A small suite of about 5–10 key flows:
- Login
- Create a session and queue a task
- The candidate link: waiting page, then Live
- A candidate completes a code review while the interviewer's view updates live (two browser contexts)
- Finishing a session locks the candidate
- The admin switches a task's active version

Edge cases belong in integration tests.

**CI.**
- **Every PR:** backend unit, integration, architecture and contract tests; frontend unit tests; lint; drift checks.
- **E2E:** also on every PR while the suite stays under about 5 minutes. If it grows, it moves to `main` plus manual runs.
- **Coverage:** collected and shown in the PR summary, with no threshold gate.

**Definition of done for feature tickets.**
- Integration tests for every new endpoint: the happy path and each domain rule that can fail.
- Unit tests where logic is dense.
- A bug fix starts with a failing test.

## Consequences

- Most tests exercise the real stack, so refactoring internals rarely breaks them, and EF Core mapping errors are caught.
- Integration tests need Docker locally and in CI, and they are slower than unit tests. Containers shared per assembly and Respawn keep that manageable.
- The auth sweep and the architecture tests turn ADR rules into failing builds instead of review comments.
- Without a coverage gate, coverage relies on the definition of done, not on a number.
- E2E on every PR adds minutes to each run. The suite stays small on purpose.

## Status

Accepted
