@description('Name of the Azure SQL logical server.')
param serverName string

@description('Name of the database.')
param databaseName string

param location string

@description('SQL Server administrator login name.')
param adminLogin string

@description('SQL Server administrator login password.')
@secure()
param adminPassword string

@description('Database SKU name, e.g. Basic, S0, S1.')
param databaseSkuName string = 'Basic'

@description('Database SKU tier.')
param databaseSkuTier string = 'Basic'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services to connect (required for App Service outbound traffic)
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: databaseSkuName
    tier: databaseSkuTier
  }
}

output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName

@secure()
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};User Id=${adminLogin};Password=${adminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
