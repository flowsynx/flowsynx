using FlowSynx.Core.Extensions;
using FlowSynx.Core.Services;
using FlowSynx.Core.Wrapper;
using FlowSynx.PluginCore;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Features.Plugins.Query.Details;

internal class PluginDetailsHandler : IRequestHandler<PluginDetailsRequest, Result<PluginDetailsResponse>>
{
    private readonly ILogger<PluginDetailsHandler> _logger;
    private readonly IPluginService _pluginService;

    public PluginDetailsHandler(ILogger<PluginDetailsHandler> logger, IPluginService pluginService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        _logger = logger;
        _pluginService = pluginService;
    }

    public async Task<Result<PluginDetailsResponse>> Handle(PluginDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var plugin = await _pluginService.Get(request.Type, cancellationToken);
            var specificationsType = plugin.SpecificationsType;
            var properties = specificationsType.GetProperties().Where(x => x.CanWrite).ToList();
            var specifications = properties
                .Select(property => new PluginDetailsSpecification
                {
                    Key = property.Name,
                    Type = property.PropertyType.GetPrimitiveType(),
                    Required = Attribute.IsDefined(property, typeof(RequiredMemberAttribute))
                }).ToList();

            var response = new PluginDetailsResponse
            {
                Id = plugin.Id,
                Type = plugin.Type,
                Description = plugin.Description,
                Specifications = specifications
            };

            return await Result<PluginDetailsResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<PluginDetailsResponse>.FailAsync(new List<string> { ex.Message });
        }
    }
}