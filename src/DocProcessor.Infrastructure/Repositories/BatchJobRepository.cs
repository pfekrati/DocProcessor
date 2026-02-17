using Azure.Core;
using Azure.Identity;
using DocProcessor.Core.Configuration;
using DocProcessor.Core.Entities;
using DocProcessor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DocProcessor.Infrastructure.Repositories;

public class BatchJobRepository : IBatchJobRepository
{
    private readonly IMongoCollection<BatchJob> _collection;
    private readonly ILogger<BatchJobRepository> _logger;

    public BatchJobRepository(
        IOptions<CosmosDbSettings> settings,
        ILogger<BatchJobRepository> logger)
    {
        _logger = logger;
        var client = CreateMongoClient(settings.Value);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<BatchJob>(settings.Value.BatchJobsCollectionName);

        CreateIndexes();
    }

    private static MongoClient CreateMongoClient(CosmosDbSettings settings)
    {
        if (!settings.UseManagedIdentity)
            return new MongoClient(settings.ConnectionString);

        var credential = new DefaultAzureCredential();
        var mongoSettings = MongoClientSettings.FromUrl(new MongoUrl(settings.AccountEndpoint));
        mongoSettings.UseTls = true;
        mongoSettings.RetryWrites = false;
        mongoSettings.Credential = MongoCredential.CreateOidcCredential("azure", null)
            .WithMechanismProperty("ENVIRONMENT", "azure")
            .WithMechanismProperty("TOKEN_RESOURCE", "https://cosmos.azure.com");

        return new MongoClient(mongoSettings);
    }

    private void CreateIndexes()
    {
        var indexKeys = Builders<BatchJob>.IndexKeys;
        
        var statusIndex = new CreateIndexModel<BatchJob>(indexKeys.Ascending(x => x.Status));
        var openAiBatchIdIndex = new CreateIndexModel<BatchJob>(indexKeys.Ascending(x => x.OpenAiBatchId));
        var openAiCreatedAtIndex = new CreateIndexModel<BatchJob>(indexKeys.Descending(x => x.CreatedAt));

        _collection.Indexes.CreateMany([statusIndex, openAiBatchIdIndex, openAiCreatedAtIndex]);
    }

    public async Task<BatchJob?> GetByIdAsync(string id)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<BatchJob?> GetByOpenAiBatchIdAsync(string openAiBatchId)
    {
        return await _collection.Find(x => x.OpenAiBatchId == openAiBatchId).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<BatchJob>> GetPendingJobsAsync()
    {
        return await _collection
            .Find(x => x.Status == BatchJobStatus.Created)
            .SortBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BatchJob>> GetSubmittedJobsAsync()
    {
        return await _collection
            .Find(x => x.Status == BatchJobStatus.Submitted || x.Status == BatchJobStatus.Processing)
            .ToListAsync();
    }

    public async Task<BatchJob> CreateAsync(BatchJob batchJob)
    {
        await _collection.InsertOneAsync(batchJob);
        _logger.LogInformation("Created batch job with ID: {BatchJobId}", batchJob.Id);
        return batchJob;
    }

    public async Task UpdateAsync(BatchJob batchJob)
    {
        await _collection.ReplaceOneAsync(x => x.Id == batchJob.Id, batchJob);
    }

    public async Task<IEnumerable<BatchJob>> GetAllAsync(int skip = 0, int take = 100)
    {
        return await _collection
            .Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();
    }

    public async Task<long> GetTotalCountAsync()
    {
        return await _collection.CountDocumentsAsync(_ => true);
    }
}
