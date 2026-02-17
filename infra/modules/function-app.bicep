param name string
param location string
param tags object = {}
param appServicePlanId string
param appInsightsConnectionString string
param appInsightsInstrumentationKey string
param storageAccountConnectionString string

param cosmosDbConnectionString string
param cosmosDbAccountEndpoint string = ''
param openAIEndpoint string
param openAIApiKey string
param openAIApiVersion string = '2024-05-01-preview'
param openAIBatchEndpoint string
param openAIBatchApiKey string
param docIntelligenceEndpoint string
param docIntelligenceApiKey string
param useManagedIdentity bool = false

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'functions' })
  kind: 'functionapp,linux'
  identity: {
    type: useManagedIdentity ? 'SystemAssigned' : 'None'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccountConnectionString
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'BatchSubmitSchedule'
          value: '0 */15 * * * *'
        }
        {
          name: 'BatchPollingSchedule'
          value: '0 */5 * * * *'
        }
        {
          name: 'AzureOpenAI__Endpoint'
          value: openAIEndpoint
        }
        {
          name: 'AzureOpenAI__ApiKey'
          value: useManagedIdentity ? '' : openAIApiKey
        }
        {
          name: 'AzureOpenAI__ApiVersion'
          value: openAIApiVersion
        }
        {
          name: 'AzureOpenAI__BatchEndpoint'
          value: openAIBatchEndpoint
        }
        {
          name: 'AzureOpenAI__BatchApiKey'
          value: useManagedIdentity ? '' : openAIBatchApiKey
        }
        {
          name: 'AzureOpenAI__UseManagedIdentity'
          value: string(useManagedIdentity)
        }
        {
          name: 'DocumentIntelligence__Endpoint'
          value: docIntelligenceEndpoint
        }
        {
          name: 'DocumentIntelligence__ApiKey'
          value: useManagedIdentity ? '' : docIntelligenceApiKey
        }
        {
          name: 'DocumentIntelligence__UseManagedIdentity'
          value: string(useManagedIdentity)
        }
        {
          name: 'CosmosDb__ConnectionString'
          value: useManagedIdentity ? '' : cosmosDbConnectionString
        }
        {
          name: 'CosmosDb__AccountEndpoint'
          value: useManagedIdentity ? cosmosDbAccountEndpoint : ''
        }
        {
          name: 'CosmosDb__UseManagedIdentity'
          value: string(useManagedIdentity)
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: 'DocProcessor'
        }
        {
          name: 'CosmosDb__RequestsCollectionName'
          value: 'Requests'
        }
        {
          name: 'CosmosDb__BatchJobsCollectionName'
          value: 'BatchJobs'
        }
        {
          name: 'BatchProcessing__QueueSizeThreshold'
          value: '100'
        }
        {
          name: 'BatchProcessing__ProcessingIntervalMinutes'
          value: '15'
        }
        {
          name: 'BatchProcessing__MaxRetryCount'
          value: '3'
        }
        {
          name: 'BatchProcessing__BatchCheckIntervalMinutes'
          value: '5'
        }
      ]
    }
  }
}

output name string = functionApp.name
output defaultHostName string = functionApp.properties.defaultHostName
output uri string = 'https://${functionApp.properties.defaultHostName}'
output principalId string = useManagedIdentity ? functionApp.identity.principalId : ''
