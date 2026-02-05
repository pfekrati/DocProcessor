using DocProcessor.Infrastructure;
using DocProcessor.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<BatchProcessingWorker>();
builder.Services.AddHostedService<BatchResultPollingWorker>();

var host = builder.Build();
host.Run();
