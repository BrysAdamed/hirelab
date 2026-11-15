# 0010. Hosting and environments

## Context

HireLab is one API container ([0001](0001-modular-monolith.md)) with one replica ([0006](0006-real-time-channel.md)). It serves two SPAs that must be same-site with the API for cookie auth ([0007](0007-staff-authentication.md), [0008](0008-candidate-access.md)), and it uses PostgreSQL ([0004](0004-database-and-ef-core.md)) and Blob storage ([0005](0005-attachment-storage.md)). It must run in the EU on a personal subscription. Development is expected to be quick, so paying for Azure before there is something to use buys little.

## Decision

**No Azure until MVP 1 is done.** Until then everything runs locally through Aspire. CI validates the infrastructure code on every PR ([0011](0011-iac-and-ci-cd.md)). The first deployment is the go-live at the end of MVP 1.

**Region.** Sweden Central. Before the first deployment we confirm that every service we need is available there; if one isn't, we pick another EU region.

**Environments.** Local (Aspire) plus one Azure environment, production. No staging; pull requests are validated in CI only. The infrastructure code is parameterised, so a staging environment could be added later.

**Compute.** Azure Container Apps, Consumption plan.

| Container app | Replicas | Notes |
|---|---|---|
| API | Min 0, max 1 | Scale-to-zero |
| Migration worker | n/a | A Container Apps Job, run by the pipeline before each rollout |

- The first request after idling waits for a cold start of about 5–15 seconds. A small chiselled image and ReadyToRun keep it short.
- The interviewer opens HireLab before the candidate, so the candidate rarely meets a cold start.

**SPAs are served by the API container** from one origin:

| Path | Serves |
|---|---|
| `/` | The `staff` app |
| `/s/` | The `candidate` app, including the candidate link `/s/{token}` |
| `/api/staff`, `/api/candidate` | API route groups |
| `/hubs/staff`, `/hubs/candidate` | SignalR hubs |

- The candidate cookie is scoped to `/api/candidate` and `/hubs/candidate`.
- Hashed static assets get long cache headers; `index.html` is served with `no-cache`.
- No CORS.

**Domain.** The default `*.azurecontainerapps.io` address. A custom domain (with a free managed certificate) is an optional parameter.

**Data Protection keys.** Persisted to a private Blob container and encrypted with a key in Azure Key Vault (Standard tier), both reached through managed identity. Cookies and antiforgery tokens therefore survive restarts and deployments. The same Key Vault holds future secrets, such as the LLM API key for HLB-9.

**Health endpoints.** Liveness (`/alive`) and readiness (`/health`, which checks the database) are exposed in every environment as Container Apps probes. They return only a status, never details.

**Cost.** Expected about €20–25 a month, mostly PostgreSQL. An Azure budget of €40 a month alerts at 50%, 80% and 100%. The alert email is a deployment parameter and is never committed.

## Consequences

- No Azure cost until MVP 1 is done. In exchange, infrastructure surprises (capacity, Entra auth to PostgreSQL, Data Protection, cold start) show up only at go-live; the go-live ticket plans time for them.
- One container serves everything, so a cold start delays the first page as well as the first API call.
- Scale-to-zero makes idle time nearly free. The first visitor of the day pays for it in seconds.
- Without staging, the first time a change meets Azure is in production. Forward-only migrations and image rollback (0011) limit the damage.

## Status

Accepted
