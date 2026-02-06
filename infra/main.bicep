targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that is used to generate a short unique hash for resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('SKU for the App Service Plan. Default is P0v3.')
param appServicePlanSku string = 'P0v3'

// ── Azure OpenAI (optional) ──────────────────────────────────────────────────
@description('Set to true to deploy a new Azure OpenAI resource. Set to false to use an existing one.')
param deployAzureOpenAI bool = true

@description('Endpoint of an existing Azure OpenAI resource (required when deployAzureOpenAI is false).')
param existingOpenAIEndpoint string = ''

@secure()
@description('API key of an existing Azure OpenAI resource (required when deployAzureOpenAI is false).')
param existingOpenAIApiKey string = ''

@description('Azure OpenAI API version used for real-time calls. Default is 2024-05-01-preview.')
param openAIApiVersion string = '2024-05-01-preview'

@description('Batch endpoint of an existing Azure OpenAI resource (required when deployAzureOpenAI is false). If empty, falls back to the main endpoint.')
param existingOpenAIBatchEndpoint string = ''

@secure()
@description('Batch API key of an existing Azure OpenAI resource (required when deployAzureOpenAI is false). If empty, falls back to the main API key.')
param existingOpenAIBatchApiKey string = ''

// ── Document Intelligence (optional) ─────────────────────────────────────────
@description('Set to true to deploy a new Document Intelligence resource. Set to false to use an existing one.')
param deployDocumentIntelligence bool = true

@description('Endpoint of an existing Document Intelligence resource (required when deployDocumentIntelligence is false).')
param existingDocIntelligenceEndpoint string = ''

@secure()
@description('API key of an existing Document Intelligence resource (required when deployDocumentIntelligence is false).')
param existingDocIntelligenceApiKey string = ''

// ── Variables ────────────────────────────────────────────────────────────────
var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
}

// ── Resource Group ───────────────────────────────────────────────────────────
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

// ── Log Analytics & Application Insights ─────────────────────────────────────
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics'
  scope: rg
  params: {
    name: '${abbrs.logAnalyticsWorkspace}-${resourceToken}'
    location: location
    tags: tags
  }
}

