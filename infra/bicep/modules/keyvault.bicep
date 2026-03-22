@description('Name of the Key Vault. Must be 3–24 alphanumeric characters or hyphens.')
param name string

param location string

@description('Principal ID of the App Service system-assigned managed identity.')
param appServicePrincipalId string

@description('Full SQL connection string to store as a secret.')
@secure()
param sqlConnectionString string

// Key Vault Secrets User built-in role
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e0'
var connectionStringSecretName = 'ConnectionStrings--DefaultConnection'

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource connectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: connectionStringSecretName
  properties: {
    value: sqlConnectionString
  }
}

// Grant the App Service managed identity read access to secrets
resource kvSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, appServicePrincipalId, kvSecretsUserRoleId)
  properties: {
    roleDefinitionId: '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/${kvSecretsUserRoleId}'
    principalId: appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

output vaultUri string = kv.properties.vaultUri
