using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Domain.Plugin;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Query.PluginDetails;

internal class PluginDetailsHandler : IRequestHandler<PluginDetailsRequest, Result<PluginDetailsResponse>>
{
    private readonly ILogger<PluginDetailsHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public PluginDetailsHandler(
        ILogger<PluginDetailsHandler> logger, 
        IPluginService pluginService,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _pluginService = pluginService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<PluginDetailsResponse>> Handle(PluginDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var pluginId = Guid.Parse(request.PluginId);
            var plugin = await _pluginService.Get(_currentUserService.UserId(), pluginId, cancellationToken);
            if (plugin is null)
            {
                var message = _localization.Get("Features_Plugin_Details_PluginCouldNotBeFound", pluginId);
                throw new FlowSynxException((int)ErrorCode.PluginNotFound, message);
            }

            var specifications = plugin.Specifications?
                .Select(property => new PluginDetailsSpecification
                {
                    Key = property.Name,
                    Type = property.Type,
                    IsRequired = property.IsRequired
                }).ToList();

            var response = new PluginDetailsResponse
            {
                Id = plugin.Id,
                Type = plugin.Type,
                Version = plugin.Version,
                Description = plugin.Description,
                Specifications = specifications
            };

            return await Result<PluginDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<PluginDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}
