using FlowSynx.Application.Model;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities.Plugin;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Services;

public class PluginSpecificationsService : IPluginSpecificationsService
{
    private readonly ILogger<PluginSpecificationsService> _logger;

    public PluginSpecificationsService(ILogger<PluginSpecificationsService> logger)
    {
        _logger = logger;
    }

    public PluginSpecificationsResult Validate(Dictionary<string, object?> inputSpecifications,
        List<PluginSpecification> pluginSpecifications)
    {
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

                var errorMessage = new ErrorMessage((int)ErrorCode.PluginNotFound, string.Format(Resources.SpecificationsRequiredMemberMustHaveValue, specification.Name));
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



        //Plugin plugin = await _pluginService.Get(type, cancellationToken);
        //var specificationsType = plugin.SpecificationsType;
        //var requiredProperties = specificationsType
        //    .GetProperties()
        //    .Where(prop => Attribute.IsDefined(prop, typeof(RequiredMemberAttribute)))
        //    .ToList();

        //if (!requiredProperties.Any())
        //    return new PluginSpecificationsResult { Valid = true };

        //var convertedSpecifications = ConvertKeysToLowerCase(specifications);

        //var messages = new List<string>();
        //foreach (var property in requiredProperties)
        //{
        //    if (convertedSpecifications == null || !convertedSpecifications.Any() || !ContainsKey(convertedSpecifications, property.Name))
        //    {
        //        var specs = string.Join(", ", requiredProperties.Select(p => p.Name));
        //        return new PluginSpecificationsResult { 
        //            Valid = false, 
        //            Message = string.Format(Resources.SpecificationsMustHaveValue, specs) 
        //        };
        //    }

        //    var value = convertedSpecifications[property.Name.ToLower()];
        //    var valid = true;

        //    switch (value)
        //    {
        //        case null:
        //        case string when string.IsNullOrEmpty(value.ToString()):
        //            valid = false;
        //            break;
        //    }

        //    if (valid) continue;

        //    return new PluginSpecificationsResult
        //    {
        //        Valid = false,
        //        Message = string.Format(Resources.SpecificationsRequiredMemberMustHaveValue, property.Name)
        //    };
        //}

        //return new PluginSpecificationsResult { Valid = true };
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