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

Playwright starts the AppHost with `dotnet run --project ../../src/HireLab.AppHost --launch-profile http` and waits until both apps answer, which takes a minute or two on a cold start. Locally, an AppHost that is already running is reused; with `CI` set, the suite always starts its own and fails if the ports are taken.

`npm run typecheck` type-checks the suite. The HTML report lands in `playwright-report/`, and traces of failed tests in `test-results/`.

## Notes

- On Linux and macOS, Playwright stops the AppHost with `SIGINT`, so Aspire removes its containers. On Windows it can only kill the process tree, which leaves the AppHost's PostgreSQL and Azurite containers running; the next AppHost start removes them.
- Staff tests log in as the development seed users (`src/Modules/Identity/HireLab.Modules.Identity/Seed/development-seed.json`), which `staff-users.ts` reads. Tests that need other users create them.
- The AppHost keeps PostgreSQL data in a named volume, so data survives between runs. Tests must create the data they need and not rely on an empty database.
