using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Command.UpdatePlugin;

internal class UpdatePluginHandler : IRequestHandler<UpdatePluginRequest, Result<Unit>>
{
    private readonly ILogger<UpdatePluginHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginManager _pluginManager;
    private readonly ILocalization _localization;

    public UpdatePluginHandler(
        ILogger<UpdatePluginHandler> logger, 
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

    public async Task<Result<Unit>> Handle(UpdatePluginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            await _pluginManager.UpdateAsync(request.Type, request.OldVersion, request.NewVersion, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_Plugin_Update_UpdatedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}