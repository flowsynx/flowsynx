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
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
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
            }
        }

        return new PluginSpecificationsResult
        {
            Valid = errors.Count == 0,
            Messages = errors
        };
    }
}