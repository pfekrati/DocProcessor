using System.Text;
using System.Text.Json;
using DocProcessor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocProcessor.Infrastructure.Services;

public class CallbackService : ICallbackService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CallbackService> _logger;

    public CallbackService(
        HttpClient httpClient,
        ILogger<CallbackService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendCallbackAsync(string callbackUrl, string requestId, string result)
    {
        _logger.LogInformation("Sending callback to {CallbackUrl} for request {RequestId}", callbackUrl, requestId);

        try
        {
            var payload = new
            {
                requestId,
                status = "Completed",
                result,
                completedAt = DateTime.UtcNow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(callbackUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent callback to {CallbackUrl}", callbackUrl);
            }
            else
            {
                _logger.LogWarning("Callback to {CallbackUrl} returned status code {StatusCode}", 
                    callbackUrl, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send callback to {CallbackUrl}", callbackUrl);
        }
    }
}
