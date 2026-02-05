using DocProcessor.Core.Configuration;
using DocProcessor.Core.Interfaces;
using DocProcessor.Infrastructure.Repositories;
using DocProcessor.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocProcessor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<AzureOpenAISettings>(configuration.GetSection(AzureOpenAISettings.SectionName));
        services.Configure<DocumentIntelligenceSettings>(configuration.GetSection(DocumentIntelligenceSettings.SectionName));
        services.Configure<CosmosDbSettings>(configuration.GetSection(CosmosDbSettings.SectionName));
        services.Configure<BatchProcessingSettings>(configuration.GetSection(BatchProcessingSettings.SectionName));

        // Repositories
        services.AddSingleton<IDocumentRequestRepository, DocumentRequestRepository>();
        services.AddSingleton<IBatchJobRepository, BatchJobRepository>();

        // Services
        services.AddSingleton<IDocumentIntelligenceService, DocumentIntelligenceService>();
        services.AddSingleton<ILlmService, LlmService>();
        services.AddSingleton<IDocumentProcessingService, DocumentProcessingService>();

        // HTTP Clients
        services.AddHttpClient<ICallbackService, CallbackService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IBatchLlmService, BatchLlmService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        return services;
    }
}
