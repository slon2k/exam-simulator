@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Name of the App Service Plan.')
param appServicePlanName string

@description('Name of the Web App.')
param appName string

@description('Name of the Azure SQL logical server.')
param sqlServerName string

@description('Name of the SQL database.')
param sqlDatabaseName string

@description('Name of the Key Vault.')
param keyVaultName string

@description('SQL Server administrator login name.')
param adminLogin string

@description('SQL Server administrator login password.')
@secure()
param adminPassword string

@description('App Service Plan SKU name.')
param appServiceSkuName string = 'S1'

@description('App Service Plan SKU tier.')
param appServiceSkuTier string = 'Standard'

@description('SQL database SKU name.')
param sqlDatabaseSkuName string = 'S0'

@description('SQL database SKU tier.')
param sqlDatabaseSkuTier string = 'Standard'

module appSvc 'modules/appservice.bicep' = {
  params: {
    planName: appServicePlanName
    appName: appName
    location: location
    skuName: appServiceSkuName
    skuTier: appServiceSkuTier
    keyVaultName: keyVaultName
  }
}

module sql 'modules/sql.bicep' = {
  params: {
    serverName: sqlServerName
    databaseName: sqlDatabaseName
    location: location
    adminLogin: adminLogin
    adminPassword: adminPassword
    databaseSkuName: sqlDatabaseSkuName
    databaseSkuTier: sqlDatabaseSkuTier
  }
}

// Key Vault depends on both modules: needs the App Service principal ID and the SQL connection string
module kv 'modules/keyvault.bicep' = {
  params: {
    name: keyVaultName
    location: location
    appServicePrincipalId: appSvc.outputs.principalId
    sqlConnectionString: sql.outputs.connectionString
  }
}

output appUrl string = 'https://${appSvc.outputs.hostname}'
output sqlServerFqdn string = sql.outputs.serverFqdn
output keyVaultUri string = kv.outputs.vaultUri
