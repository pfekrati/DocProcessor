using DocProcessor.Core.Configuration;
using DocProcessor.Core.DTOs;
using DocProcessor.Core.Entities;
using DocProcessor.Core.Enums;
using DocProcessor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocProcessor.Infrastructure.Services;

public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IDocumentRequestRepository _requestRepository;
    private readonly IBatchJobRepository _batchJobRepository;
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly ILlmService _llmService;
    private readonly IBatchLlmService _batchLlmService;
    private readonly BatchProcessingSettings _settings;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        IDocumentRequestRepository requestRepository,
        IBatchJobRepository batchJobRepository,
        IDocumentIntelligenceService documentIntelligenceService,
        ILlmService llmService,
        IBatchLlmService batchLlmService,
        IOptions<BatchProcessingSettings> settings,
        ILogger<DocumentProcessingService> logger)
    {
        _requestRepository = requestRepository;
        _batchJobRepository = batchJobRepository;
        _documentIntelligenceService = documentIntelligenceService;
        _llmService = llmService;
        _batchLlmService = batchLlmService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<DocumentProcessingResponse> ProcessRealTimeAsync(DocumentProcessingRequest request)
    {
        _logger.LogInformation("Processing real-time request for document {DocumentName}", request.DocumentName);

        var documentRequest = new DocumentRequest
        {
            DocumentContent = request.DocumentContent,
            DocumentName = request.DocumentName,
            Instruction = request.Instruction,
            JsonSchema = request.JsonSchema,
            ModelDeploymentId = request.ModelDeploymentId,
            ProcessingMode = ProcessingMode.RealTime,
            CallbackUrl = request.CallbackUrl,
            Status = ProcessingStatus.Processing,
            DocumentType = DetermineDocumentType(request.DocumentName)
        };

        try
        {
            // Save initial request
            await _requestRepository.CreateAsync(documentRequest);

            // Convert document to markdown
            var markdownContent = await _documentIntelligenceService.ConvertToMarkdownAsync(
                request.DocumentContent,
                request.DocumentName);

            documentRequest.MarkdownContent = markdownContent;

            // Process with LLM
            var result = await _llmService.ProcessDocumentAsync(
                markdownContent,
                request.Instruction,
                request.JsonSchema,
                request.ModelDeploymentId);

            documentRequest.Result = result;
            documentRequest.Status = ProcessingStatus.Completed;
            documentRequest.CompletedAt = DateTime.UtcNow;

            await _requestRepository.UpdateAsync(documentRequest);

            _logger.LogInformation("Successfully processed real-time request {RequestId}", documentRequest.Id);

            return new DocumentProcessingResponse
            {
                RequestId = documentRequest.Id,
                Status = ProcessingStatus.Completed,
                Result = result,
                CreatedAt = documentRequest.CreatedAt,
                CompletedAt = documentRequest.CompletedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process real-time request {RequestId}", documentRequest.Id);

            documentRequest.Status = ProcessingStatus.Failed;
            documentRequest.ErrorMessage = ex.Message;
            await _requestRepository.UpdateAsync(documentRequest);

            return new DocumentProcessingResponse
            {
                RequestId = documentRequest.Id,
                Status = ProcessingStatus.Failed,
                ErrorMessage = ex.Message,
                CreatedAt = documentRequest.CreatedAt
            };
        }
    }

    public async Task<DocumentProcessingResponse> QueueForBatchProcessingAsync(DocumentProcessingRequest request)
    {
        _logger.LogInformation("Queueing batch request for document {DocumentName}", request.DocumentName);

        try
        {
            // Queue the request immediately without converting to markdown
            // The markdown conversion will be done asynchronously by the batch worker
            var documentRequest = new DocumentRequest
            {
                DocumentContent = request.DocumentContent,
                DocumentName = request.DocumentName,
                Instruction = request.Instruction,
                JsonSchema = request.JsonSchema,
                ModelDeploymentId = request.ModelDeploymentId,
                ProcessingMode = ProcessingMode.Batch,
                CallbackUrl = request.CallbackUrl,
                Status = ProcessingStatus.Queued,
                DocumentType = DetermineDocumentType(request.DocumentName)
            };

            await _requestRepository.CreateAsync(documentRequest);

            _logger.LogInformation("Successfully queued batch request {RequestId}", documentRequest.Id);

            _ = SubmitBatchIfThresholdMetAsync();

            return new DocumentProcessingResponse
            {
                RequestId = documentRequest.Id,
                Status = ProcessingStatus.Queued,
                CreatedAt = documentRequest.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue batch request for document {DocumentName}", request.DocumentName);
            throw;
        }
    }

    private async Task SubmitBatchIfThresholdMetAsync()
    {
        var pendingRequests = (await _requestRepository.GetByStatusAsync(ProcessingStatus.Queued)).ToList();


        _logger.LogInformation("Current queue size: {QueuedCount}, threshold: {Threshold}", pendingRequests.Count, _settings.QueueSizeThreshold);

        if (pendingRequests.Count < _settings.QueueSizeThreshold)
        {
            _logger.LogDebug("Queue size {QueuedCount} has not reached threshold {Threshold}, skipping batch submission",
                pendingRequests, _settings.QueueSizeThreshold);
            return;
        }


        await ProcessBatchQueueAsync(pendingRequests);

    }

    public async Task ProcessBatchQueueAsync(List<DocumentRequest>? pendingRequests = null)
    {

        List<DocumentRequest> requests = pendingRequests ??
            (await _requestRepository.GetQueuedRequestsAsync(_settings.QueueSizeThreshold)).ToList();

        if (requests.Count == 0)
            return;

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

            // Update request statuses
            foreach (var request in requests)
            {
                request.Status = ProcessingStatus.BatchSubmitted;
                request.BatchId = batchJob.Id;
                await _requestRepository.UpdateAsync(request);
            }

            await _batchJobRepository.CreateAsync(batchJob);

            _logger.LogInformation("Processing {Count} queued requests for markdown conversion", requests.Count);


            foreach (var request in requests)
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

    public async Task<RequestStatusResponse?> GetRequestStatusAsync(string requestId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);

        if (request == null)
            return null;

        return new RequestStatusResponse
        {
            RequestId = request.Id,
            Status = request.Status,
            Result = request.Result,
            ErrorMessage = request.ErrorMessage,
            CreatedAt = request.CreatedAt,
            CompletedAt = request.CompletedAt
        };
    }

    public async Task<DocumentRequest?> GetRequestAsync(string requestId)
    {
        return await _requestRepository.GetByIdAsync(requestId);
    }

    private static DocumentType DetermineDocumentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => DocumentType.Pdf,
            ".doc" or ".docx" => DocumentType.Word,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" => DocumentType.Image,
            ".html" or ".htm" => DocumentType.Html,
            ".txt" => DocumentType.Text,
            _ => DocumentType.Unknown
        };
    }
}
