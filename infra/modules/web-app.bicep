param name string
param location string
param tags object = {}
param appServicePlanId string
param appInsightsConnectionString string

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

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'admin-portal' })
  identity: {
    type: useManagedIdentity ? 'SystemAssigned' : 'None'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
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
      ]
    }
  }
}

output name string = webApp.name
output defaultHostName string = webApp.properties.defaultHostName
output uri string = 'https://${webApp.properties.defaultHostName}'
output principalId string = useManagedIdentity ? webApp.identity.principalId : ''
