// Main Bicep template for PoFastType Azure infrastructure
// Following Infrastructure as Code best practices with secure configuration

@description('The application name used for naming resources')
param appName string

@description('Environment (prod, staging, dev)')
@allowed(['prod', 'staging', 'dev'])
param environment string = 'prod'

@description('Azure region for resources')
param location string = resourceGroup().location

@description('App Service Plan SKU')
@allowed(['F1', 'B1', 'B2', 'S1', 'S2', 'P1v2', 'P2v2', 'P3v2'])
param appServicePlanSku string = 'B1'

@description('Storage Account SKU for Table Storage')
@allowed(['Standard_LRS', 'Standard_GRS', 'Standard_ZRS'])
param storageAccountSku string = 'Standard_LRS'

// Variables - Using GoF Strategy Pattern for naming convention
var namingStrategy = {
  appServicePlan: 'asp-${appName}-${environment}'
  appService: 'app-${appName}-${environment}'
  storageAccount: replace('st${appName}${environment}', '-', '')  // Storage accounts don't allow hyphens
  tableStorageName: 'PoFastTypeGameResults'
}

var commonTags = {
  Application: 'PoFastType'
  Environment: environment
  ManagedBy: 'Bicep'
}

// App Service Plan - Following Azure best practices for scalability
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: namingStrategy.appServicePlan
  location: location
  tags: commonTags
  sku: {
    name: appServicePlanSku
    tier: appServicePlanSku == 'F1' ? 'Free' : (startsWith(appServicePlanSku, 'B') ? 'Basic' : (startsWith(appServicePlanSku, 'S') ? 'Standard' : 'PremiumV2'))
  }
  properties: {
    reserved: false  // Windows-based App Service Plan for .NET 9.x
    perSiteScaling: false
    maximumElasticWorkerCount: 1
    targetWorkerCount: 1
  }
}

// Storage Account for Azure Table Storage
// Following security best practices with HTTPS only and minimum TLS 1.2
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: namingStrategy.storageAccount
  location: location
  tags: commonTags
  kind: 'StorageV2'
  sku: {
    name: storageAccountSku
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true  // Required for Table Storage
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    encryption: {
      keySource: 'Microsoft.Storage'
      services: {
        table: {
          enabled: true
          keyType: 'Account'
        }
        blob: {
          enabled: true
          keyType: 'Account'
        }
      }
    }
  }
}

// App Service (Web App) - Implementing security best practices
resource appService 'Microsoft.Web/sites@2024-04-01' = {
  name: namingStrategy.appService
  location: location
  tags: commonTags
  kind: 'app'
  identity: {
    type: 'SystemAssigned'  // Using Managed Identity for secure Azure service access
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      // .NET 9.x runtime configuration
      netFrameworkVersion: 'v8.0'  // Latest supported framework version
      alwaysOn: appServicePlanSku != 'F1'  // Always On not available on Free tier
      ftpsState: 'Disabled'  // Security: Disable FTP
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      use32BitWorkerProcess: false
      webSocketsEnabled: false
      httpLoggingEnabled: true
      detailedErrorLoggingEnabled: true
      requestTracingEnabled: true
      healthCheckPath: '/api/diag/health'  // Health check endpoint for the app
      
      // Application settings - Following 12-factor app principles
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : (environment == 'staging' ? 'Staging' : 'Development')
        }
        {
          name: 'AzureTableStorage__ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'AzureTableStorage__TableName'
          value: environment == 'staging' ? '${namingStrategy.tableStorageName}Staging' : namingStrategy.tableStorageName
        }
        {
          name: 'Logging__LogLevel__Default'
          value: environment == 'prod' ? 'Information' : 'Debug'
        }
        {
          name: 'Logging__LogLevel__Microsoft.AspNetCore'
          value: 'Warning'
        }
        {
          name: 'WEBSITE_ENABLE_SYNC_UPDATE_SITE'
          value: 'true'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
      
      // IP Security restrictions for production environment
      ipSecurityRestrictions: environment == 'prod' ? [
        {
          action: 'Allow'
          priority: 100
          name: 'AllowAll'
          description: 'Allow all traffic'
          ipAddress: '0.0.0.0/0'
        }
      ] : []
    }
  }
}

// Create staging slot for production environment
resource stagingSlot 'Microsoft.Web/sites/slots@2024-04-01' = if (environment == 'prod') {
  parent: appService
  name: 'staging'
  location: location
  tags: commonTags
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      alwaysOn: appServicePlanSku != 'F1'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Staging'
        }
        {
          name: 'AzureTableStorage__ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'AzureTableStorage__TableName'
          value: '${namingStrategy.tableStorageName}Staging'
        }
        {
          name: 'Logging__LogLevel__Default'
          value: 'Debug'
        }
      ]
    }
  }
}

// Outputs for use in deployment pipeline
@description('The name of the created App Service')
output webAppName string = appService.name

@description('The default hostname of the App Service')
output webAppUrl string = 'https://${appService.properties.defaultHostName}'

@description('The name of the storage account')
output storageAccountName string = storageAccount.name

@description('The resource group name')
output resourceGroupName string = resourceGroup().name

@description('The staging slot URL (if created)')
output stagingSlotUrl string = environment == 'prod' ? 'https://${appService.name}-staging.azurewebsites.net' : ''
