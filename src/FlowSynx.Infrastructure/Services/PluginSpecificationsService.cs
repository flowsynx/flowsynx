using FlowSynx.Application.Model;
using FlowSynx.Application.Services;
using FlowSynx.Infrastructure;
using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.Services;

public class PluginSpecificationsService : IPluginSpecificationsService
{
    private readonly IPluginService _pluginService;

    public PluginSpecificationsService(IPluginService pluginService)
    {
        _pluginService = pluginService;
    }

    public async Task<PluginSpecificationsResult> Validate(string type, Dictionary<string, object?>? specifications, 
        CancellationToken cancellationToken)
    {
        Plugin plugin = await _pluginService.Get(type, cancellationToken);
        var specificationsType = plugin.SpecificationsType;
        var requiredProperties = specificationsType
            .GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(RequiredMemberAttribute)))
            .ToList();

        if (!requiredProperties.Any())
            return new PluginSpecificationsResult { Valid = true };

        var convertedSpecifications = ConvertKeysToLowerCase(specifications);

        var messages = new List<string>();
        foreach (var property in requiredProperties)
        {
            if (convertedSpecifications == null || !convertedSpecifications.Any() || !ContainsKey(convertedSpecifications, property.Name))
            {
                var specs = string.Join(", ", requiredProperties.Select(p => p.Name));
                return new PluginSpecificationsResult { 
                    Valid = false, 
                    Message = string.Format(Resources.SpecificationsMustHaveValue, specs) 
                };
            }

            var value = convertedSpecifications[property.Name.ToLower()];
            var valid = true;

            switch (value)
            {
                case null:
                case string when string.IsNullOrEmpty(value.ToString()):
                    valid = false;
                    break;
            }

            if (valid) continue;

            return new PluginSpecificationsResult
            {
                Valid = false,
                Message = string.Format(Resources.SpecificationsRequiredMemberMustHaveValue, property.Name)
            };
        }

        return new PluginSpecificationsResult { Valid = true };
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

    private bool ContainsKey(Dictionary<string, object?> dictionary, string value)
    {
        return dictionary.Keys.Any(k => string.Equals(k, value, StringComparison.CurrentCultureIgnoreCase));
    }
}