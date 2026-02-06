param name string
param location string
param tags object = {}

@allowed(['S0'])
param sku string = 'S0'

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

output endpoint string = openAI.properties.endpoint
output apiKey string = openAI.listKeys().key1
output name string = openAI.name
