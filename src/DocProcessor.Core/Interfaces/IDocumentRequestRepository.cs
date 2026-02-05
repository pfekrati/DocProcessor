using DocProcessor.Core.Entities;
using DocProcessor.Core.Enums;

namespace DocProcessor.Core.Interfaces;

public interface IDocumentRequestRepository
{
    Task<DocumentRequest?> GetByIdAsync(string id);
    Task<IEnumerable<DocumentRequest>> GetByStatusAsync(ProcessingStatus status);
    Task<IEnumerable<DocumentRequest>> GetQueuedRequestsAsync(int limit);
    Task<int> GetQueuedRequestsCountAsync();
    Task<IEnumerable<DocumentRequest>> GetByBatchIdAsync(string batchId);
    Task<DocumentRequest> CreateAsync(DocumentRequest request);
    Task UpdateAsync(DocumentRequest request);
    Task UpdateStatusAsync(string id, ProcessingStatus status, string? errorMessage = null);
    Task UpdateResultAsync(string id, string result);
    Task<IEnumerable<DocumentRequest>> GetAllAsync(int skip = 0, int take = 100);
    Task<long> GetTotalCountAsync();
    Task<IEnumerable<DocumentRequest>> GetRecentRequestsAsync(int count = 50);
}
