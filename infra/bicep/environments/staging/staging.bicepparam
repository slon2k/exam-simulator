using '../../main.bicep'

param appServicePlanName = 'plan-exam-simulator-stg'
param appName = 'app-exam-simulator-stg'
param sqlServerName = 'sql-exam-simulator-stg'
param sqlDatabaseName = 'ExamSimulator'
param keyVaultName = 'kv-exam-sim-stg'
param adminLogin = readEnvironmentVariable('SQL_ADMIN_LOGIN', '')
param adminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
param appServiceSkuName = 'F1'
param appServiceSkuTier = 'Free'
param sqlDatabaseSkuName = 'Basic'
param sqlDatabaseSkuTier = 'Basic'
