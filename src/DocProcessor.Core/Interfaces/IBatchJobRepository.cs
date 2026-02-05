using DocProcessor.Core.Entities;

namespace DocProcessor.Core.Interfaces;

public interface IBatchJobRepository
{
    Task<BatchJob?> GetByIdAsync(string id);
    Task<BatchJob?> GetByOpenAiBatchIdAsync(string openAiBatchId);
    Task<IEnumerable<BatchJob>> GetPendingJobsAsync();
    Task<IEnumerable<BatchJob>> GetSubmittedJobsAsync();
    Task<BatchJob> CreateAsync(BatchJob batchJob);
    Task UpdateAsync(BatchJob batchJob);
    Task<IEnumerable<BatchJob>> GetAllAsync(int skip = 0, int take = 100);
    Task<long> GetTotalCountAsync();
}
