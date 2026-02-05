namespace DocProcessor.Core.Interfaces;

public interface IDocumentIntelligenceService
{
    Task<string> ConvertToMarkdownAsync(byte[] documentContent, string fileName);
}
