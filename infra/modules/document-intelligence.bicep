param name string
param location string
param tags object = {}

@allowed(['S0'])
param sku string = 'S0'

resource documentIntelligence 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'FormRecognizer'
  sku: {
    name: sku
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
  }
}

output endpoint string = documentIntelligence.properties.endpoint
output apiKey string = documentIntelligence.listKeys().key1
output name string = documentIntelligence.name
output id string = documentIntelligence.id
