# Deploying to Staging

## Overview

Staging deployments are triggered manually via GitHub Actions. The workflow provisions (or updates) Azure infrastructure using Bicep and then deploys the application to the App Service.

**Workflow file:** `.github/workflows/deploy-staging.yml`

## Pre-requisites

The following GitHub Actions secrets must be configured in the repository before running the workflow:

| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Service principal JSON with Contributor access to the staging resource group |
| `SQL_ADMIN_LOGIN` | SQL Server administrator login name |
| `SQL_ADMIN_PASSWORD` | SQL Server administrator login password |

### Creating the service principal

```bash
az ad sp create-for-rbac \
  --name "sp-exam-simulator-github-stg" \
  --role Contributor \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/rg-exam-simulator-stg \
  --sdk-auth
```

Copy the resulting JSON and store it as the `AZURE_CREDENTIALS` secret.

The service principal also needs `User Access Administrator` on the resource group to assign the Key Vault role to the app's managed identity:

```bash
az role assignment create \
  --assignee <SP_CLIENT_ID> \
  --role "User Access Administrator" \
  --scope /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/rg-exam-simulator-stg
```

### Creating the resource group

The resource group must exist before the first deployment:

```bash
az group create --name rg-exam-simulator-stg --location eastus
```

## Triggering a deployment

1. Go to the **Actions** tab in the GitHub repository.
2. Select **Deploy to Staging** from the workflow list.
3. Click **Run workflow** → **Run workflow**.

## What the workflow does

1. **Provision infrastructure** — runs `az deployment group` with `infra/bicep/main.bicep` and `staging.bicepparam`. This is idempotent: re-running it on existing resources is safe.
2. **Build and deploy application** — runs `dotnet publish` in Release configuration, then deploys the output package to `app-exam-simulator-stg` via the Azure Web Apps Deploy action.

## Verifying the deployment

After the workflow completes, the application is available at:

```
https://app-exam-simulator-stg.azurewebsites.net
```

Check the App Service logs in the Azure Portal under **Diagnose and solve problems → Application logs** if the app does not start.
