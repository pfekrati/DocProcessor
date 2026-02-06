using DocProcessor.Core.Configuration;
using DocProcessor.Core.Entities;
using DocProcessor.Core.Enums;
using DocProcessor.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocProcessor.Functions;

/// <summary>
/// Timer-triggered function that processes pending document requests and submits batches to Azure OpenAI.
/// Runs every 15 minutes by default (configurable via BatchSubmitSchedule).
/// </summary>
public class BatchSubmitFunction
{
    private readonly IDocumentRequestRepository _requestRepository;
    private readonly IBatchJobRepository _batchJobRepository;
    private readonly IBatchLlmService _batchLlmService;
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly BatchProcessingSettings _settings;
    private readonly ILogger<BatchSubmitFunction> _logger;

    public BatchSubmitFunction(
        IDocumentRequestRepository requestRepository,
        IBatchJobRepository batchJobRepository,
        IBatchLlmService batchLlmService,
        IDocumentIntelligenceService documentIntelligenceService,
        IOptions<BatchProcessingSettings> settings,
        ILogger<BatchSubmitFunction> logger)
    {
        _requestRepository = requestRepository;
        _batchJobRepository = batchJobRepository;
        _batchLlmService = batchLlmService;
        _documentIntelligenceService = documentIntelligenceService;
        _settings = settings.Value;
        _logger = logger;
    }

    [Function("BatchSubmitFunction")]
    public async Task Run([TimerTrigger("%BatchSubmitSchedule%")] TimerInfo timerInfo)
    {
        _logger.LogInformation("BatchSubmitFunction triggered at: {Time}", DateTime.UtcNow);

        try
        {
            // First, process pending requests to convert them to markdown
            await ProcessPendingRequestsAsync();

            // Then, submit queued requests to batch API
            await ProcessBatchQueueAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BatchSubmitFunction");
            throw;
        }

        _logger.LogInformation("BatchSubmitFunction completed. Next scheduled run: {NextRun}", 
            timerInfo.ScheduleStatus?.Next);
    }

    private async Task ProcessPendingRequestsAsync()
    {
        var pendingRequests = (await _requestRepository.GetByStatusAsync(ProcessingStatus.Pending)).ToList();

        if (pendingRequests.Count == 0)
        {
            _logger.LogDebug("No pending requests to process");
            return;
        }

        _logger.LogInformation("Processing {Count} pending requests for markdown conversion", pendingRequests.Count);

        foreach (var request in pendingRequests)
        {
            try
            {
                _logger.LogDebug("Converting document {RequestId} to markdown", request.Id);

                var markdownContent = await _documentIntelligenceService.ConvertToMarkdownAsync(
                    request.DocumentContent,
                    request.DocumentName);

                request.MarkdownContent = markdownContent;
                request.Status = ProcessingStatus.Queued;
                await _requestRepository.UpdateAsync(request);

                _logger.LogDebug("Successfully converted document {RequestId} to markdown", request.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert document {RequestId} to markdown", request.Id);
                request.Status = ProcessingStatus.Failed;
                request.ErrorMessage = $"Failed to convert document to markdown: {ex.Message}";
                await _requestRepository.UpdateAsync(request);
            }
        }
    }

    private async Task ProcessBatchQueueAsync()
    {
        var queuedCount = await _requestRepository.GetQueuedRequestsCountAsync();

        _logger.LogInformation("Current queue size: {QueuedCount}", queuedCount);

        if (queuedCount == 0)
        {
            _logger.LogDebug("No requests in queue, skipping batch processing");
            return;
        }

        // Process if threshold reached or on interval if there are any requests
        if (queuedCount >= _settings.QueueSizeThreshold || queuedCount > 0)
        {
            await SubmitBatchAsync();
        }
    }

    private async Task SubmitBatchAsync()
    {
        // Get queued requests (up to threshold limit)
        var requests = (await _requestRepository.GetQueuedRequestsAsync(_settings.QueueSizeThreshold)).ToList();

        if (requests.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Submitting batch with {Count} requests", requests.Count);

        try
        {
            // Create batch job record
            var batchJob = new BatchJob
            {
                RequestIds = requests.Select(r => r.Id).ToList(),
                TotalRequests = requests.Count,
                Status = BatchJobStatus.Created
            };

            await _batchJobRepository.CreateAsync(batchJob);

            // Update request statuses
            foreach (var request in requests)
            {
                request.Status = ProcessingStatus.BatchSubmitted;
                request.BatchId = batchJob.Id;
                await _requestRepository.UpdateAsync(request);
            }

            // Submit to OpenAI Batch API
            var openAiBatchId = await _batchLlmService.SubmitBatchAsync(requests);

            // Update batch job with OpenAI batch ID
            batchJob.OpenAiBatchId = openAiBatchId;
            batchJob.Status = BatchJobStatus.Submitted;
            batchJob.SubmittedAt = DateTime.UtcNow;
            await _batchJobRepository.UpdateAsync(batchJob);

            _logger.LogInformation("Successfully submitted batch {BatchId} with OpenAI batch ID {OpenAiBatchId}",
                batchJob.Id, openAiBatchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit batch");

            // Revert request statuses back to queued
            foreach (var request in requests)
            {
                request.Status = ProcessingStatus.Queued;
                request.BatchId = null;
                await _requestRepository.UpdateAsync(request);
            }

            throw;
        }
    }
}
