# 0002. Backend internal style

## Context

[0001](0001-modular-monolith.md) splits the backend into modules. Inside each module we need one way to write a use case: how a request arrives, how it is validated, how failures are reported and where cross-cutting behaviour (transactions, event dispatch, logging) lives. MediatR, the usual choice, now requires a commercial licence for new versions.

## Decision

**Vertical slices with minimal APIs.** One folder per use case holds its endpoint, request and response types, handler and validator. Domain types (aggregates, value objects) live in the module's `Domain` folder and are shared by its slices. There is no repository layer over EF Core: handlers use the module's DbContext directly.

**Own handler abstraction.** `HireLab.SharedKernel` defines `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>`. Decorators add the cross-cutting behaviour in one place:

1. Logging
2. Validation
3. Unit of work (commands only): save changes, then dispatch integration events after the commit. A lost optimistic-concurrency race (a `DbUpdateConcurrencyException` from the handler or the save) becomes the `409` error `Concurrency.ChangedMeanwhile`

No mediator library.

**Validation** uses FluentValidation and runs in the validation decorator. A failure returns a validation error and the handler never runs.

**Errors.**
- Expected failures (not found, forbidden, invalid state, validation) are returned as a `Result<T>` carrying an `Error` with a code and a type, never thrown.
- Endpoints map `Error` to RFC 9457 `ProblemDetails` with the right status code.
- Exceptions are only for bugs and infrastructure failures. A global exception handler turns them into a 500 `ProblemDetails` without leaking details.

**API surface.** Two route groups:

| Group | Used by |
|---|---|
| `/api/staff/...` | The `staff` app |
| `/api/candidate/...` | The `candidate` app |

Each group requires its auth policy at the group level (HLB-15 decides the schemes) and produces its own OpenAPI document. A module exposes `MapStaffEndpoints` and/or `MapCandidateEndpoints`; the API host only wires modules into groups.

## Consequences

- A use case is one folder to read. Adding one doesn't touch shared layers.
- We own about a hundred lines of plumbing (handler interfaces, decorators, `Result`) instead of depending on a mediator library and its licence.
- Without repositories, handlers depend on EF Core. Tests for them run against a real database rather than mocks (HLB-18).
- Forgetting auth on a single endpoint is impossible by construction. A new route group has to choose its policy explicitly.
- Two OpenAPI documents keep the candidate client from seeing staff endpoints at all.

## Status

Accepted
