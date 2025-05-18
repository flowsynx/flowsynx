using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Infrastructure.Serialization;

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
        return Deserialize<T>(input, new JsonSerializationConfiguration());
    }

    public T Deserialize<T>(string? input, JsonSerializationConfiguration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.DeserializerEmptyValue, 
                    Localization.Get("JsonDeserializer_InputValueCanNotBeEmpty"));
                _logger.LogError(errorMessage.ToString());
                throw new FlowSynxException(errorMessage);
            }

            var settings = new JsonSerializerSettings
            {
                Formatting = configuration.Indented ? Formatting.Indented : Formatting.None,
                ContractResolver = configuration.NameCaseInsensitive 
                                   ? new DefaultContractResolver() 
                                   : new CamelCasePropertyNamesContractResolver()
            };

            if (configuration.Converters is not null)
                settings.Converters = configuration.Converters.ConvertAll(item => (JsonConverter)item);

            return JsonConvert.DeserializeObject<T>(input, settings)!;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.Serialization, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}