module appInsights 'modules/app-insights.bicep' = {
  name: 'app-insights'
  scope: rg
  params: {
    name: '${abbrs.applicationInsights}-${resourceToken}'
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// ── Cosmos DB (MongoDB API – Serverless) ─────────────────────────────────────
module cosmosDb 'modules/cosmos-db.bicep' = {
  name: 'cosmos-db'
  scope: rg
  params: {
    name: '${abbrs.cosmosDBAccount}-${resourceToken}'
    location: location
    tags: tags
  }
}

// ── Storage Account (for Function App) ───────────────────────────────────────
module storageAccount 'modules/storage-account.bicep' = {
  name: 'storage-account'
  scope: rg
  params: {
    name: '${abbrs.storageAccount}${resourceToken}'
    location: location
    tags: tags
  }
}

// ── App Service Plan ─────────────────────────────────────────────────────────
module appServicePlan 'modules/app-service-plan.bicep' = {
  name: 'app-service-plan'
  scope: rg
  params: {
    name: '${abbrs.appServicePlan}-${resourceToken}'
    location: location
    tags: tags
    sku: appServicePlanSku
  }
}

// ── Azure OpenAI (conditional) ───────────────────────────────────────────────
module azureOpenAI 'modules/azure-openai.bicep' = if (deployAzureOpenAI) {
  name: 'azure-openai'
  scope: rg
  params: {
    name: '${abbrs.openAIAccount}-${resourceToken}'
    location: location
    tags: tags
  }
}

var openAIEndpoint = deployAzureOpenAI ? azureOpenAI.outputs.endpoint : existingOpenAIEndpoint
var openAIApiKey = deployAzureOpenAI ? azureOpenAI.outputs.apiKey : existingOpenAIApiKey
var openAIBatchEndpoint = deployAzureOpenAI ? azureOpenAI.outputs.endpoint : (empty(existingOpenAIBatchEndpoint) ? existingOpenAIEndpoint : existingOpenAIBatchEndpoint)
var openAIBatchApiKey = deployAzureOpenAI ? azureOpenAI.outputs.apiKey : (empty(existingOpenAIBatchApiKey) ? existingOpenAIApiKey : existingOpenAIBatchApiKey)

// ── Document Intelligence (conditional) ──────────────────────────────────────
module documentIntelligence 'modules/document-intelligence.bicep' = if (deployDocumentIntelligence) {
  name: 'document-intelligence'
  scope: rg
  params: {
    name: '${abbrs.documentIntelligence}-${resourceToken}'
    location: location
    tags: tags
  }
}

var docIntelligenceEndpoint = deployDocumentIntelligence ? documentIntelligence.outputs.endpoint : existingDocIntelligenceEndpoint
var docIntelligenceApiKey = deployDocumentIntelligence ? documentIntelligence.outputs.apiKey : existingDocIntelligenceApiKey

// ── Admin Portal (Web App) ───────────────────────────────────────────────────
module adminPortal 'modules/web-app.bicep' = {
  name: 'admin-portal'
  scope: rg
  params: {
    name: '${abbrs.webApp}-portal-${resourceToken}'
    location: location
    tags: tags
    appServicePlanId: appServicePlan.outputs.id
    appInsightsConnectionString: appInsights.outputs.connectionString
    cosmosDbConnectionString: cosmosDb.outputs.connectionString
    openAIEndpoint: openAIEndpoint
    openAIApiKey: openAIApiKey
    openAIApiVersion: openAIApiVersion
    openAIBatchEndpoint: openAIBatchEndpoint
    openAIBatchApiKey: openAIBatchApiKey
    docIntelligenceEndpoint: docIntelligenceEndpoint
    docIntelligenceApiKey: docIntelligenceApiKey
  }
}

// ── API App
module apiApp 'modules/api-app.bicep' = {
  name: 'api-app'
  scope: rg
  params: {
    name: '${abbrs.webApp}-api-${resourceToken}'
    location: location
    tags: tags
    appServicePlanId: appServicePlan.outputs.id
    appInsightsConnectionString: appInsights.outputs.connectionString
    cosmosDbConnectionString: cosmosDb.outputs.connectionString
    openAIEndpoint: openAIEndpoint
    openAIApiKey: openAIApiKey
    openAIApiVersion: openAIApiVersion
    openAIBatchEndpoint: openAIBatchEndpoint
    openAIBatchApiKey: openAIBatchApiKey
    docIntelligenceEndpoint: docIntelligenceEndpoint
    docIntelligenceApiKey: docIntelligenceApiKey
  }
}

// ── Function App
module functionApp 'modules/function-app.bicep' = {
  name: 'function-app'
  scope: rg
  params: {
    name: '${abbrs.functionApp}-${resourceToken}'
    location: location
    tags: tags
    appServicePlanId: appServicePlan.outputs.id
    appInsightsConnectionString: appInsights.outputs.connectionString
    appInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    storageAccountConnectionString: storageAccount.outputs.connectionString
    cosmosDbConnectionString: cosmosDb.outputs.connectionString
    openAIEndpoint: openAIEndpoint
    openAIApiKey: openAIApiKey
    openAIApiVersion: openAIApiVersion
    openAIBatchEndpoint: openAIBatchEndpoint
    openAIBatchApiKey: openAIBatchApiKey
    docIntelligenceEndpoint: docIntelligenceEndpoint
    docIntelligenceApiKey: docIntelligenceApiKey
  }
}

// ── Outputs
output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output ADMIN_PORTAL_URL string = adminPortal.outputs.uri
output API_APP_URL string = apiApp.outputs.uri
output FUNCTION_APP_URL string = functionApp.outputs.uri
output COSMOS_DB_NAME string = cosmosDb.outputs.name
output AZURE_OPENAI_ENDPOINT string = openAIEndpoint
output DOCUMENT_INTELLIGENCE_ENDPOINT string = docIntelligenceEndpoint
