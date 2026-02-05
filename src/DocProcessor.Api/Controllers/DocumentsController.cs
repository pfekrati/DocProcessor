using DocProcessor.Api.Models;
using DocProcessor.Core.DTOs;
using DocProcessor.Core.Enums;
using DocProcessor.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentProcessingService _processingService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentProcessingService processingService,
        ILogger<DocumentsController> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    /// <summary>
    /// Process a document with form data upload
    /// </summary>
    [HttpPost("process")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentProcessingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentProcessingResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessDocument([FromForm] ProcessDocumentRequest request)
    {
        if (request.Document.Length == 0)
        {
            return BadRequest("Document file is required");
        }

        using var memoryStream = new MemoryStream();
        await request.Document.CopyToAsync(memoryStream);

        var processingRequest = new DocumentProcessingRequest
        {
            DocumentContent = memoryStream.ToArray(),
            DocumentName = request.Document.FileName,
            Instruction = request.Instruction,
            JsonSchema = request.JsonSchema,
            ModelDeploymentId = request.ModelDeploymentId,
            ProcessingMode = request.ProcessingMode,
            CallbackUrl = request.CallbackUrl
        };

        if (request.ProcessingMode == ProcessingMode.RealTime)
        {
            var response = await _processingService.ProcessRealTimeAsync(processingRequest);
            return Ok(response);
        }
        else
        {
            var response = await _processingService.QueueForBatchProcessingAsync(processingRequest);
            return Accepted(response);
        }
    }

    /// <summary>
    /// Process a document with JSON body (base64 encoded)
    /// </summary>
    [HttpPost("process/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DocumentProcessingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentProcessingResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessDocumentJson([FromBody] ProcessDocumentJsonRequest request)
    {
        byte[] documentContent;
        try
        {
            documentContent = Convert.FromBase64String(request.DocumentBase64);
        }
        catch (FormatException)
        {
            return BadRequest("Invalid base64 document content");
        }

        var processingRequest = new DocumentProcessingRequest
        {
            DocumentContent = documentContent,
            DocumentName = request.DocumentName,
            Instruction = request.Instruction,
            JsonSchema = request.JsonSchema,
            ModelDeploymentId = request.ModelDeploymentId,
            ProcessingMode = request.ProcessingMode,
            CallbackUrl = request.CallbackUrl
        };

        if (request.ProcessingMode == ProcessingMode.RealTime)
        {
            var response = await _processingService.ProcessRealTimeAsync(processingRequest);
            return Ok(response);
        }
        else
        {
            var response = await _processingService.QueueForBatchProcessingAsync(processingRequest);
            return Accepted(response);
        }
    }

    /// <summary>
    /// Get the status of a processing request
    /// </summary>
    [HttpGet("status/{requestId}")]
    [ProducesResponseType(typeof(RequestStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(string requestId)
    {
        var status = await _processingService.GetRequestStatusAsync(requestId);
        
        if (status == null)
        {
            return NotFound($"Request with ID {requestId} not found");
        }

        return Ok(status);
    }

    /// <summary>
    /// Get the result of a completed processing request
    /// </summary>
    [HttpGet("result/{requestId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> GetResult(string requestId)
    {
        var request = await _processingService.GetRequestAsync(requestId);
        
        if (request == null)
        {
            return NotFound($"Request with ID {requestId} not found");
        }

        if (request.Status == ProcessingStatus.Completed && request.Result != null)
        {
            return Content(request.Result, "application/json");
        }

        if (request.Status == ProcessingStatus.Failed)
        {
            return BadRequest(new { error = request.ErrorMessage });
        }

        return Accepted(new 
        { 
            requestId = request.Id, 
            status = request.Status.ToString(),
            message = "Request is still being processed"
        });
    }
}
