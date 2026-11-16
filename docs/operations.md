# Operations

How to run HireLab in Azure once it is deployed. Provisioning itself is in [ADR 0017](adr/0017-azure-provisioning-and-rollout.md).

## First admin

There is no admin in a new environment. The migration job creates one invite for them ([ADR 0022](adr/0022-first-admin-bootstrap.md)):

1. `azd up` asks for the `admin-email` parameter: the email the admin will log in with. It is stored in the `azd` environment, never in the repo.
2. The first run of the migration job (from `azd up` or the deploy workflow) writes a warning with a one-time link: `No admin exists yet. Open this one-time admin invite link before <time> ...`. Find it in the job execution's console logs, or in Application Insights by searching traces for `AdminInviteLink`.
3. Open the link within 24 hours and set the admin's password. The admin is then logged in.

This log line is the only one allowed to carry a link. Until the link is redeemed, anyone who can read those logs can claim the admin account, so redeem it straight away.

Later runs leave a pending link alone and log only when it expires. After it expires, the next run issues a new one.

## A new admin link

If the link was lost or expired before anyone redeemed it, start one execution of the job with the `--issue-admin-link` argument:

```bash
az containerapp job start --name migrations --resource-group <group> --args "--issue-admin-link"
```

It revokes every open admin invite and writes a new link to that execution's log. If an admin already exists, the run fails and issues nothing.

Locally, against a database without an admin: `dotnet run --project src/HireLab.MigrationService -- --issue-admin-link`, with `DOTNET_ENVIRONMENT`, `ConnectionStrings__hirelab`, `HireLab__PublicBaseUrl` and `AdminBootstrap__Email` set.

## Settings of the migration job

| Setting | Required | Set by |
|---|---|---|
| `HireLab:PublicBaseUrl` | Outside Development | The AppHost: the API's HTTPS endpoint. Change it together with a custom domain. |
| `AdminBootstrap:Email` | Outside Development | The `admin-email` parameter |
| `AdminBootstrap:DisplayName` | No (default `Administrator`) | |

The job refuses to start outside Development when a required setting is missing or invalid, and says which one.
