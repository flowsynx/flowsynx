using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Command.InstallPlugin;

internal class InstallPluginHandler : IRequestHandler<InstallPluginRequest, Result<Unit>>
{
    private readonly ILogger<InstallPluginHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginManager _pluginManager;

    public InstallPluginHandler(
        ILogger<InstallPluginHandler> logger, 
        ICurrentUserService currentUserService,
        IPluginManager pluginManager)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginManager);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginManager = pluginManager;
    }

    public async Task<Result<Unit>> Handle(InstallPluginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, Resources.Authentication_Access_Denied);

            await _pluginManager.InstallAsync(request.Type, request.Version, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.Feature_Plugin_Add_AddedSuccessfully);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}