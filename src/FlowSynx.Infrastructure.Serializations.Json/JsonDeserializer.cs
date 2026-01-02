using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Application.Core.Serializations;

namespace FlowSynx.Infrastructure.Serializations.Json;

public class JsonDeserializer : IDeserializer
{
    private readonly ILogger<JsonDeserializer> _logger;
    private readonly INormalizer _normalizer;

    public JsonDeserializer(
        ILogger<JsonDeserializer> logger,
        INormalizer normalizer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
    }

    public T Deserialize<T>(string? input)
    {
        return Deserialize<T>(input, new SerializationConfiguration());
    }

    public T Deserialize<T>(string? input, SerializationConfiguration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.DeserializerEmptyValue,
                    InfrastructureSerializationsResources.JsonDeserializer_InputValueCanNotBeEmpty);
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

            var sanitized = _normalizer.Normalize(input);
            return JsonConvert.DeserializeObject<T>(sanitized, settings)!;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.Serialization, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}
