using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Command.InstallPlugin;

internal class InstallPluginHandler : IRequestHandler<InstallPluginRequest, Result<Unit>>
{
    private readonly ILogger<InstallPluginHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginManager _pluginManager;
    private readonly ILocalization _localization;

    public InstallPluginHandler(
        ILogger<InstallPluginHandler> logger, 
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

    public async Task<Result<Unit>> Handle(InstallPluginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            await _pluginManager.InstallAsync(request.Type, request.Version, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_Plugin_Install_AddedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}