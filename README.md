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
    "Endpoint": "https://your-openai-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DefaultDeploymentId": "gpt-4o",
    "ApiVersion": "2024-10-21"
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

## 🚀 Running the Solution

### Prerequisites
- .NET 8.0 SDK
- Azure Cosmos DB account (MongoDB API)
- Azure Document Intelligence resource
- Azure OpenAI resource

### Running Locally

1. Update configuration in all `appsettings.json` files

2. Run the API:
```bash
cd src/DocProcessor.Api
dotnet run
```

3. Run the Worker (for batch processing):
```bash
cd src/DocProcessor.Worker
dotnet run
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
