# HireLab - notes for agents

Live technical interview platform: a task bank authored in the app, interview sessions with a task queue, and live results for interviewers. Before starting a ticket, read `docs/vision.md` and `docs/glossary.md`. Before changing anything structural, check `docs/adr/`.

## Hard rules

- **Public repo.** Never commit real personal data: candidate names, CVs, emails, company names, real interview notes. Use invented examples. Secrets go in user secrets or environment variables only.
- Planted "personal data" and "secrets" in AI-assistant tasks must be obviously fake (reserved domains such as `example.com`, test card numbers, made-up names).
- Use glossary terms in code, API and UI. When you introduce a new term, add it to `docs/glossary.md`.
- Every structural decision gets a new ADR in `docs/adr/` (next number) and a row in `docs/adr/README.md`.
- Tasks are authored in the app; task *types* are code ([ADR 0015](docs/adr/0015-task-authoring-in-the-app.md)). Published task versions are immutable. Seed packages in `src/Tasks/` are showcase tasks only: never edit a published seed version (`tasks.lock.json` enforces it); add `v<N+1>` instead.
- A new task type ships with three frontend entry points: `candidate`, `review` and `editor`.
- The UI is in English only.
- UI: Spartan UI (helm components live in the `ui` lib and are our code) with Tailwind v4 and design tokens only; no PrimeNG (paid licence). Follow `docs/design-system.md` ([ADR 0016](docs/adr/0016-ui-stack.md)).
- Design is code-first in the `ui` lib, or explored as Claude artifacts. Never use Figma.

## Architecture rules

- Modular monolith ([ADR 0001](docs/adr/0001-modular-monolith.md)). A module references another module only through its `.Contracts` project; no cross-schema foreign keys or joins.
- Vertical slices, minimal APIs, own handler abstraction, `Result` for expected failures, `ProblemDetails` on the wire ([ADR 0002](docs/adr/0002-backend-internal-style.md)).
- Staff endpoints go under `/api/staff`, candidate endpoints under `/api/candidate`. The `candidate` app never imports `staff` code ([ADR 0003](docs/adr/0003-repository-and-frontend-layout.md)).
- The frontend calls the API only through the generated `@hirelab/api-client/staff` and `@hirelab/api-client/candidate` (never hand-written types). After changing an endpoint, run `npm run api:update` in `src/web` and commit the regenerated documents and client; `npm run api:check` (CI) fails on drift.
- Self-contained: only public packages from nuget.org and npm.
- One DbContext per module. Migrations are forward-only; a breaking change goes expand → deploy → contract ([ADR 0004](docs/adr/0004-database-and-ef-core.md)).
- Ids are `Guid` v7 behind strongly typed ids. Timestamps are UTC `DateTimeOffset` from an injected `TimeProvider`.
- Attachments go through the API, never SAS links ([ADR 0005](docs/adr/0005-attachment-storage.md)).
- SignalR hubs are push-only and send thin events (ids plus a revision); clients refetch over HTTP. Modules raise integration events and never reference SignalR ([ADR 0006](docs/adr/0006-real-time-channel.md)).
- Auth is cookies for staff and candidates; no tokens in JavaScript or URLs ([ADR 0007](docs/adr/0007-staff-authentication.md), [ADR 0008](docs/adr/0008-candidate-access.md)). Candidate endpoints take the session from the cookie, never from the route.
- Team-owned aggregates implement `ITeamOwned`; the module conventions give them the `Team` global query filter (the admin sees none). Staff endpoints that need a role use `StaffPolicies` (`Admin`, `Manager`, `ManagerOrInterviewer`) at group or endpoint level. `IgnoreQueryFilters` is allowed only in the migration worker and system jobs ([ADR 0009](docs/adr/0009-authorisation-model.md)).
- No Azure deployment until MVP 1 is done; everything runs locally through Aspire ([ADR 0010](docs/adr/0010-hosting-and-environments.md)). Infrastructure is declared in the AppHost ([ADR 0011](docs/adr/0011-iac-and-ci-cd.md)).
- No personal data in logs or traces; use ids ([ADR 0012](docs/adr/0012-observability.md)).
- Candidate personal data lives only in Positions. Other modules store `CandidateId` and read names through `ICandidateDirectory` in `Positions.Contracts` each time they show them; never copy a name or contact detail. Show a candidate by `DisplayName` ([ADR 0023](docs/adr/0023-candidate-personal-data-stays-in-positions.md)).

## Testing

See [ADR 0014](docs/adr/0014-testing-strategy.md). xUnit v3, Shouldly, NSubstitute; Vitest and Playwright on the frontend.

- Every feature ticket ships with integration tests for its endpoints (the happy path and each domain rule that can fail), against real PostgreSQL through Testcontainers. Don't mock the database.
- Unit tests where logic is dense. A bug fix starts with a failing test.
- Test data comes from hand-written builders with obviously fake values.
- Integration tests derive from `IntegrationTest` in `tests/HireLab.Testing`, which runs the real API on Testcontainers PostgreSQL and Azurite (one set per test assembly) and resets the database with Respawn before each test. A test assembly registers the host with `[assembly: AssemblyFixture(...)]` and disables parallelisation.
- Backend tests (needs Docker): `dotnet test --solution HireLab.slnx`. With Cobertura coverage: add `--coverage --coverage-output-format cobertura --coverage-settings tests/coverage.config --results-directory artifacts/coverage`.
- E2E: `cd tests/e2e && npm ci && npm test` (see `tests/e2e/README.md`).

## Work tracking

Tickets live in the ticketing app (MCP server `ticketing`), project key `HLB`. Epics are HLB-1 to HLB-11. Branch names and commit messages start with the ticket key, e.g. `HLB-12 Add job position aggregate`.

## Layout and commands

- `src/HireLab.AppHost` (Aspire: local orchestration and the Azure resources), `src/HireLab.Api` (host; serves both SPAs), `src/HireLab.MigrationService` (migration worker), `src/HireLab.ServiceDefaults`, `src/HireLab.SharedKernel`.
- `src/Modules/<Name>/HireLab.Modules.<Name>` and `.Contracts`: Identity, Positions, TaskBank, Sessions, Evaluation.
- `src/web`: one Angular workspace with the `staff` and `candidate` apps and the `ui`, `core`, `tasks` and `api-client` libs.
- `tests/`: .NET test projects, `HireLab.Testing` (shared integration host), `coverage.config`; `tests/e2e`: Playwright.
- `infra/`: hand-written Bicep (budget, deployment identity). `eng/`: scripts. `docs/`: vision, glossary, ADRs, design system. `.github/workflows`: `ci.yml` (every PR) and `deploy.yml` (manual).

Commands (from the repo root unless noted; Docker must be running):

- Build: `dotnet build HireLab.slnx`. Run everything locally: `dotnet run --project src/HireLab.AppHost`.
- Backend tests and E2E: see Testing above.
- In `src/web`: `npm ci`, `npm run lint`, `npm test`, `npm run build`, `npm run check:contrast`, `npm run api:update` / `npm run api:check`.
- Operations in Azure (first admin link, `--issue-admin-link`): `docs/operations.md`.
- Infrastructure: `eng/validate-infra.sh` (generates the Bicep from the AppHost, then builds and lints it and `infra/`; needs the Bicep CLI or `az`).
- Container images (local Docker, no push): `dotnet publish src/HireLab.Api -c Release -r linux-x64 -t:PublishContainer` (builds the SPAs unless `-p:SkipSpaBuild=true`), and the same for `src/HireLab.MigrationService`.
