using DocProcessor.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IDocumentRequestRepository _requestRepository;
    private readonly IBatchJobRepository _batchJobRepository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IDocumentRequestRepository requestRepository,
        IBatchJobRepository batchJobRepository,
        ILogger<AdminController> logger)
    {
        _requestRepository = requestRepository;
        _batchJobRepository = batchJobRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all document requests with pagination
    /// </summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var requests = await _requestRepository.GetAllAsync(skip, take);
        var total = await _requestRepository.GetTotalCountAsync();

        return Ok(new
        {
            data = requests.Select(r => new
            {
                r.Id,
                r.DocumentName,
                r.ProcessingMode,
                r.Status,
                r.CreatedAt,
                r.CompletedAt,
                r.ErrorMessage,
                hasResult = !string.IsNullOrEmpty(r.Result)
            }),
            total,
            skip,
            take
        });
    }

    /// <summary>
    /// Get all batch jobs with pagination
    /// </summary>
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatchJobs([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var batches = await _batchJobRepository.GetAllAsync(skip, take);
        var total = await _batchJobRepository.GetTotalCountAsync();

        return Ok(new
        {
            data = batches,
            total,
            skip,
            take
        });
    }

    /// <summary>
    /// Get queue statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var queuedCount = await _requestRepository.GetQueuedRequestsCountAsync();
        var totalRequests = await _requestRepository.GetTotalCountAsync();
        var totalBatches = await _batchJobRepository.GetTotalCountAsync();

        return Ok(new
        {
            queuedRequests = queuedCount,
            totalRequests,
            totalBatches
        });
    }

    /// <summary>
    /// Get request details by ID
    /// </summary>
    [HttpGet("requests/{requestId}")]
    public async Task<IActionResult> GetRequestDetails(string requestId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        
        if (request == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            request.Id,
            request.DocumentName,
            request.DocumentType,
            request.ProcessingMode,
            request.Status,
            request.Instruction,
            request.JsonSchema,
            request.ModelDeploymentId,
            request.CallbackUrl,
            request.Result,
            request.ErrorMessage,
            request.BatchId,
            request.CreatedAt,
            request.UpdatedAt,
            request.CompletedAt,
            request.RetryCount
        });
    }
}
