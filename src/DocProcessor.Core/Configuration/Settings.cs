namespace DocProcessor.Core.Configuration;

public class AzureOpenAISettings
{
    public const string SectionName = "AzureOpenAI";

    // Real-time API settings
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-10-21";

    // Batch API settings
    public string BatchEndpoint { get; set; } = string.Empty;
    public string BatchApiKey { get; set; } = string.Empty;

    public bool UseManagedIdentity { get; set; }
}

public class DocumentIntelligenceSettings
{
    public const string SectionName = "DocumentIntelligence";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool UseManagedIdentity { get; set; }
}

public class CosmosDbSettings
{
    public const string SectionName = "CosmosDb";

    public string ConnectionString { get; set; } = string.Empty;
    public string AccountEndpoint { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "DocProcessor";
    public string RequestsCollectionName { get; set; } = "Requests";
    public string BatchJobsCollectionName { get; set; } = "BatchJobs";
    public bool UseManagedIdentity { get; set; }
}

public class BatchProcessingSettings
{
    public const string SectionName = "BatchProcessing";
    
    public int QueueSizeThreshold { get; set; } = 100;
    public int ProcessingIntervalMinutes { get; set; } = 15;
    public int MaxRetryCount { get; set; } = 3;
    public int BatchCheckIntervalMinutes { get; set; } = 5;
}
