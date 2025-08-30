using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Localizations;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Serialization;

public class JsonDeserializer : IJsonDeserializer
{
    private readonly ILogger<JsonDeserializer> _logger;
    private readonly ILocalization _localization;

    public JsonDeserializer(
        ILogger<JsonDeserializer> logger,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _localization = localization;
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
                    _localization.Get("JsonDeserializer_InputValueCanNotBeEmpty"));
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

            return JsonConvert.DeserializeObject<T>(CleanJson(input), settings)!;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.Serialization, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private string CleanJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        // Remove trailing commas before ] or }
        // Handles cases like:
        //   [1,2,3,]
        //   {"a":1,"b":2,}
        var withoutTrailingCommas = Regex.Replace(
            json,
            @",\s*(\]|\})",
            "$1"
        );

        return withoutTrailingCommas;
    }
}