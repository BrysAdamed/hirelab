# 0017. Azure provisioning and rollout

## Context

[0011](0011-iac-and-ci-cd.md) has the AppHost own the infrastructure, `azd` deploy it, and a GitHub identity scoped to the resource group run the deploy workflow. Declaring the resources (HLB-32) showed three things the earlier ADRs don't settle:

- The Bicep that Aspire and `azd` generate deploys at subscription scope and creates the resource group. An identity scoped to the resource group can't run it.
- PostgreSQL rights below "Entra administrator" are SQL grants, not Azure resources. Neither Aspire nor Bicep can give the API "data rights only" ([0004](0004-database-and-ef-core.md)).
- Aspire can express the Log Analytics daily cap, so it doesn't need hand-written Bicep.

## Decision

**Provisioning is a manual step with subscription rights.** A person with Owner rights on the subscription runs it at go-live and again whenever the AppHost's Azure resources change:

1. Create the resource group in Sweden Central.
2. Deploy `infra/main.bicep` into it. It holds the budget, which needs the alert email and the first budget month as parameters, and the GitHub deployment identity with its federated credential for the `production` environment and Contributor on the resource group.
3. Run `azd up` with `AZURE_LOCATION=swedencentral` and `AZURE_RESOURCE_GROUP` set to that group. This provisions everything the AppHost declares and creates the container app and the migration job.
4. Set the GitHub `production` environment variables `AZURE_CLIENT_ID` (output of step 2), `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` and `AZURE_RESOURCE_GROUP`.

**The deploy workflow only rolls out images.** It signs in through OIDC, pushes images tagged with the commit SHA to the registry in the group, updates and runs the migration job, updates the API container app to the new image and smoke-checks `/health`. It uses `az containerapp` because that needs nothing outside the resource group. A change to an app's configuration (a new reference or environment variable) needs the manual `azd deploy`.

**Settings Aspire can express live in the AppHost**, including the Log Analytics daily cap. `infra/` holds only the budget and the deployment identity.

**PostgreSQL rights.**
- The migration job's identity (`migrations-identity`) is the only Entra administrator. Aspire's default of making every app that references the server an administrator is switched off.
- The API runs as `api-identity` and gets its rights from SQL: in Azure, the migration job creates the API's Entra principal (`pgaadauth_create_principal_with_oid`) and grants it usage and DML on the module schemas, with default privileges for new tables. The job receives the API identity's name and object id as `Migrations:ApiPrincipalName` and `Migrations:ApiPrincipalId`.

**PostgreSQL version.** 17, both in Azure and in the local container, so migrations meet the same server major version everywhere.

## Consequences

- No credential with subscription rights exists in GitHub. In exchange, infrastructure changes need a person to run `azd`.
- `infra/` and the AppHost are deployed separately; the budget and identity exist before the first `azd up`.
- Until the migration job grants the API its rights (open for go-live), the API can't read the database in Azure.
- The local PostgreSQL data volume is named after the major version (`hirelab-postgres-17-data`), so a version change starts from an empty local database rather than failing on incompatible files.

## Status

Accepted
