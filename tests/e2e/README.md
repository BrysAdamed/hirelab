# E2E tests

Playwright (TypeScript) against the whole app started through the Aspire AppHost ([ADR 0014](../../docs/adr/0014-testing-strategy.md)): PostgreSQL, Azurite, the migration worker, the API and both Angular dev servers. The suite opens the `staff` app at `http://localhost:4200/` and the `candidate` app at `http://localhost:4201/s/`.

## Prerequisites

- .NET SDK, Node and npm, and a running Docker engine.
- A development HTTPS certificate for the API: `dotnet dev-certs https` (it doesn't need to be trusted).
- Once per machine, the browser: `npm run install-browsers` (on Linux it also installs the system packages, which needs sudo).

## Run

```sh
cd tests/e2e
npm ci
npm test
```

Playwright starts its own AppHost with `dotnet run --project ../../src/HireLab.AppHost --launch-profile http` and waits until both apps answer, which takes a minute or two on a cold start. It fails if the ports are taken, so stop an AppHost you started yourself first. That AppHost runs with `HIRELAB_E2E_EPHEMERAL_DATA=true`: PostgreSQL and Azurite get no data volumes, so every run starts from an empty, freshly seeded database and your development data is never touched.

To iterate faster, start the AppHost yourself with `HIRELAB_E2E_EPHEMERAL_DATA=true` and run the suite with `E2E_REUSE_APP=1`, which reuses it instead of starting a new one. Without the variable on the AppHost, the tests write their users and teams into your development database. `CI` ignores `E2E_REUSE_APP`.

`npm run typecheck` type-checks the suite. The HTML report lands in `playwright-report/`, and traces of failed tests in `test-results/`.

## Notes

- On Linux and macOS, Playwright stops the AppHost with `SIGINT`, so Aspire removes its containers. On Windows it can only kill the process tree, which leaves the AppHost's PostgreSQL and Azurite containers running; the next AppHost start removes them, but their anonymous data volumes stay behind. Clean those up now and then with `docker volume prune`.
- Staff tests log in as the development seed users (`src/Modules/Identity/HireLab.Modules.Identity/Seed/development-seed.json`), which `staff-users.ts` reads. Tests that need other users create them.
- Every browser reaches the API through the dev server proxy, so the API sees one client address and the login rate limit (10 a minute) would be shared by the whole suite. The suite's AppHost gets `HIRELAB_E2E_AUTH_PERMIT_LIMIT=1000`, which it passes to the API as `RateLimiting:AuthenticationPermitLimit`. A reused AppHost you started yourself keeps the normal limit, so parallel runs against it can hit 429s.
- Tests run in parallel against one database, so each test creates the data it needs and doesn't rely on an empty database.
