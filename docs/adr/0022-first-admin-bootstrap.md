# 0022. First admin bootstrap

## Context

[0007](0007-staff-authentication.md) has the migration job create the first admin's invite in Azure and write the link to its log, and lets a manual run issue a new link. [0020](0020-teams-and-user-lifecycle.md) allows at most one admin, and [0021](0021-one-time-links.md) gives the invite its token, states and redemption page. Building it (HLB-39) left open how the job knows the admin's email and the app's address, what a repeated run does, and how a person starts the manual run of a Container Apps Job.

## Decision

**A bootstrap step in the migration job.** `IModuleMigrator.BootstrapAsync` runs after every module is migrated and, in Development, seeded. It runs in every environment and is idempotent. The Identity module uses it for the admin:

| State | What the run does |
|---|---|
| An admin exists | Nothing. |
| No admin, an admin invite is pending | Nothing. It logs when the pending invite expires. |
| No admin, no pending admin invite | Revokes any open admin invite, issues an admin invite (24 hours) and logs its link. |
| `--issue-admin-link`, no admin | Revokes every open admin invite, whatever its email, then issues a new one and logs its link. |
| `--issue-admin-link`, an admin exists | Fails without issuing anything. An invite can never create a second admin. |

A repeated deployment doesn't replace a link someone may be opening. In Development the seed creates the admin first, so the step does nothing.

**The link is the one log line allowed to carry a secret.** It is a single warning from the `HireLab.Modules.Identity.Bootstrap.AdminBootstrap` category with the link in the `AdminInviteLink` field. It reaches the job's console log and Application Insights. Nothing else may log a link, token or email ([0012](0012-observability.md)).

**Settings.**

| Setting | Meaning | Set by |
|---|---|---|
| `HireLab:PublicBaseUrl` | The origin of the staff app, such as `https://hirelab.example.com`, with no path. The link is built on it. | The AppHost, from the API's HTTPS endpoint (`https://api.<environment domain>`) |
| `AdminBootstrap:Email` | The email the first admin logs in with. | The AppHost parameter `admin-email`, which `azd up` asks for |
| `AdminBootstrap:DisplayName` | The admin's display name. Defaults to `Administrator`. | Optional |
| `AdminBootstrap:IssueNewLink` | Issue a new link in this run. | The `--issue-admin-link` argument |

Outside Development the job refuses to start without a valid `HireLab:PublicBaseUrl` and `AdminBootstrap:Email`, so a wrong configuration shows at the first deployment, not on the day the link is needed. In Development neither is required.

**The manual run is an argument.** `--issue-admin-link` is passed as the container's arguments for one execution of the job: `az containerapp job start --name migrations --resource-group <group> --args "--issue-admin-link"`. The job's image starts the app through its entrypoint and has no arguments of its own, so the override should change nothing else; the go-live checks this, since nothing runs in Azure before then ([0010](0010-hosting-and-environments.md)). An environment variable was rejected: setting it on the job would stay for every later deployment and replace the pending link each time. Locally the same argument works with `dotnet run --project src/HireLab.MigrationService -- --issue-admin-link`.

## Consequences

- Anyone who can read the job's logs or Application Insights during those 24 hours can claim the admin account ([0007](0007-staff-authentication.md)). Redeeming it closes that window.
- A custom domain needs `HireLab:PublicBaseUrl` changed with it, or the printed link points at the default address (which still works).
- An admin who exists but has lost their password can't recover through the invite. The manual run then needs to issue a reset link for the admin instead; until that exists it fails clearly.
- Another module can add its own bootstrap step without touching the job.

## Status

Accepted
