# 0018. Strict JSON number handling

## Context

ASP.NET Core minimal APIs use the web defaults of `System.Text.Json`, which read numbers from JSON strings as well as from JSON numbers (`JsonNumberHandling.AllowReadingFromString`). Since .NET 10, the built-in OpenAPI generator describes such numbers honestly: every integer and decimal becomes `integer | string` in the schema.

The frontend client is generated from those documents ([0003](0003-repository-and-frontend-layout.md)). With the default, every numeric field in TypeScript becomes `number | string`, which pushes type checks and conversions into every caller and hides real mistakes.

## Decision

**The API serialises and reads numbers strictly.** `Program.cs` sets `JsonNumberHandling.Strict` on the HTTP JSON options, for both route groups.

- Numbers are always JSON numbers on the wire, in both directions.
- A number sent as a string (`"42"`) fails request binding with a `400`, and the handler never runs.
- The OpenAPI documents describe numeric fields as plain `integer` / `number`, so the generated client types them as `number`.

## Consequences

- The generated TypeScript types match what the API actually sends and accepts.
- Clients that send numbers as strings are rejected. Both clients are ours and generated, so this only catches bugs.
- Values that don't fit a JSON number safely (for example 64-bit ids beyond 2^53) must be modelled as strings explicitly. Our ids are `Guid`s ([0004](0004-database-and-ef-core.md)), so this doesn't arise today.
- Changing this setting changes every numeric field in both OpenAPI documents; the API client drift check catches it.

## Status

Accepted
