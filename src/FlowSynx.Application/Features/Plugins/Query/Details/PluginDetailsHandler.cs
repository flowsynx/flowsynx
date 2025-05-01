using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Plugin;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Query.Details;

internal class PluginDetailsHandler : IRequestHandler<PluginDetailsRequest, Result<PluginDetailsResponse>>
{
    private readonly ILogger<PluginDetailsHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly ICurrentUserService _currentUserService;

    public PluginDetailsHandler(ILogger<PluginDetailsHandler> logger, IPluginService pluginService,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _pluginService = pluginService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PluginDetailsResponse>> Handle(PluginDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAthenticationIsRequired, Resources.Authentication_Access_Denied);

            var pluginId = Guid.Parse(request.Id);
            var plugin = await _pluginService.Get(_currentUserService.UserId, pluginId, cancellationToken);
            if (plugin is null)
            {
                var message = string.Format(Resources.Features_Plugin_Details_PluginCouldNotBeFound, pluginId);
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