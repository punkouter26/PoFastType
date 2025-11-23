targetScope = 'resourceGroup'

@description('Name of the budget')
param budgetName string = 'PoFastType-MonthlyBudget'

@description('The total amount of cost or usage to track with the budget')
param amount int = 5

@description('The time covered by a budget. Tracking of the amount will be reset based on the time grain.')
@allowed([
  'Monthly'
  'Quarterly'
  'Annually'
  'BillingMonth'
  'BillingQuarter'
  'BillingAnnual'
])
param timeGrain string = 'Monthly'

@description('The start date for the budget')
param startDate string = utcNow('yyyy-MM-01')

@description('The end date for the budget (5 years from now)')
param endDate string = dateTimeAdd(utcNow(), 'P5Y', 'yyyy-MM-dd')

@description('Action Group Resource ID for budget alerts')
param actionGroupId string

@description('Threshold percentage for alert (default: 80%)')
param thresholdPercentage int = 80

resource budget 'Microsoft.Consumption/budgets@2023-11-01' = {
  name: budgetName
  properties: {
    timePeriod: {
      startDate: startDate
      endDate: endDate
    }
    timeGrain: timeGrain
    amount: amount
    category: 'Cost'
    notifications: {
      'Actual_GreaterThan_${thresholdPercentage}_Percent': {
        enabled: true
        operator: 'GreaterThan'
        threshold: thresholdPercentage
        contactEmails: [
          'punkouter26@gmail.com'
        ]
        contactGroups: [
          actionGroupId
        ]
        thresholdType: 'Actual'
      }
    }
  }
}

output budgetId string = budget.id
output budgetName string = budget.name
