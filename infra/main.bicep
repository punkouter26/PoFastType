targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Principal ID of the user or app to assign application roles')
param principalId string

@description('Principal type of user or app')
param principalType string = 'User'

// Tags that should be applied to all resources.
var tags = {
  'azd-env-name': environmentName
}

// Organize resources in a resource group (name derived from solution)
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'PoFastType'
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    principalId: principalId
    principalType: principalType
    environmentName: environmentName
  }
}

// Deploy budget with alert
module budget 'budget.bicep' = {
  scope: rg
  name: 'budget'
  params: {
    budgetName: 'PoFastType-MonthlyBudget'
    amount: 5
    actionGroupId: resources.outputs.BUDGET_ACTION_GROUP_ID
  }
  dependsOn: [
    resources
  ]
}

output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output WEBSITE_URL string = resources.outputs.WEBSITE_URL
output API_BASE_URL string = resources.outputs.API_BASE_URL
output RESOURCE_GROUP_ID string = rg.id
output BUDGET_ID string = budget.outputs.budgetId
