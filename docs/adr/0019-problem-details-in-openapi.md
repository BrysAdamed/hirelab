# 0019. ProblemDetails in the OpenAPI documents

## Context

Every expected failure leaves the API as an RFC 9457 `ProblemDetails` body ([0002](0002-backend-internal-style.md)), and so do 401, 403 and unhandled 500s. The generated client ([0003](0003-repository-and-frontend-layout.md)) types a call's `error` from the error responses in the OpenAPI document. Minimal API endpoints only list the error responses someone declares by hand, so without help most operations would have an untyped `error`, and the shape the frontend relies on would be undocumented.

## Decision

**Every operation documents `ProblemDetails` as its default response.** A document transformer (`ProblemResponsesTransformer`), registered on both the staff and the candidate document, does two things:

- adds the `ProblemDetails` schema to the document's components, even when no endpoint references it yet;
- adds a `default` response with content type `application/problem+json` and that schema to every operation that doesn't declare one.

Endpoints still declare their success responses, and may declare specific error status codes when the frontend needs to tell them apart. Anything undeclared falls under `default`.

## Consequences

- In the generated client, `error` is always typed as `ProblemDetails`, never `unknown`, with no per-endpoint boilerplate.
- The documents state the error contract once, matching what the API actually does.
- `default` doesn't say which status codes an endpoint can return. Frontend code that branches on a specific failure checks `status` or the `code` extension, and the endpoint should declare that code explicitly.
- A new endpoint can't forget to document its error shape.

## Status

Accepted
