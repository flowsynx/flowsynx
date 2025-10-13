using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost;
using FlowSynx.Domain.Plugin;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginSpecificationsService : IPluginSpecificationsService
{
    private readonly ILogger<PluginSpecificationsService> _logger;
    private readonly ILocalization _localization;

    public PluginSpecificationsService(
        ILogger<PluginSpecificationsService> logger, 
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _localization = localization;
    }

    public PluginSpecificationsResult Validate(Dictionary<string, object?>? inputSpecifications,
        List<PluginSpecification>? pluginSpecifications)
    {
        inputSpecifications ??= new Dictionary<string, object?>();
        pluginSpecifications ??= new List<PluginSpecification>();
        var errors = new List<string>();

        foreach (var specification in pluginSpecifications)
        {
            if (specification.IsRequired && !inputSpecifications.ContainsKey(specification.Name))
            {
                var value = inputSpecifications[specification.Name.ToLowerInvariant()];
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
                    _localization.Get("SpecificationsRequiredMemberMustHaveValue", specification.Name));
                _logger.LogError(errorMessage.ToString());
                errors.Add(errorMessage.ToString());
            }
        }

        return new PluginSpecificationsResult
        {
            Valid = errors.Count == 0,
            Messages = errors
        };
    }
}