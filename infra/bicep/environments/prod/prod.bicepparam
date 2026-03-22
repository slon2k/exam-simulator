using '../../main.bicep'

param appServicePlanName = 'plan-exam-simulator-prd'
param appName = 'app-exam-simulator-prd'
param sqlServerName = 'sql-exam-simulator-prd'
param sqlDatabaseName = 'ExamSimulator'
param keyVaultName = 'kv-exam-sim-prd'
param adminLogin = readEnvironmentVariable('SQL_ADMIN_LOGIN', '')
param adminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
param appServiceSkuName = 'B1'
param appServiceSkuTier = 'Basic'
param sqlDatabaseSkuName = 'Basic'
param sqlDatabaseSkuTier = 'Basic'
