using FlowSynx.Core.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FlowSynx.Core.Models;
using FlowSynx.Core.Exceptions;

namespace FlowSynx.Infrastructure.Services;

public class JsonSerializer : IJsonSerializer
{
    private readonly ILogger<JsonSerializer> _logger;

    public JsonSerializer(ILogger<JsonSerializer> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public string ContentMineType => "application/json";

    public string Serialize(object? input)
    {
        return Serialize(input, new JsonSerializationConfiguration { Indented = false });
    }

    public string Serialize(object? input, JsonSerializationConfiguration configuration)
    {
        try
        {
            if (input is null)
            {
                _logger.LogWarning($"Input value can't be empty or null.");
                throw new JsonSerializerException(Resources.JsonSerializerValueCanNotBeEmpty);
            }

            var settings = new JsonSerializerSettings
            {
                Formatting = configuration.Indented ? Formatting.Indented : Formatting.None,
                ContractResolver = configuration.NameCaseInsensitive ? new DefaultContractResolver() : new CamelCasePropertyNamesContractResolver()
            };

            if (configuration.Converters is not null)
                settings.Converters = configuration.Converters.ConvertAll(item => (JsonConverter)item);

            return JsonConvert.SerializeObject(input, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in serializer data. Message: {ex.Message}");
            throw new JsonSerializerException(ex.Message);
        }
    }
}