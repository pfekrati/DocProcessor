param name string
param location string
param tags object = {}

@allowed(['S0'])
param sku string = 'S0'

@description('Model name for real-time chat completion deployment.')
param realtimeModelName string = 'gpt-4.1'

@description('Deployment name for real-time chat completion.')
param realtimeDeploymentName string = 'gpt-4.1'

@description('Model name for batch processing deployment.')
param batchModelName string = 'gpt-4.1'

@description('Deployment name for batch processing.')
param batchDeploymentName string = 'gpt-4.1-batch'

resource openAI 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: sku
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
  }
}

resource realtimeDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-04-01-preview' = {
  parent: openAI
  name: realtimeDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: 50
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: realtimeModelName
      version: '2025-04-14'
    }
  }
}

resource batchDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-04-01-preview' = {
  parent: openAI
  name: batchDeploymentName
  dependsOn: [realtimeDeployment]
  sku: {
    name: 'GlobalBatch'
    capacity: 50
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: batchModelName
      version: '2025-04-14'
    }
  }
}

output endpoint string = openAI.properties.endpoint
output apiKey string = openAI.listKeys().key1
output name string = openAI.name
output id string = openAI.id
output realtimeDeploymentName string = realtimeDeployment.name
output batchDeploymentName string = batchDeployment.name
