using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocProcessor.Core.Configuration;
using DocProcessor.Core.Entities;
using DocProcessor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocProcessor.Infrastructure.Services;

public class BatchLlmService : IBatchLlmService
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<BatchLlmService> _logger;

    public BatchLlmService(
        HttpClient httpClient,
        IOptions<AzureOpenAISettings> settings,
        ILogger<BatchLlmService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_settings.BatchEndpoint);
        _httpClient.DefaultRequestHeaders.Add("api-key", _settings.BatchApiKey);
    }

    public async Task<string> SubmitBatchAsync(IEnumerable<DocumentRequest> requests)
    {
        _logger.LogInformation("Submitting batch request to Azure OpenAI");

        try
        {
            // Create JSONL content for batch
            var jsonlContent = new StringBuilder();
            foreach (var request in requests)
            {
                var batchRequest = CreateBatchRequest(request);
                jsonlContent.AppendLine(JsonSerializer.Serialize(batchRequest));
            }

            // Upload the batch file
            var fileContent = new MultipartFormDataContent();
            var fileBytes = Encoding.UTF8.GetBytes(jsonlContent.ToString());
            var byteContent = new ByteArrayContent(fileBytes);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/jsonl");
            fileContent.Add(byteContent, "file", $"batch_{DateTime.UtcNow:yyyyMMddHHmmss}.jsonl");
            fileContent.Add(new StringContent("batch"), "purpose");

            var uploadResponse = await _httpClient.PostAsync(
            $"openai/v1/files",
            fileContent);

            uploadResponse.EnsureSuccessStatusCode();
            var uploadResult = await uploadResponse.Content.ReadAsStringAsync();
            var uploadJson = JsonDocument.Parse(uploadResult);
            var fileId = uploadJson.RootElement.GetProperty("id").GetString()!;

            // Create batch job
            var batchPayload = new
            {
                input_file_id = fileId,
                endpoint = "/chat/completions",
                completion_window = "24h"
            };

            var batchResponse = await _httpClient.PostAsync(
                $"openai/v1/batches",
                new StringContent(JsonSerializer.Serialize(batchPayload), Encoding.UTF8, "application/json"));

            batchResponse.EnsureSuccessStatusCode();
            var batchResult = await batchResponse.Content.ReadAsStringAsync();
            var batchJson = JsonDocument.Parse(batchResult);
            var batchId = batchJson.RootElement.GetProperty("id").GetString()!;

            _logger.LogInformation("Successfully submitted batch with ID: {BatchId}", batchId);

            return batchId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit batch request");
            throw;
        }
    }

    public async Task<BatchStatus> GetBatchStatusAsync(string batchId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"openai/v1/batches/{batchId}");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(result);

            var status = json.RootElement.GetProperty("status").GetString() ?? "Unknown";

            var requestCounts = json.RootElement.TryGetProperty("request_counts", out var counts) ? counts : default;

            return new BatchStatus
            {
                Status = MapBatchStatus(status),
                TotalRequests = requestCounts.ValueKind != JsonValueKind.Undefined &&
                               requestCounts.TryGetProperty("total", out var total) ? total.GetInt32() : 0,
                CompletedRequests = requestCounts.ValueKind != JsonValueKind.Undefined &&
                                   requestCounts.TryGetProperty("completed", out var completed) ? completed.GetInt32() : 0,
                FailedRequests = requestCounts.ValueKind != JsonValueKind.Undefined &&
                                requestCounts.TryGetProperty("failed", out var failed) ? failed.GetInt32() : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get batch status for {BatchId}", batchId);
            throw;
        }
    }

    public async Task<BatchResult> GetBatchResultAsync(string batchId)
    {
        try
        {
            // Get batch info to find output file
            var batchResponse = await _httpClient.GetAsync(
                $"openai/v1/batches/{batchId}");

            batchResponse.EnsureSuccessStatusCode();
            var batchResult = await batchResponse.Content.ReadAsStringAsync();
            var batchJson = JsonDocument.Parse(batchResult);

            var status = batchJson.RootElement.GetProperty("status").GetString();
            var results = new Dictionary<string, string>();
            var errors = new Dictionary<string, string>();

            if (status == "completed" &&
                batchJson.RootElement.TryGetProperty("output_file_id", out var outputFileId))
            {
                // Download output file
                var fileResponse = await _httpClient.GetAsync(
                    $"openai/v1/files/{outputFileId.GetString()}/content");

                fileResponse.EnsureSuccessStatusCode();
                var content = await fileResponse.Content.ReadAsStringAsync();

                foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var response = JsonDocument.Parse(line);
                        var customId = response.RootElement.GetProperty("custom_id").GetString()!;

                        if (response.RootElement.TryGetProperty("response", out var responseElement) &&
                            responseElement.TryGetProperty("body", out var bodyElement) &&
                            bodyElement.TryGetProperty("choices", out var choices) &&
                            choices.GetArrayLength() > 0)
                        {
                            var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();
                            results[customId] = messageContent ?? string.Empty;
                        }
                        else if (response.RootElement.TryGetProperty("error", out var errorElement))
                        {
                            var errorMsg = errorElement.TryGetProperty("message", out var msg)
                                ? msg.GetString()
                                : "Unknown error";
                            errors[customId] = errorMsg ?? "Unknown error";
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.LogWarning("Failed to parse batch result line");
                    }
                }
            }

            // Check for error file
            if (batchJson.RootElement.TryGetProperty("error_file_id", out var errorFileId) &&
                errorFileId.ValueKind != JsonValueKind.Null)
            {
                var errorFileResponse = await _httpClient.GetAsync(
                    $"openai/v1/files/{errorFileId.GetString()}/content");

                if (errorFileResponse.IsSuccessStatusCode)
                {
                    var errorContent = await errorFileResponse.Content.ReadAsStringAsync();

                    foreach (var line in errorContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            var errorResponse = JsonDocument.Parse(line);
                            var customId = errorResponse.RootElement.GetProperty("custom_id").GetString()!;
                            var errorMessage = errorResponse.RootElement.TryGetProperty("error", out var err) &&
                                             err.TryGetProperty("message", out var errMsg)
                                ? errMsg.GetString()
                                : "Unknown error";
                            errors[customId] = errorMessage ?? "Unknown error";
                        }
                        catch (JsonException)
                        {
                            _logger.LogWarning("Failed to parse error file line");
                        }
                    }
                }
            }

            return new BatchResult
            {
                BatchId = batchId,
                IsCompleted = status == "completed",
                Results = results,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get batch result for {BatchId}", batchId);
            throw;
        }
    }

    private static string MapBatchStatus(string status) => status switch
    {
        "validating" => "Validating",
        "in_progress" => "Processing",
        "completed" => "Completed",
        "failed" => "Failed",
        "expired" => "Expired",
        "cancelling" => "Cancelling",
        "cancelled" => "Cancelled",
        _ => status
    };

    private object CreateBatchRequest(DocumentRequest request)
    {
        var systemPrompt = $"""
            You are a document processing assistant. Your task is to extract information from the provided document 
            based on the user's instruction and return the result in the specified JSON schema format.

            IMPORTANT: Your response must be valid JSON that conforms to the following schema:
            {request.JsonSchema}

            Only return the JSON object, no additional text or explanation.
            """;

        var userPrompt = $"""
            Instruction: {request.Instruction}

            Document Content:
            {request.MarkdownContent}
            """;

        return new
        {
            custom_id = request.Id,
            method = "POST",
            url = "/chat/completions",
            body = new
            {
                model = request.ModelDeploymentId,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1,
                response_format = new { type = "json_object" }
            }
        };
    }
}
