using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Command.UninstallPlugin;

internal class UninstallPluginHandler : IRequestHandler<UninstallPluginRequest, Result<Unit>>
{
    private readonly ILogger<UninstallPluginHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginManager _pluginManager;
    private readonly ILocalization _localization;

    public UninstallPluginHandler(
        ILogger<UninstallPluginHandler> logger, 
        ICurrentUserService currentUserService,
        IPluginManager pluginManager,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginManager);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginManager = pluginManager;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(UninstallPluginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            await _pluginManager.Uninstall(request.Type, request.Version, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_Plugin_Uninstall_DeletedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}