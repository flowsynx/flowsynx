using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Infrastructure.Serialization;

public class JsonSerializer : IJsonSerializer
{
    private readonly ILogger<JsonSerializer> _logger;
    private readonly ILocalization _localization;

    public JsonSerializer(
        ILogger<JsonSerializer> logger,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _localization = localization;
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
                var errorMessage = new ErrorMessage((int)ErrorCode.SerializerEmptyValue,
                                    _localization.Get("JsonSerializer_InputValueCanNotBeEmpty"));
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