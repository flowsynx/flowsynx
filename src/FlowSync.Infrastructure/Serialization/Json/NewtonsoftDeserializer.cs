using FlowSync.Core.Exceptions;
using FlowSync.Core.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowSync.Infrastructure.Serialization.Json;

public class NewtonsoftDeserializer : IDeserializer
{
    private readonly ILogger<NewtonsoftDeserializer> _logger;

    public NewtonsoftDeserializer(ILogger<NewtonsoftDeserializer> logger)
    {
        _logger = logger;
    }

    public string ContentMineType => "application/json";

    public T? Deserialize<T>(string? input)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(input)) return JsonConvert.DeserializeObject<T>(input);

            _logger.LogWarning($"Input value can't be empty or null.");
            throw new ArgumentNullException(nameof(input), "Input value can't be empty or null.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in deserialize data. Message: {ex.Message}");
            throw new DeserializerException(ex.Message);
        }
    }
}