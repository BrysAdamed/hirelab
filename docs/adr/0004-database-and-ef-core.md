# 0004. Database: PostgreSQL and EF Core

## Context

[0001](0001-modular-monolith.md) gives each module its own schema in one database. We need a database host that fits a personal Azure budget without a cold start during a live interview, a way to evolve schemas safely, and conventions for ids, time, concurrency and task responses.

Options for the host: Azure Database for PostgreSQL Flexible Server (about €18 a month with storage, always on), Neon serverless PostgreSQL (free tier, cold starts, a third-party vendor), the Azure SQL Database free offer (free, but SQL Server), and PostgreSQL in a container on Azure Files (cheap, fragile, no managed backups).

## Decision

**Host.** Azure Database for PostgreSQL Flexible Server, Burstable B1ms, 32 GB, in the same EU region as the app. Locally, the Aspire AppHost runs a PostgreSQL container.

**Access in Azure.**
- Microsoft Entra ID authentication only; password authentication is disabled.
- The API and the migration job connect with their own managed identities. The migration identity holds DDL rights; the API identity holds data rights only.
- Public endpoint with the firewall limited to Azure services. No private endpoint.

**EF Core.**
- One DbContext per module, mapped to the module's schema, with its own migrations folder and its own `__EFMigrationsHistory` table in that schema.
- `snake_case` names through `EFCore.NamingConventions`.
- Reads use `AsNoTracking()` with projections straight into response DTOs. Raw SQL only when a query can't be expressed in LINQ.

**Migrations.**
- A `HireLab.MigrationService` worker applies every module's migrations. Locally, the AppHost runs it and the API waits for it to complete. In Azure, it runs as a Container Apps Job that the pipeline triggers before rolling out a new API revision.
- Forward-only: `Down` methods are never relied on.
- A change that would break the running version (rename, drop, new NOT NULL on an existing column) is split into expand → deploy → contract.

**Seed data.** The migration worker also seeds data:

| Environment | What it seeds |
|---|---|
| Development | Obviously fake positions, candidates (`example.com` addresses), sessions. Idempotent. |
| Azure | Only what the app needs to start, such as the admin user (HLB-15 decides how). |

**Identifiers.**
- `Guid` v7 (`Guid.CreateVersion7()`), generated in the app.
- Strongly typed ids (`SessionId`, `CandidateId` …) as `readonly record struct` with EF value converters.
- Human-readable numbers, such as the per-position candidate number used in "Candidate #N", are separate columns.

**Time.** Every timestamp is UTC: `DateTimeOffset` in .NET, `timestamptz` in PostgreSQL. Code gets the time from an injected `TimeProvider`. Conversion to local time happens only in the UI.

**Concurrency.** Aggregate roots use optimistic concurrency on PostgreSQL's `xmin` column. A conflict is returned as `409` `ProblemDetails`: the unit-of-work decorator ([0002](0002-backend-internal-style.md)) turns it into `Concurrency.ChangedMeanwhile` for every command, and a handler that can say more maps it itself. A unique index that a concurrent request can hit is mapped to a readable `409` by the code that writes it.

**Deleting.** No global soft delete. Domain states (`Closed` positions, `Finished` sessions) keep history where it matters. Everything else is a hard delete.

**Audit columns.** Every table has `created_at` and `updated_at`, set by an EF interceptor. Who performed an action is an explicit domain field only where the domain needs it, for example the author of an evaluation.

**Task responses.** A session task's response is stored as a `jsonb` document with a `schemaVersion`. Its shape belongs to the task type (HLB-17 defines it).

**Backups.** The default 7-day point-in-time restore, geo-redundancy off.

## Consequences

- The database is the largest fixed cost (about €18 a month). Stopping the server is possible but limited to 7 days at a time.
- No database password exists anywhere, locally aside. Running ad-hoc SQL against Azure needs an Entra token.
- Because migrations run before the new revision, old code briefly runs against the new schema. Expand/contract keeps that safe.
- Anonymised data stays in backups for up to 7 days. Restoring a backup older than the latest position closures requires re-running anonymisation for those positions.
- A new task type needs no migration for its responses. In exchange, responses can't be queried by field without JSON operators.
- Strongly typed ids add value converters and a little boilerplate, and catch id mix-ups at compile time.

## Status

Accepted
