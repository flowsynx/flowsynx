using FlowSynx.Core.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FlowSynx.Core.Models;
using FlowSynx.IO.Exceptions;

namespace FlowSynx.Infrastructure.Services;

public class JsonDeserializer : IJsonDeserializer
{
    private readonly ILogger<JsonDeserializer> _logger;

    public JsonDeserializer(ILogger<JsonDeserializer> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public T Deserialize<T>(string? input)
    {
        return Deserialize<T>(input, new JsonSerializationConfiguration { });
    }

    public T Deserialize<T>(string? input, JsonSerializationConfiguration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning($"Input value can't be empty or null.");
                throw new JsonDeserializerException(Resources.JsonDeserializerValueCanNotBeEmpty);
            }

            var settings = new JsonSerializerSettings
            {
                Formatting = configuration.Indented ? Formatting.Indented : Formatting.None,
                ContractResolver = configuration.NameCaseInsensitive ? new DefaultContractResolver() : new CamelCasePropertyNamesContractResolver()
            };

            if (configuration.Converters is not null)
                settings.Converters = configuration.Converters.ConvertAll(item => (JsonConverter)item);
            
            return JsonConvert.DeserializeObject<T>(input, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in deserialize data. Message: {ex.Message}");
            throw new JsonDeserializerException(ex.Message);
        }
    }
}