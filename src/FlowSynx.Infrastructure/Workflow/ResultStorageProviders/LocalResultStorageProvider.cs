using FlowSynx.Application.Serialization;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ResultStorageProviders;

public class LocalResultStorageProvider : IResultStorageProvider, IConfigurableResultStorage
{
    private string _basePath = "workflow-results";
    private readonly ILogger<LocalResultStorageProvider> _logger;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IJsonDeserializer _jsonDeserializer;
    private long _maxLimitSize;
    public LocalResultStorageProvider(
        ILogger<LocalResultStorageProvider> logger,
        IJsonSerializer jsonSerializer,
        IJsonDeserializer jsonDeserializer)
    {
        _logger = logger;
        _jsonSerializer = jsonSerializer;
        _jsonDeserializer = jsonDeserializer;
    }

    public string Type => "Local";

    public void Configure(Dictionary<string, string> configuration, long maxLimitSize)
    {
        _basePath = configuration.GetValueOrDefault("BasePath", "workflow-results");
        if (configuration.TryGetValue("MaxSizeInMB", out var maxSizeValue) &&
            long.TryParse(maxSizeValue, out var maxSizeMB))
        {
            _maxLimitSize = maxSizeMB;
        }
    }

    public async Task<string> SaveResultAsync(
        WorkflowExecutionContext executionContext,
        Guid workflowTaskId,
        object result,
        CancellationToken cancellationToken = default)
    {
        var directoryPath = Path.Combine(_basePath,
            executionContext.UserId,
            executionContext.WorkflowId.ToString(),
            executionContext.WorkflowExecutionId.ToString());

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var json = _jsonSerializer.Serialize(result);

        // Check size
        var sizeInBytes = System.Text.Encoding.UTF8.GetByteCount(json);
        if (sizeInBytes > _maxLimitSize)
        {
            _logger.LogWarning("Result size ({Size} bytes) exceeds the max allowed size ({MaxSize} bytes).", sizeInBytes, _maxLimitSize);
            throw new InvalidOperationException($"Result size exceeds maximum allowed size of {_maxLimitSize} bytes.");
        }

        var filePath = Path.Combine(directoryPath, $"{workflowTaskId}.json");
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        return filePath;
    }

    public async Task<T?> LoadResultAsync<T>(
        WorkflowExecutionContext executionContext,
        Guid workflowTaskId,
        CancellationToken cancellationToken = default)
    {
        var directoryPath = Path.Combine(_basePath,
            executionContext.UserId,
            executionContext.WorkflowId.ToString(),
            executionContext.WorkflowExecutionId.ToString());

        var filePath = Path.Combine(directoryPath, $"{workflowTaskId}.json");

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return _jsonDeserializer.Deserialize<T>(json);
    }
}