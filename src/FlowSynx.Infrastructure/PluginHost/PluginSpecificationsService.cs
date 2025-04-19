using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost;
using FlowSynx.Domain.Plugin;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginSpecificationsService : IPluginSpecificationsService
{
    private readonly ILogger<PluginSpecificationsService> _logger;

    public PluginSpecificationsService(ILogger<PluginSpecificationsService> logger)
    {
        _logger = logger;
    }

    public PluginSpecificationsResult Validate(Dictionary<string, object?>? inputSpecifications,
        List<PluginSpecification>? pluginSpecifications)
    {
        inputSpecifications ??= new Dictionary<string, object?>();
        pluginSpecifications ??= new List<PluginSpecification>();
        var errors = new List<string>();
        var convertedSpecifications = ConvertKeysToLowerCase(inputSpecifications);

        foreach (var specification in pluginSpecifications)
        {
            if (specification.IsRequired && !inputSpecifications.ContainsKey(specification.Name))
            {
                var value = inputSpecifications[specification.Name.ToLower()];
                var valid = true;

                switch (value)
                {
                    case null:
                    case string when string.IsNullOrEmpty(value.ToString()):
                        valid = false;
                        break;
                }

                if (valid) continue;

                var errorMessage = new ErrorMessage((int)ErrorCode.PluginNotFound, 
                    string.Format(Resources.SpecificationsRequiredMemberMustHaveValue, specification.Name));
                _logger.LogError(errorMessage.ToString());
                errors.Add(errorMessage.ToString());
                continue;
            }
        }

        return new PluginSpecificationsResult
        {
            Valid = errors.Count == 0,
            Messages = errors
        };
    }

    private Dictionary<string, object?>? ConvertKeysToLowerCase(Dictionary<string, object?>? dictionaries)
    {
        var convertedDictionary = new Dictionary<string, object?>();

        if (dictionaries == null)
            return convertedDictionary;

        foreach (string key in dictionaries.Keys)
        {
            convertedDictionary.Add(key.ToLower(), dictionaries[key]);
        }
        return convertedDictionary;
    }
}