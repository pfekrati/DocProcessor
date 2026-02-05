using DocProcessor.Core.Configuration;
using DocProcessor.Core.Entities;
using DocProcessor.Core.Enums;
using DocProcessor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DocProcessor.Infrastructure.Repositories;

public class DocumentRequestRepository : IDocumentRequestRepository
{
    private readonly IMongoCollection<DocumentRequest> _collection;
    private readonly ILogger<DocumentRequestRepository> _logger;

    public DocumentRequestRepository(
        IOptions<CosmosDbSettings> settings,
        ILogger<DocumentRequestRepository> logger)
    {
        _logger = logger;
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<DocumentRequest>(settings.Value.RequestsCollectionName);
        
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var indexKeys = Builders<DocumentRequest>.IndexKeys;
        
        var statusIndex = new CreateIndexModel<DocumentRequest>(indexKeys.Ascending(x => x.Status));
        var createdAtIndex = new CreateIndexModel<DocumentRequest>(indexKeys.Descending(x => x.CreatedAt));
        var batchIdIndex = new CreateIndexModel<DocumentRequest>(indexKeys.Ascending(x => x.BatchId));

        _collection.Indexes.CreateMany([statusIndex, createdAtIndex, batchIdIndex]);
    }

    public async Task<DocumentRequest?> GetByIdAsync(string id)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DocumentRequest>> GetByStatusAsync(ProcessingStatus status)
    {
        return await _collection.Find(x => x.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<DocumentRequest>> GetQueuedRequestsAsync(int limit)
    {
        return await _collection
            .Find(x => x.Status == ProcessingStatus.Queued)
            .SortBy(x => x.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<int> GetQueuedRequestsCountAsync()
    {
        return (int)await _collection.CountDocumentsAsync(x => x.Status == ProcessingStatus.Queued);
    }

    public async Task<IEnumerable<DocumentRequest>> GetByBatchIdAsync(string batchId)
    {
        return await _collection.Find(x => x.BatchId == batchId).ToListAsync();
    }

    public async Task<DocumentRequest> CreateAsync(DocumentRequest request)
    {
        await _collection.InsertOneAsync(request);
        _logger.LogInformation("Created document request with ID: {RequestId}", request.Id);
        return request;
    }

    public async Task UpdateAsync(DocumentRequest request)
    {
        request.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(x => x.Id == request.Id, request);
    }

    public async Task UpdateStatusAsync(string id, ProcessingStatus status, string? errorMessage = null)
    {
        var update = Builders<DocumentRequest>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        if (errorMessage != null)
        {
            update = update.Set(x => x.ErrorMessage, errorMessage);
        }

        if (status == ProcessingStatus.Completed || status == ProcessingStatus.Failed)
        {
            update = update.Set(x => x.CompletedAt, DateTime.UtcNow);
        }

        await _collection.UpdateOneAsync(x => x.Id == id, update);
    }

    public async Task UpdateResultAsync(string id, string result)
    {
        var update = Builders<DocumentRequest>.Update
            .Set(x => x.Result, result)
            .Set(x => x.Status, ProcessingStatus.Completed)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.CompletedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(x => x.Id == id, update);
    }

    public async Task<IEnumerable<DocumentRequest>> GetAllAsync(int skip = 0, int take = 100)
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

    public async Task<IEnumerable<DocumentRequest>> GetRecentRequestsAsync(int count = 50)
    {
        return await _collection
            .Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .Limit(count)
            .ToListAsync();
    }
}
