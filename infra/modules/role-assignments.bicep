@description('Principal ID of the managed identity to assign roles to.')
param principalId string

@description('Resource ID of the Azure OpenAI account (empty to skip).')
param openAIAccountId string = ''

@description('Resource ID of the Document Intelligence account (empty to skip).')
param docIntelligenceAccountId string = ''

@description('Resource ID of the Cosmos DB account (empty to skip).')
param cosmosDbAccountId string = ''

// ── Role Definition IDs ──────────────────────────────────────────────────────
// Cognitive Services OpenAI User – allows calling OpenAI endpoints
var cognitiveServicesOpenAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

// Cognitive Services User – allows calling Document Intelligence endpoints
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'

// Cosmos DB Built-in Data Contributor – read/write data via MongoDB API
var cosmosDbDataContributorRoleId = '00000000-0000-0000-0000-000000000002'

// DocumentDB Account Contributor – manage Cosmos DB accounts
var documentDBAccountContributorRoleId = '5bd9cd88-fe45-4216-938b-f97437e15450'

// ── Azure OpenAI Role Assignment ─────────────────────────────────────────────
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(openAIAccountId)) {
  name: guid(openAIAccountId, principalId, cognitiveServicesOpenAIUserRoleId)
  scope: openAIAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUserRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource openAIAccount 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' existing = if (!empty(openAIAccountId)) {
  name: last(split(openAIAccountId, '/'))
}

// ── Document Intelligence Role Assignment ────────────────────────────────────
resource docIntelligenceRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(docIntelligenceAccountId)) {
  name: guid(docIntelligenceAccountId, principalId, cognitiveServicesUserRoleId)
  scope: docIntelligenceAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource docIntelligenceAccount 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' existing = if (!empty(docIntelligenceAccountId)) {
  name: last(split(docIntelligenceAccountId, '/'))
}

// ── Cosmos DB Role Assignment ────────────────────────────────────────────────
resource cosmosDbRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(cosmosDbAccountId)) {
  name: guid(cosmosDbAccountId, principalId, documentDBAccountContributorRoleId)
  scope: cosmosDbAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', documentDBAccountContributorRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-02-15-preview' existing = if (!empty(cosmosDbAccountId)) {
  name: last(split(cosmosDbAccountId, '/'))
}
