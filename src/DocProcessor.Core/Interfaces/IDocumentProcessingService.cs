using DocProcessor.Core.DTOs;
using DocProcessor.Core.Entities;

namespace DocProcessor.Core.Interfaces;

public interface IDocumentProcessingService
{
    Task<DocumentProcessingResponse> ProcessRealTimeAsync(DocumentProcessingRequest request);
    Task<DocumentProcessingResponse> QueueForBatchProcessingAsync(DocumentProcessingRequest request);
    Task<RequestStatusResponse?> GetRequestStatusAsync(string requestId);
    Task<DocumentRequest?> GetRequestAsync(string requestId);
    Task ProcessBatchQueueAsync(List<DocumentRequest>? pendingRequests = null);
}
