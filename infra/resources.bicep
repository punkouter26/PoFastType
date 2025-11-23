@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

@description('Environment name for resource naming')
param environmentName string

@description('Principal ID of the user or app to assign application roles')
param principalId string

@description('Principal type of user or app')
param principalType string = 'User'

// Generate a unique token for resource naming
var resourceToken = uniqueString(subscription().id, location, environmentName)

// Use existing shared App Service Plan from PoShared resource group
// Note: Reference to existing resource in different resource group
resource existingAppServicePlan 'Microsoft.Web/serverfarms@2022-03-01' existing = {
  name: 'PoShared4'
  scope: resourceGroup('PoShared')
}

// Create Log Analytics Workspace in the same resource group
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'PoFastType-logs'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: 1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Create Application Insights in the same resource group
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'PoFastType-ai'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Create Azure Storage Account for Table Storage
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'pofasttype${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        table: {
          enabled: true
        }
        blob: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// Create Table Service
resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

// Create the game results table
resource gameResultsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-01-01' = {
  parent: tableService
  name: 'PoFastTypeGameResults'
}

// Create Key Vault for secrets
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'pofasttype-kv'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    publicNetworkAccess: 'Enabled'
  }
}

// Create Action Group for budget alerts
resource budgetAlertActionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'PoFastType-budget-alerts'
  location: 'global'
  tags: tags
  properties: {
    groupShortName: 'BudgetAlert'
    enabled: true
    emailReceivers: [
      {
        name: 'OwnerEmail'
        emailAddress: 'punkouter26@gmail.com'
        useCommonAlertSchema: true
      }
    ]
  }
}

// Create a user-assigned managed identity
resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'PoFastType-identity'
  location: location
  tags: tags
}

// Create the App Service for PoFastType
resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: 'PoFastType'
  location: location
  tags: union(tags, { 'azd-service-name': 'pofasttype-api' })
  kind: 'app'
  properties: {
    serverFarmId: existingAppServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      cors: {
        allowedOrigins: ['*']
        supportCredentials: false
      }
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'AzureTableStorage__ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'AzureTableStorage__TableName'
          value: 'PoFastTypeGameResults'
        }
        {
          name: 'AzureKeyVault__VaultUri'
          value: keyVault.properties.vaultUri
        }
        // Enable Application Insights Snapshot Debugger
        {
          name: 'APPINSIGHTS_SNAPSHOTFEATURE_VERSION'
          value: '1.0.0'
        }
        {
          name: 'SnapshotDebugger__IsEnabled'
          value: 'true'
        }
        {
          name: 'SnapshotDebugger__UploadAllSnapshots'
          value: 'true'
        }
        // Enable Application Insights Profiler
        {
          name: 'APPINSIGHTS_PROFILERFEATURE_VERSION'
          value: '1.0.0'
        }
        {
          name: 'ApplicationInsightsProfiler__IsEnabled'
          value: 'true'
        }
        {
          name: 'DiagnosticServices__EnableProfiler'
          value: 'true'
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
}

// Configure deployment settings
resource webAppConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: webApp
  name: 'web'
  properties: {
    netFrameworkVersion: 'v10.0'
    scmType: 'GitHubAction'
    use32BitWorkerProcess: true
    webSocketsEnabled: false
    alwaysOn: false // F1 tier doesn't support AlwaysOn
    managedPipelineMode: 'Integrated'
    virtualApplications: [
      {
        virtualPath: '/'
        physicalPath: 'site\\wwwroot'
        preloadEnabled: false
      }
    ]
    loadBalancing: 'LeastRequests'
    experiments: {
      rampUpRules: []
    }
    autoHealEnabled: false
    vnetRouteAllEnabled: false
    vnetPrivatePortsCount: 0
    localMySqlEnabled: false
    ipSecurityRestrictions: [
      {
        ipAddress: 'Any'
        action: 'Allow'
        priority: 2147483647
        name: 'Allow all'
        description: 'Allow all access'
      }
    ]
    scmIpSecurityRestrictions: [
      {
        ipAddress: 'Any'
        action: 'Allow'
        priority: 2147483647
        name: 'Allow all'
        description: 'Allow all access'
      }
    ]
    scmIpSecurityRestrictionsUseMain: false
    http20Enabled: false
    minTlsVersion: '1.2'
    scmMinTlsVersion: '1.2'
    ftpsState: 'FtpsOnly'
    preWarmedInstanceCount: 0
    functionAppScaleLimit: 0
    functionsRuntimeScaleMonitoringEnabled: false
    minimumElasticInstanceCount: 0
    azureStorageAccounts: {}
  }
}

output WEBSITE_URL string = 'https://${webApp.properties.defaultHostName}'
output API_BASE_URL string = 'https://${webApp.properties.defaultHostName}/api'
output AZURE_RESOURCE_GROUP_NAME string = resourceGroup().name
output AZURE_WEB_APP_NAME string = webApp.name
output AZURE_STORAGE_ACCOUNT_NAME string = storageAccount.name
output AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING string = appInsights.properties.ConnectionString
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logAnalyticsWorkspace.id
output AZURE_KEY_VAULT_NAME string = keyVault.name
output AZURE_KEY_VAULT_URI string = keyVault.properties.vaultUri
output BUDGET_ACTION_GROUP_ID string = budgetAlertActionGroup.id
