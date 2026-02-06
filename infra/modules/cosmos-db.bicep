param name string
param location string
param tags object = {}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-02-15-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'MongoDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    capabilities: [
      {
        name: 'EnableMongo'
      }
      {
        name: 'EnableServerless'
      }
    ]
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    apiProperties: {
      serverVersion: '4.2'
    }
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases@2024-02-15-preview' = {
  parent: cosmosDbAccount
  name: 'DocProcessor'
  properties: {
    resource: {
      id: 'DocProcessor'
    }
  }
}

resource requestsCollection 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases/collections@2024-02-15-preview' = {
  parent: database
  name: 'Requests'
  properties: {
    resource: {
      id: 'Requests'
      shardKey: {
        _id: 'Hash'
      }
    }
  }
}

resource batchJobsCollection 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases/collections@2024-02-15-preview' = {
  parent: database
  name: 'BatchJobs'
  properties: {
    resource: {
      id: 'BatchJobs'
      shardKey: {
        _id: 'Hash'
      }
    }
  }
}

output connectionString string = 'mongodb://${cosmosDbAccount.name}:${cosmosDbAccount.listKeys().primaryMasterKey}@${cosmosDbAccount.name}.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@${cosmosDbAccount.name}@'
output name string = cosmosDbAccount.name
