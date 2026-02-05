using DocProcessor.Core.Enums;

namespace DocProcessor.Core.DTOs;

public record DocumentProcessingRequest
{
    public required byte[] DocumentContent { get; init; }
    public required string DocumentName { get; init; }
    public required string Instruction { get; init; }
    public required string JsonSchema { get; init; }
    public string? ModelDeploymentId { get; init; }
    public ProcessingMode ProcessingMode { get; init; } = ProcessingMode.RealTime;
    public string? CallbackUrl { get; init; }
}

public record DocumentProcessingResponse
{
    public required string RequestId { get; init; }
    public required ProcessingStatus Status { get; init; }
    public string? Result { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record BatchStatusResponse
{
    public required string BatchId { get; init; }
    public required string Status { get; init; }
    public int TotalRequests { get; init; }
    public int CompletedRequests { get; init; }
    public int FailedRequests { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record RequestStatusResponse
{
    public required string RequestId { get; init; }
    public required ProcessingStatus Status { get; init; }
    public string? Result { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
