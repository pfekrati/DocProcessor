using DocProcessor.Core.Entities;

namespace DocProcessor.Core.Interfaces;

public interface ILlmService
{
    Task<string> ProcessDocumentAsync(string markdownContent, string instruction, string jsonSchema, string? modelDeploymentId = null);
}

public interface IBatchLlmService
{
    Task<string> SubmitBatchAsync(IEnumerable<DocumentRequest> requests);
    Task<BatchResult> GetBatchResultAsync(string batchId);
    Task<BatchStatus> GetBatchStatusAsync(string batchId);
}

public record BatchResult
{
    public required string BatchId { get; init; }
    public required bool IsCompleted { get; init; }
    public Dictionary<string, string> Results { get; init; } = [];
    public Dictionary<string, string> Errors { get; init; } = [];
}

public record BatchStatus
{
    public required string Status { get; init; }
    public int TotalRequests { get; init; }
    public int CompletedRequests { get; init; }
    public int FailedRequests { get; init; }
}
