using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Application.Core.Serializations;

namespace FlowSynx.Infrastructure.Serializations.Json;

public class JsonSerializer : ISerializer
{
    private readonly ILogger<JsonSerializer> _logger;

    public JsonSerializer(
        ILogger<JsonSerializer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ContentMineType => "application/json";

    public string Serialize(object? input)
    {
        return Serialize(input, new SerializationConfiguration { Indented = false });
    }

    public string Serialize(object? input, SerializationConfiguration configuration)
    {
        try
        {
            if (input is null)
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.SerializerEmptyValue,
                                    InfrastructureSerializationsResources.JsonSerializer_InputValueCanNotBeEmpty);
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

            return JsonConvert.SerializeObject(input, settings);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.Serialization, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}