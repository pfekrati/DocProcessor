using DocProcessor.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace DocProcessor.Api.Models;

public class ProcessDocumentRequest
{
    [Required]
    public required IFormFile Document { get; set; }

    [Required]
    public required string Instruction { get; set; }

    [Required]
    public required string JsonSchema { get; set; }

    public string? ModelDeploymentId { get; set; }

    public ProcessingMode ProcessingMode { get; set; } = ProcessingMode.RealTime;

    public string? CallbackUrl { get; set; }
}

public class ProcessDocumentJsonRequest
{
    [Required]
    public required string DocumentBase64 { get; set; }

    [Required]
    public required string DocumentName { get; set; }

    [Required]
    public required string Instruction { get; set; }

    [Required]
    public required string JsonSchema { get; set; }

    public string? ModelDeploymentId { get; set; }

    public ProcessingMode ProcessingMode { get; set; } = ProcessingMode.RealTime;

    public string? CallbackUrl { get; set; }
}
