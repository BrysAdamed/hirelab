# 0003. Repository and frontend workspace layout

## Context

The repo holds a .NET backend ([0001](0001-modular-monolith.md), [0002](0002-backend-internal-style.md)) and two Angular apps: `candidate` for the person being interviewed and `staff` for admins, managers and interviewers. Candidate code must never ship staff code. Local development should start with one command. The repo is public and fully self-contained: no private package feeds.

## Decision

**Runtimes and baseline.**
- .NET 10 (LTS) and the latest stable Angular.
- `global.json` pins the SDK.
- Central Package Management (`Directory.Packages.props`).
- `Directory.Build.props`: nullable enabled, `TreatWarningsAsErrors`, .NET analyzers.
- `.editorconfig` for both C# and TypeScript.
- Only public packages from nuget.org and npm.

**Layout.**

```
HireLab.slnx
global.json
Directory.Build.props
Directory.Packages.props
src/
  HireLab.AppHost/                Aspire orchestration
  HireLab.ServiceDefaults/        OpenTelemetry, health checks, resilience
  HireLab.Api/                    Host: composition, auth, route groups, OpenAPI
  HireLab.SharedKernel/           Result, Error, handler abstractions, events
  Modules/
    <Name>/HireLab.Modules.<Name>/
    <Name>/HireLab.Modules.<Name>.Contracts/
  web/                            Angular CLI workspace
    projects/
      candidate/                  App
      staff/                      App
      ui/                         Design system components and tokens
      api-client/                 Generated client, entry points: staff, candidate
      tasks/                      Task type UIs, entry points: candidate, review
      core/                       Auth, HTTP, error handling, utilities
tests/
  HireLab.ArchitectureTests/
  ...                             Test projects, decided in HLB-18
```

**Angular workspace.**
- A plain Angular CLI multi-project workspace, not Nx.
- Apps consume libs through TypeScript path mappings, with no separate library builds.
- Import rules, enforced by lint:
  - Apps may import libs; libs never import apps.
  - `candidate` never imports `staff`, and vice versa.
  - `api-client/staff` is off-limits to `candidate`.

**Frontend style.** Standalone components, zoneless change detection, signals for state, `inject()` for dependencies, signal forms where stable. No global store; a feature-level signal store service only where a feature needs shared state.

**API client.** Generated from the backend's two OpenAPI documents (staff and candidate) as TypeScript types with a thin typed fetch wrapper (for example `openapi-typescript` + `openapi-fetch`). The generated code is committed, and CI fails when it differs from what the current API produces.

**Local orchestration.** The Aspire AppHost runs the API, PostgreSQL, Azurite and both Angular dev servers, so one `dotnet run` on the AppHost starts everything with ports and proxies wired in one place.

## Consequences

- One repo and one command to run the whole system locally.
- The candidate bundle contains only candidate code by construction; a lint rule backs it up.
- Without Nx, there are no affected-only builds or caching. With two apps that's fine; we revisit if build times hurt.
- Generated client code adds noise to diffs, but API drift between backend and frontend is caught in CI instead of at runtime.
- Pinning .NET 10 LTS means skipping .NET 11 (STS) features until the next LTS.

## Status

Accepted
