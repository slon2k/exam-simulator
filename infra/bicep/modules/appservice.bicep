@description('Name of the App Service Plan.')
param planName string

@description('Name of the Web App.')
param appName string

param location string

@description('App Service Plan SKU name, e.g. S1, B1.')
param skuName string = 'B1'

@description('App Service Plan SKU tier, e.g. Standard, Basic.')
param skuTier string = 'Basic'

@description('Name of the Key Vault holding the connection string secret.')
param keyVaultName string

@description('Name of the Key Vault secret containing the database connection string.')
param connectionStringSecretName string = 'sql-connection-string'

@description('Value for ASPNETCORE_ENVIRONMENT app setting.')
param aspNetCoreEnvironment string = 'Production'

resource plan 'Microsoft.Web/serverFarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: aspNetCoreEnvironment
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/${connectionStringSecretName}/)'
          type: 'SQLAzure'
        }
      ]
    }
  }
}

output principalId string = app.identity.principalId
output hostname string = app.properties.defaultHostName
