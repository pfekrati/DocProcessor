using System.ClientModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using DocProcessor.Core.Configuration;
using DocProcessor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace DocProcessor.Infrastructure.Services;

public class LlmService : ILlmService
{
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<LlmService> _logger;

    public LlmService(
        IOptions<AzureOpenAISettings> settings,
        ILogger<LlmService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _client = new AzureOpenAIClient(
            new Uri(_settings.Endpoint),
            new ApiKeyCredential(_settings.ApiKey));
    }

    public async Task<string> ProcessDocumentAsync(
        string markdownContent,
        string instruction,
        string jsonSchema,
        string? modelDeploymentId = null)
    {
        var deploymentId = modelDeploymentId;
        
        _logger.LogInformation("Processing document with deployment {DeploymentId}", deploymentId);

        try
        {
            var chatClient = _client.GetChatClient(deploymentId);

            var systemPrompt = $"""
                You are a document processing assistant. Your task is to extract information from the provided document 
                based on the user's instruction and return the result in the specified JSON schema format.
                
                IMPORTANT: Your response must be valid JSON that conforms to the following schema:
                {jsonSchema}
                
                Only return the JSON object, no additional text or explanation.
                """;

            var userPrompt = $"""
                Instruction: {instruction}
                
                Document Content:
                {markdownContent}
                """;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.1f,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await chatClient.CompleteChatAsync(messages, options);
            var result = response.Value.Content[0].Text;

            // Validate JSON
            JsonDocument.Parse(result);
            
            _logger.LogInformation("Successfully processed document with deployment {DeploymentId}", deploymentId);
            
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "LLM response was not valid JSON");
            throw new InvalidOperationException("LLM response was not valid JSON", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document with deployment {DeploymentId}", deploymentId);
            throw;
        }
    }
}
