using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Command.Delete;

internal class DeletePluginHandler : IRequestHandler<DeletePluginRequest, Result<Unit>>
{
    private readonly ILogger<DeletePluginHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginManager _pluginManager;

    public DeletePluginHandler(ILogger<DeletePluginHandler> logger, ICurrentUserService currentUserService,
        IPluginManager pluginManager)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginManager);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginManager = pluginManager;
    }

    public async Task<Result<Unit>> Handle(DeletePluginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAthenticationIsRequired, 
                    "Access is denied. Authentication is required.");

            await _pluginManager.Uninstall(request.Type, request.Version, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.AddConfigHandlerSuccessfullyAdded);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}