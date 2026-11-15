# 0011. Infrastructure as code and CI/CD

## Context

The Aspire AppHost already describes the system for local development ([0003](0003-repository-and-frontend-layout.md)). Describing the same resources again in hand-written Bicep would mean keeping two models in sync. The repo is public and lives on GitHub, so no Azure secret may be stored there. Azure deployment waits until MVP 1 ([0010](0010-hosting-and-environments.md)), but the pipeline must not rot in the meantime.

## Decision

**Aspire owns the infrastructure definition.**
- The AppHost declares the Azure resources: `AddAzureContainerAppEnvironment`, `AddAzurePostgresFlexibleServer`, `AddAzureStorage`, `AddAzureKeyVault`.
- Settings from earlier ADRs are applied through `ConfigureInfrastructure`: Entra-only PostgreSQL, storage without shared keys, soft delete off for attachments, replica limits.
- `azd` generates the Bicep and deploys it.
- What Aspire can't express goes in a small hand-written Bicep file under `infra/`: the budget, the Log Analytics daily cap, and the GitHub deployment identity with its federated credential.
- If Aspire can't express a required setting for a resource, that resource moves to hand-written Bicep.

**GitHub → Azure** uses OIDC federated credentials on a user-assigned identity. The identity is scoped to the GitHub `production` environment and to the HireLab resource group only. GitHub stores no client secret.

**Registry.** Azure Container Registry, Basic tier, created by `azd` with the rest of the resource group.

**Images** are tagged with the git commit SHA. The API runs in single-revision mode.

**Pull request workflow:**
1. Build and test the backend and both SPAs.
2. Lint, including the frontend import rules.
3. Drift checks for the generated API client and the hub types.
4. Infrastructure validation: generate the Bicep and run `bicep build` / lint on it.

**Deploy workflow**, run under the GitHub `production` environment:
1. Build the image and push it to ACR.
2. Run the migration job and wait for it to succeed.
3. Deploy the new API revision.
4. Smoke-check `/health`, and fail visibly if it doesn't pass.

**Triggers.** Until MVP 1 is done, the deploy workflow runs only on `workflow_dispatch` and isn't run. At go-live it switches to every push to `main`.

**Rollback** means redeploying the previous commit's image. That's safe because migrations are forward-only and use expand/contract ([0004](0004-database-and-ef-core.md)).

## Consequences

- One model describes the system locally and in Azure. The generated Bicep is still reviewable.
- We depend on Aspire's Azure integrations and `azd` keeping pace with the settings we need. The fallback to hand-written Bicep keeps that risk bounded.
- Validating the infrastructure on every PR keeps it compiling while nothing is deployed. Only a real deployment proves it works, and that happens at go-live.
- No Azure credential exists in GitHub. A leaked workflow can act only within the resource group.
- Rolling back can't undo a migration. Expand/contract makes that acceptable.

## Status

Accepted
