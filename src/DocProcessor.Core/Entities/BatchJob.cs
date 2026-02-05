using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DocProcessor.Core.Entities;

public class BatchJob
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("openAiBatchId")]
    public string? OpenAiBatchId { get; set; }

    [BsonElement("requestIds")]
    public List<string> RequestIds { get; set; } = [];

    [BsonElement("status")]
    public BatchJobStatus Status { get; set; } = BatchJobStatus.Created;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("submittedAt")]
    public DateTime? SubmittedAt { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("totalRequests")]
    public int TotalRequests { get; set; }

    [BsonElement("completedRequests")]
    public int CompletedRequests { get; set; }

    [BsonElement("failedRequests")]
    public int FailedRequests { get; set; }
}

public enum BatchJobStatus
{
    Created = 0,
    Submitted = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    PartiallyCompleted = 5
}
