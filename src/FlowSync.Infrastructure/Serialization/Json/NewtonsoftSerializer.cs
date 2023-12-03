using EnsureThat;
using FlowSync.Core.Serialization;
using FlowSync.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowSync.Infrastructure.Serialization.Json;

public class NewtonsoftSerializer : ISerializer
{
    private readonly ILogger<NewtonsoftSerializer> _logger;

    public NewtonsoftSerializer(ILogger<NewtonsoftSerializer> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public string ContentMineType => "application/json";

    public string Serialize(object? input)
    {
        try
        {
            if (input is not null) return JsonConvert.SerializeObject(input);

            _logger.LogWarning($"Input value can't be empty or null.");
            throw new SerializerException("Input value can't be empty or null.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in serializer data. Message: {ex.Message}");
            throw new SerializerException(ex.Message);
        }
    }
}