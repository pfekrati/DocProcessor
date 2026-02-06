param name string
param location string
param tags object = {}
param appServicePlanId string
param appInsightsConnectionString string

param cosmosDbConnectionString string
param openAIEndpoint string
param openAIApiKey string
param openAIApiVersion string = '2024-05-01-preview'
param openAIBatchEndpoint string
param openAIBatchApiKey string
param docIntelligenceEndpoint string
param docIntelligenceApiKey string

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'AzureOpenAI__Endpoint'
          value: openAIEndpoint
        }
        {
          name: 'AzureOpenAI__ApiKey'
          value: openAIApiKey
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
          value: openAIBatchApiKey
        }
        {
          name: 'DocumentIntelligence__Endpoint'
          value: docIntelligenceEndpoint
        }
        {
          name: 'DocumentIntelligence__ApiKey'
          value: docIntelligenceApiKey
        }
        {
          name: 'CosmosDb__ConnectionString'
          value: cosmosDbConnectionString
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

output name string = apiApp.name
output defaultHostName string = apiApp.properties.defaultHostName
output uri string = 'https://${apiApp.properties.defaultHostName}'
