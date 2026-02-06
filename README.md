# DocProcessor

A comprehensive .NET 8 document processing solution that leverages Azure AI services (Azure Document Intelligence and Azure OpenAI) to extract structured data from documents. The system supports both real-time and batch processing modes, making it suitable for various document processing workloads from single document extraction to high-volume batch operations.

## 🛠️ Technology Stack

- **.NET 8** - Runtime and framework
- **Blazor Server** - Admin portal UI
- **Azure Cosmos DB** (MongoDB API) - Document storage
- **Azure Document Intelligence** - Document-to-markdown conversion
- **Azure OpenAI** - LLM processing with batch API support

## 📄 Supported Document Types

| Type | Extensions | Description |
|------|------------|-------------|
| PDF | `.pdf` | PDF documents |
| Word | `.docx`, `.doc` | Microsoft Word documents |
| Image | `.png`, `.jpg`, `.jpeg`, `.tiff`, `.bmp` | Image files with text |
| HTML | `.html`, `.htm` | Web pages |
| Text | `.txt` | Plain text files |

## 📊 Processing Status Flow

Documents progress through the following statuses:

```
Pending → Queued → Processing → Completed
                ↘ BatchSubmitted ↗
                        ↓
                      Failed
```

| Status | Description |
|--------|-------------|
| `Pending` | Request received, awaiting markdown conversion |
| `Queued` | Document converted to markdown, queued for LLM processing |
| `Processing` | Currently being processed by LLM |
| `BatchSubmitted` | Submitted to OpenAI batch API |
| `Completed` | Processing completed successfully |
| `Failed` | Processing failed with error |

## 🏗️ Architecture

The solution consists of 5 projects:

- **DocProcessor.Core** - Domain models, interfaces, DTOs, and configuration classes
- **DocProcessor.Infrastructure** - Repository implementations, Azure service integrations (Document Intelligence, OpenAI)
- **DocProcessor.Api** - REST API for document processing
- **DocProcessor.Worker** - Background worker services for batch processing and result polling
- **DocProcessor.AdminPortal** - Blazor Server admin portal for monitoring and management

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│  DocProcessor   │     │   DocProcessor   │     │    DocProcessor     │
│      Api        │────▶│      Core        │◀────│   Infrastructure    │
└─────────────────┘     └──────────────────┘     └─────────────────────┘
        │                        ▲                         │
        │                        │                         │
        ▼                        │                         ▼
┌─────────────────┐              │              ┌─────────────────────┐
│  DocProcessor   │──────────────┘              │   Azure Services    │
│     Worker      │                             │  • Document Intel   │
└─────────────────┘                             │  • OpenAI           │
        │                                       │  • Cosmos DB        │
        ▼                                       └─────────────────────┘
┌─────────────────┐
│  DocProcessor   │
│   AdminPortal   │
└─────────────────┘
```

## ✨ Features

### ⚡ Real-time Processing
- Upload a document with an instruction and JSON schema
- Document is converted to markdown using Azure Document Intelligence
- Markdown content is processed by Azure OpenAI to extract data according to the schema
- Response is returned immediately

### 📦 Batch Processing
- Queue documents for batch processing
- Receive a request ID to check status later
- Optional webhook callback URL for completion notification
- Two triggers for batch submission:
  - **Queue threshold**: When queue size reaches configurable threshold
  - **Time interval**: Periodic processing at configurable intervals

### 🖥️ Admin Portal
- Dashboard with statistics
- View all requests with filtering by status
- View queue status and pending requests
- Monitor batch jobs and their progress
- Detailed view of individual requests and results

## ⚙️ Configuration

Update `appsettings.json` in each project with your Azure credentials:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-openai-resource.cognitiveservices.azure.com/",
    "ApiKey": "your-api-key",
    "ApiVersion": "2024-05-01-preview",
    "BatchEndpoint": "https://your-openai-resource.cognitiveservices.azure.com/",
    "BatchApiKey": "your-batch-api-key"
  },
  "DocumentIntelligence": {
    "Endpoint": "https://your-doc-intelligence.cognitiveservices.azure.com/",
    "ApiKey": "your-api-key"
  },
  "CosmosDb": {
    "ConnectionString": "mongodb://your-cosmos-account:your-key@your-cosmos-account.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@your-cosmos-account@",
    "DatabaseName": "DocProcessor",
    "RequestsCollectionName": "Requests",
    "BatchJobsCollectionName": "BatchJobs"
  },
  "BatchProcessing": {
    "QueueSizeThreshold": 100,
    "ProcessingIntervalMinutes": 15,
    "MaxRetryCount": 3,
    "BatchCheckIntervalMinutes": 5
  }
}
```

