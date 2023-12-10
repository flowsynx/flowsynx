using EnsureThat;
using FlowSync.Core.Serialization;
using FlowSync.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowSync.Infrastructure.Serialization.Json;

public class NewtonsoftDeserializer : IDeserializer
{
    private readonly ILogger<NewtonsoftDeserializer> _logger;

    public NewtonsoftDeserializer(ILogger<NewtonsoftDeserializer> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public string ContentMineType => "application/json";

    public T? Deserialize<T>(string? input)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(input)) return JsonConvert.DeserializeObject<T>(input);

            _logger.LogWarning($"Input value can't be empty or null.");
            throw new DeserializerException(FlowSyncInfrastructureResource.NewtonsoftDeserializerValueCanNotBeEmpty);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in deserialize data. Message: {ex.Message}");
            throw new DeserializerException(ex.Message);
        }
    }
}