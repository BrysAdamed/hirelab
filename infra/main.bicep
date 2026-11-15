targetScope = 'resourceGroup'

@description('Location of the deployment identity. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('GitHub repository allowed to deploy, as owner/name.')
param githubRepository string

@description('GitHub environment the federated credential is scoped to.')
param githubEnvironment string = 'production'

@description('Email that receives the budget alerts. Pass it at deployment time; never commit it.')
param budgetAlertEmail string

@description('Monthly budget in the billing currency (EUR).')
param budgetAmount int = 40

@description('First day of the first budget month, yyyy-MM-01. Keep the original value on later deployments.')
param budgetStartDate string

var contributorRoleId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'
var budgetAlertThresholds = [
  50
  80
  100
]

resource deploymentIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: 'id-github-deploy'
  location: location
}

resource githubCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2024-11-30' = {
  parent: deploymentIdentity
  name: 'github-${githubEnvironment}'
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${githubRepository}:environment:${githubEnvironment}'
    audiences: [
      'api://AzureADTokenExchange'
    ]
  }
}

resource deploymentRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, deploymentIdentity.id, contributorRoleId)
  properties: {
    principalId: deploymentIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
  }
}

resource budget 'Microsoft.Consumption/budgets@2024-08-01' = {
  name: 'budget-hirelab-monthly'
  properties: {
    category: 'Cost'
    amount: budgetAmount
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: budgetStartDate
    }
    notifications: toObject(
      budgetAlertThresholds,
      threshold => 'actual-${threshold}-percent',
      threshold => {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: threshold
        thresholdType: 'Actual'
        contactEmails: [
          budgetAlertEmail
        ]
      }
    )
  }
}

output deploymentClientId string = deploymentIdentity.properties.clientId
output deploymentPrincipalId string = deploymentIdentity.properties.principalId