### Configuration Options

| Section | Setting | Description |
|---------|---------|-------------|
| **AzureOpenAI** | `Endpoint` | Azure OpenAI service endpoint for real-time processing |
| | `ApiKey` | API key for Azure OpenAI |
| | `ApiVersion` | API version (e.g., `2024-05-01-preview`) |
| | `BatchEndpoint` | Endpoint for batch API operations |
| | `BatchApiKey` | API key for batch operations |
| **DocumentIntelligence** | `Endpoint` | Azure Document Intelligence endpoint |
| | `ApiKey` | API key for Document Intelligence |
| **CosmosDb** | `ConnectionString` | MongoDB connection string for Cosmos DB |
| | `DatabaseName` | Database name |
| | `RequestsCollectionName` | Collection for document requests |
| | `BatchJobsCollectionName` | Collection for batch job tracking |
| **BatchProcessing** | `QueueSizeThreshold` | Number of queued items to trigger batch submission |
| | `ProcessingIntervalMinutes` | Interval for checking queue |
| | `MaxRetryCount` | Maximum retry attempts for failed operations |
| | `BatchCheckIntervalMinutes` | Interval for polling batch status |

## 📡 API Endpoints

### POST /api/documents/process
Process a document (form-data upload)

**Parameters:**
- `Document` (file): The document to process
- `Instruction` (string): What to extract from the document
- `JsonSchema` (string): Expected output JSON schema
- `ModelDeploymentId` (optional): Azure OpenAI deployment ID
- `ProcessingMode` (optional): `0` for RealTime, `1` for Batch
- `CallbackUrl` (optional): URL for webhook notification (batch only)

### POST /api/documents/process/json
Process a document (JSON body with base64 content)

**Body:**
```json
{
  "documentBase64": "base64-encoded-content",
  "documentName": "document.pdf",
  "instruction": "Extract invoice details",
  "jsonSchema": "{\"type\":\"object\",\"properties\":{\"invoiceNumber\":{\"type\":\"string\"}}}",
  "modelDeploymentId": "gpt-4o",
  "processingMode": 0,
  "callbackUrl": "https://your-webhook.com/callback"
}
```

### GET /api/documents/status/{requestId}
Get the status of a processing request

### GET /api/documents/result/{requestId}
Get the result of a completed request

### Admin Endpoints

- `GET /api/admin/requests` - List all requests (with pagination)
- `GET /api/admin/batches` - List all batch jobs
- `GET /api/admin/stats` - Get queue statistics
- `GET /api/admin/requests/{requestId}` - Get request details

## 🚀 Deploy to Azure

### One-Click Deployment with Azure Developer CLI (azd)

The fastest way to deploy the full solution to Azure:

```bash
# Install azd if you haven't already
# https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd

# Clone and deploy
azd init
azd up
```

During `azd up`, you will be prompted for:

| Parameter | Description | Default |
|-----------|-------------|---------|
| `AZURE_LOCATION` | Azure region for all resources | *(prompted)* |
| `AZURE_APP_SERVICE_PLAN_SKU` | App Service Plan SKU | `P0v3` |
| `DEPLOY_AZURE_OPENAI` | Deploy a new Azure OpenAI resource | `true` |
| `EXISTING_OPENAI_ENDPOINT` | Endpoint of existing Azure OpenAI (if not deploying new) | `""` |
| `EXISTING_OPENAI_API_KEY` | API key of existing Azure OpenAI | `""` |
| `AZURE_OPENAI_API_VERSION` | Azure OpenAI API version for real-time calls | `2024-05-01-preview` |
| `EXISTING_OPENAI_BATCH_ENDPOINT` | Batch endpoint of existing Azure OpenAI | `""` |
| `EXISTING_OPENAI_BATCH_API_KEY` | Batch API key of existing Azure OpenAI | `""` |
| `DEPLOY_DOCUMENT_INTELLIGENCE` | Deploy a new Document Intelligence resource | `true` |
| `EXISTING_DOC_INTELLIGENCE_ENDPOINT` | Endpoint of existing Document Intelligence (if not deploying new) | `""` |
| `EXISTING_DOC_INTELLIGENCE_API_KEY` | API key of existing Document Intelligence | `""` |

