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

// Create a new App Service Plan for this application (S1 Standard tier)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'asp-pofasttype-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'S1'
    tier: 'Standard'
    size: 'S1'
    family: 'S'
    capacity: 1
  }
  properties: {
    reserved: false
  }
}

// Use existing shared Application Insights (as mentioned in requirements)
resource existingAppInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: 'PoSharedApplicationInsights'
  scope: resourceGroup('poshared') // Using the actual resource group name (lowercase)
}

// Create a user-assigned managed identity
resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'pofasttype-identity-${resourceToken}'
  location: location
  tags: tags
}

// Create the App Service for PoFastType
resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: 'pofasttype-${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': 'pofasttype-api' })
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v9.0'
      cors: {
        allowedOrigins: ['*']
        supportCredentials: false
      }
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: existingAppInsights.properties.ConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
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
    netFrameworkVersion: 'v9.0'
    scmType: 'GitHubAction'
    use32BitWorkerProcess: false
    webSocketsEnabled: false
    alwaysOn: true // S1 tier supports AlwaysOn
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
