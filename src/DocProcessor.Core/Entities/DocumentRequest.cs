using DocProcessor.Core.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DocProcessor.Core.Entities;

public class DocumentRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("documentContent")]
    public byte[] DocumentContent { get; set; } = [];

    [BsonElement("documentName")]
    public string DocumentName { get; set; } = string.Empty;

    [BsonElement("documentType")]
    public DocumentType DocumentType { get; set; }

    [BsonElement("instruction")]
    public string Instruction { get; set; } = string.Empty;

    [BsonElement("jsonSchema")]
    public string JsonSchema { get; set; } = string.Empty;

    [BsonElement("modelDeploymentId")]
    public string? ModelDeploymentId { get; set; }

    [BsonElement("processingMode")]
    public ProcessingMode ProcessingMode { get; set; }

    [BsonElement("status")]
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

    [BsonElement("callbackUrl")]
    public string? CallbackUrl { get; set; }

    [BsonElement("markdownContent")]
    public string? MarkdownContent { get; set; }

    [BsonElement("result")]
    public string? Result { get; set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("batchId")]
    public string? BatchId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("retryCount")]
    public int RetryCount { get; set; } = 0;

    [BsonElement("clientId")]
    public string? ClientId { get; set; }
}