#### Using Existing Azure AI Resources

If you already have Azure OpenAI and/or Document Intelligence resources, set the deployment flags to `false` and provide the connection details:

```bash
azd env set DEPLOY_AZURE_OPENAI false
azd env set EXISTING_OPENAI_ENDPOINT "https://your-openai.openai.azure.com/"
azd env set EXISTING_OPENAI_API_KEY "your-key"
azd env set AZURE_OPENAI_API_VERSION "2024-05-01-preview"
azd env set EXISTING_OPENAI_BATCH_ENDPOINT "https://your-batch-openai.openai.azure.com/"
azd env set EXISTING_OPENAI_BATCH_API_KEY "your-batch-key"

azd env set DEPLOY_DOCUMENT_INTELLIGENCE false
azd env set EXISTING_DOC_INTELLIGENCE_ENDPOINT "https://your-doc-intel.cognitiveservices.azure.com/"
azd env set EXISTING_DOC_INTELLIGENCE_API_KEY "your-key"

azd up
```

### Deployed Resources

| Resource | Description |
|----------|-------------|
| **Resource Group** | `rg-{env-name}` |
| **Cosmos DB** (MongoDB API, Serverless) | Database with `Requests` and `BatchJobs` collections |
| **App Service Plan** | Hosts all three apps (default SKU: P0v3) |
| **Web App** (Admin Portal) | Blazor Server admin dashboard |
| **API App** | REST API for document processing |
| **Function App** | Timer-triggered batch processing functions |
| **Storage Account** | Required by Azure Functions runtime |
| **Application Insights** | Monitoring and diagnostics |
| **Log Analytics Workspace** | Centralized logging |
| **Azure OpenAI** *(optional)* | LLM processing |
| **Document Intelligence** *(optional)* | Document-to-markdown conversion |

---

## 🖥️ Running Locally

### Prerequisites
- .NET 8.0 SDK
- Azure Cosmos DB account (MongoDB API)
- Azure Document Intelligence resource
- Azure OpenAI resource

### Setup

1. Update configuration in all `appsettings.json` files

2. Run the API:
```bash
cd src/DocProcessor.Api
dotnet run
```

3. Run the Functions (for batch processing):
```bash
cd src/DocProcessor.Functions
func start
```

4. Run the Admin Portal:
```bash
cd src/DocProcessor.AdminPortal
dotnet run
```

## 💡 Example Usage

### Real-time Processing (cURL)

```bash
curl -X POST "https://localhost:5001/api/documents/process" \
  -F "document=@invoice.pdf" \
  -F "instruction=Extract the invoice number, date, and total amount" \
  -F "jsonSchema={\"type\":\"object\",\"properties\":{\"invoiceNumber\":{\"type\":\"string\"},\"date\":{\"type\":\"string\"},\"totalAmount\":{\"type\":\"number\"}}}" \
  -F "processingMode=0"
```

### Batch Processing

```bash
curl -X POST "https://localhost:5001/api/documents/process" \
  -F "document=@invoice.pdf" \
  -F "instruction=Extract the invoice details" \
  -F "jsonSchema={\"type\":\"object\",\"properties\":{\"invoiceNumber\":{\"type\":\"string\"}}}" \
  -F "processingMode=1" \
  -F "callbackUrl=https://myapp.com/webhook"
```

Response:
```json
{
  "requestId": "abc123...",
  "status": 1,
  "createdAt": "2024-01-15T10:00:00Z"
}
```

### Check Status

```bash
curl "https://localhost:5001/api/documents/status/abc123..."
```

### Get Result

```bash
curl "https://localhost:5001/api/documents/result/abc123..."
```

## 🔔 Webhook Callback

When batch processing completes, a POST request is sent to the callback URL:

```json
{
  "requestId": "abc123...",
  "status": "Completed",
  "result": "{\"invoiceNumber\":\"INV-001\",\"date\":\"2024-01-15\",\"totalAmount\":1500.00}",
  "completedAt": "2024-01-15T10:05:00Z"
}
```

## 📄 License

This project is licensed under the MIT License.
