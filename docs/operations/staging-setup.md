# Staging Environment — One-Time Setup

Run these steps once before the first deployment. All commands use the Azure CLI (`az`).

## Variables

Set these in your shell session so the commands below can be copied verbatim.

**Bash**
```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RESOURCE_GROUP=rg-exam-simulator-stg
LOCATION=centralus
SP_NAME=sp-exam-simulator-github-stg
```

**PowerShell**
```powershell
$SUBSCRIPTION_ID = az account show --query id -o tsv
$RESOURCE_GROUP  = "rg-exam-simulator-stg"
$LOCATION        = "centralus"
$SP_NAME         = "sp-exam-simulator-github-stg"
```

---

## Step 1 — Create the resource group

**Bash**
```bash
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

**PowerShell**
```powershell
az group create `
  --name $RESOURCE_GROUP `
  --location $LOCATION
```

---

## Step 2 — Create the service principal

**Bash**
```bash
az ad sp create-for-rbac \
  --name $SP_NAME \
  --role Contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

**PowerShell**
```powershell
az ad sp create-for-rbac `
  --name $SP_NAME `
  --role Contributor `
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP `
  --sdk-auth
```

The command prints a JSON block. **Copy the entire output** — you will store it as the `AZURE_CREDENTIALS` GitHub secret in Step 4.

---

## Step 3 — Grant User Access Administrator to the service principal

The Bicep deployment assigns a Key Vault role to the app's managed identity. The service principal needs permission to make role assignments inside the resource group.

**Bash**
```bash
SP_CLIENT_ID=$(az ad sp list --display-name $SP_NAME --query "[0].appId" -o tsv)

az role assignment create \
  --assignee $SP_CLIENT_ID \
  --role "User Access Administrator" \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP
```

**PowerShell**
```powershell
$SP_CLIENT_ID = az ad sp list --display-name $SP_NAME --query "[0].appId" -o tsv

az role assignment create `
  --assignee $SP_CLIENT_ID `
  --role "User Access Administrator" `
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP
```

---

## Step 4 — Create a GitHub Environment and add secrets

The workflow uses [GitHub Environments](https://docs.github.com/en/actions/deployment/targeting-different-deployment-environments/using-environments-for-deployment) so that staging and production secrets are kept separate.

1. Go to the repository on GitHub: **Settings → Environments → New environment**
2. Name it exactly **`staging`** and click **Configure environment**
3. Under **Environment secrets**, add the following three secrets:

| Secret name | Value |
|---|---|
| `AZURE_CREDENTIALS` | The full JSON output from Step 2 |
| `SQL_ADMIN_LOGIN` | A username for the SQL Server admin (e.g. `sqladmin`) |
| `SQL_ADMIN_PASSWORD` | A strong password — minimum 12 characters, must include uppercase, lowercase, digit, and symbol |

> Store the SQL credentials in a password manager. You will need them if you ever connect to the database directly.

When a production environment is needed, follow the same steps with environment name **`production`** and production-specific credentials.

---

## Done

The repository is now ready to run the [Deploy to Staging](deploy-staging.md) workflow.
