# Operations

How to run HireLab in Azure once it is deployed. Provisioning itself is in [ADR 0017](adr/0017-azure-provisioning-and-rollout.md).

## First admin

There is no admin in a new environment. The migration job creates one invite for them ([ADR 0022](adr/0022-first-admin-bootstrap.md)):

1. `azd up` asks for the `admin-email` parameter: the email the admin will log in with. It is stored in the `azd` environment, never in the repo.
2. The first run of the migration job (from `azd up` or the deploy workflow) writes a warning with a one-time link: `No admin exists yet. Open this one-time admin invite link before <time> ...`. Find it in the job execution's console logs, or in Application Insights by searching traces for `AdminInviteLink`.
3. Open the link within 24 hours and set the admin's password. The admin is then logged in.

The admin's invite and reset links are the only links allowed in logs. Until the link is redeemed, anyone who can read those logs can claim the admin account, so redeem it straight away.

Later runs leave a pending link alone and log only when it expires. After it expires, the next run issues a new one.

## A new admin link

Start one execution of the job with the `--issue-admin-link` argument:

```bash
az containerapp job start --name migrations --resource-group <group> --args "--issue-admin-link"
```

- **No admin yet** (the invite was lost or expired): it revokes every open admin invite and logs a new invite link (`AdminInviteLink`).
- **The admin exists but is locked out** or has lost their password: it logs a one-time reset link (`AdminResetLink`, `/reset#<token>`), valid for 24 hours, and revokes the admin's previous reset link. Opening it sets a new password, clears the lockout and ends the admin's other sessions; the admin then logs in. It never creates a second admin.

Locally, against a database without an admin: `dotnet run --project src/HireLab.MigrationService -- --issue-admin-link`, with `DOTNET_ENVIRONMENT`, `ConnectionStrings__hirelab`, `HireLab__PublicBaseUrl` and `AdminBootstrap__Email` set.

## Settings of the migration job

| Setting | Required | Set by |
|---|---|---|
| `HireLab:PublicBaseUrl` | Outside Development | The AppHost: the API's HTTPS endpoint. Change it together with a custom domain. |
| `AdminBootstrap:Email` | Outside Development | The `admin-email` parameter |
| `AdminBootstrap:DisplayName` | No (default `Administrator`) | |

The job refuses to start outside Development when a required setting is missing or invalid, and says which one.
