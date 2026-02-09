using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using DocProcessor.Core.Configuration;
using DocProcessor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocProcessor.Infrastructure.Services;

public class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<DocumentIntelligenceService> _logger;

    public DocumentIntelligenceService(
        IOptions<DocumentIntelligenceSettings> settings,
        ILogger<DocumentIntelligenceService> logger)
    {
        _logger = logger;
        var endpoint = new Uri(settings.Value.Endpoint);

        if (settings.Value.UseManagedIdentity)
        {
            _client = new DocumentIntelligenceClient(endpoint, new DefaultAzureCredential());
        }
        else
        {
            _client = new DocumentIntelligenceClient(endpoint, new AzureKeyCredential(settings.Value.ApiKey));
        }
    }

    public async Task<string> ConvertToMarkdownAsync(byte[] documentContent, string fileName)
    {
        _logger.LogInformation("Converting document {FileName} to markdown", fileName);

        try
        {
            var options = new AnalyzeDocumentOptions("prebuilt-layout", BinaryData.FromBytes(documentContent))
            {
                OutputContentFormat = DocumentContentFormat.Markdown
            };

            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, options);
            var result = operation.Value;

            _logger.LogInformation("Successfully converted document {FileName} to markdown", fileName);

            return result.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert document {FileName} to markdown", fileName);
            throw;
        }
    }
}
