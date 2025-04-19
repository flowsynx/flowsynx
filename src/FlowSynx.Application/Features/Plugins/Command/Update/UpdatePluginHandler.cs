using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Command.Update;

internal class UpdatePluginHandler : IRequestHandler<UpdatePluginRequest, Result<Unit>>
{
    private readonly ILogger<UpdatePluginHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginManager _pluginManager;

    public UpdatePluginHandler(ILogger<UpdatePluginHandler> logger, ICurrentUserService currentUserService,
        IPluginManager pluginManager)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginManager);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginManager = pluginManager;
    }

    public async Task<Result<Unit>> Handle(UpdatePluginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAthenticationIsRequired, Resources.Authentication_Access_Denied);

            await _pluginManager.UpdateAsync(request.Type, request.OldVersion, request.NewVersion, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.Feature_Plugin_Update_UpdatedSuccessfully);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}