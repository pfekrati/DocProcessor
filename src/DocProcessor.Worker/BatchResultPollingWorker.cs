using DocProcessor.Core.Configuration;
using DocProcessor.Core.Entities;
using DocProcessor.Core.Enums;
using DocProcessor.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace DocProcessor.Worker;

public class BatchResultPollingWorker : BackgroundService
{
    private readonly IDocumentRequestRepository _requestRepository;
    private readonly IBatchJobRepository _batchJobRepository;
    private readonly IBatchLlmService _batchLlmService;
    private readonly ICallbackService _callbackService;
    private readonly BatchProcessingSettings _settings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<BatchResultPollingWorker> _logger;

    public BatchResultPollingWorker(
        IDocumentRequestRepository requestRepository,
        IBatchJobRepository batchJobRepository,
        IBatchLlmService batchLlmService,
        ICallbackService callbackService,
        IOptions<BatchProcessingSettings> settings,
        IHostEnvironment environment,
        ILogger<BatchResultPollingWorker> logger)
    {
        _requestRepository = requestRepository;
        _batchJobRepository = batchJobRepository;
        _batchLlmService = batchLlmService;
        _callbackService = callbackService;
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Batch Result Polling Worker started");
        _logger.LogInformation("Polling interval: {Interval} minutes", _settings.BatchCheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollBatchResultsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch result polling worker");

                if (_environment.IsDevelopment())
                {
                    throw;
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.BatchCheckIntervalMinutes), stoppingToken);
        }
    }

    private async Task PollBatchResultsAsync(CancellationToken stoppingToken)
    {
        var submittedBatches = await _batchJobRepository.GetSubmittedJobsAsync();

        foreach (var batch in submittedBatches)
        {
            if (stoppingToken.IsCancellationRequested) break;

            if (string.IsNullOrEmpty(batch.OpenAiBatchId))
            {
                _logger.LogWarning("Batch {BatchId} has no OpenAI batch ID", batch.Id);
                continue;
            }

            try
            {
                await ProcessBatchResultAsync(batch, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process results for batch {BatchId}", batch.Id);

                if (_environment.IsDevelopment())
                {
                    throw;
                }
            }
        }
    }

    private async Task ProcessBatchResultAsync(BatchJob batch, CancellationToken stoppingToken)
    {
        _logger.LogDebug("Checking status for batch {BatchId}", batch.Id);

        var status = await _batchLlmService.GetBatchStatusAsync(batch.OpenAiBatchId!);
        
        _logger.LogInformation("Batch {BatchId} status: {Status}", batch.Id, status.Status);

        if (status.Status == "Completed")
        {
            await HandleCompletedBatchAsync(batch);
        }
        else if (status.Status == "Failed" || status.Status == "Expired" || status.Status == "Cancelled")
        {
            await HandleFailedBatchAsync(batch, $"Batch {status.Status}");
        }
        else
        {
            // Update progress
            batch.Status = BatchJobStatus.Processing;
            batch.CompletedRequests = status.CompletedRequests;
            batch.FailedRequests = status.FailedRequests;
            await _batchJobRepository.UpdateAsync(batch);
        }
    }

    private async Task HandleCompletedBatchAsync(BatchJob batch)
    {
        _logger.LogInformation("Processing completed batch {BatchId}", batch.Id);

        var result = await _batchLlmService.GetBatchResultAsync(batch.OpenAiBatchId!);

        var completedCount = 0;
        var failedCount = 0;

        // Process successful results
        foreach (var (requestId, responseContent) in result.Results)
        {
            try
            {
                await _requestRepository.UpdateResultAsync(requestId, responseContent);
                
                // Send callback if configured
                var request = await _requestRepository.GetByIdAsync(requestId);
                if (request?.CallbackUrl != null)
                {
                    await _callbackService.SendCallbackAsync(request.CallbackUrl, requestId, responseContent);
                }
                
                completedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update result for request {RequestId}", requestId);
                failedCount++;
            }
        }

        // Process errors
        foreach (var (requestId, errorMessage) in result.Errors)
        {
            await _requestRepository.UpdateStatusAsync(requestId, ProcessingStatus.Failed, errorMessage);
            failedCount++;
        }

        // Update batch job
        batch.Status = failedCount > 0 && completedCount > 0 
            ? BatchJobStatus.PartiallyCompleted 
            : (failedCount > 0 ? BatchJobStatus.Failed : BatchJobStatus.Completed);
        batch.CompletedAt = DateTime.UtcNow;
        batch.CompletedRequests = completedCount;
        batch.FailedRequests = failedCount;
        await _batchJobRepository.UpdateAsync(batch);

        _logger.LogInformation("Batch {BatchId} completed. Success: {Completed}, Failed: {Failed}", 
            batch.Id, completedCount, failedCount);
    }

    private async Task HandleFailedBatchAsync(BatchJob batch, string errorMessage)
    {
        _logger.LogWarning("Batch {BatchId} failed: {Error}", batch.Id, errorMessage);

        // Update all requests in the batch as failed
        foreach (var requestId in batch.RequestIds)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);
            if (request != null && request.RetryCount < _settings.MaxRetryCount)
            {
                // Retry by putting back in queue
                request.Status = ProcessingStatus.Queued;
                request.BatchId = null;
                request.RetryCount++;
                await _requestRepository.UpdateAsync(request);
                _logger.LogInformation("Request {RequestId} queued for retry (attempt {Attempt})", 
                    requestId, request.RetryCount);
            }
            else
            {
                await _requestRepository.UpdateStatusAsync(requestId, ProcessingStatus.Failed, errorMessage);
            }
        }

        batch.Status = BatchJobStatus.Failed;
        batch.ErrorMessage = errorMessage;
        batch.CompletedAt = DateTime.UtcNow;
        await _batchJobRepository.UpdateAsync(batch);
    }
}
