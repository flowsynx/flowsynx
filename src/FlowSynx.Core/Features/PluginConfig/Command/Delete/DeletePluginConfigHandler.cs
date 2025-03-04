using FlowSynx.Core.Services;
using FlowSynx.Core.Wrapper;
using FlowSynx.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Features.Config.Command.Delete;

internal class DeletePluginConfigHandler : IRequestHandler<DeletePluginConfigRequest, Result<Unit>>
{
    private readonly ILogger<DeletePluginConfigHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;

    public DeletePluginConfigHandler(ILogger<DeletePluginConfigHandler> logger, ICurrentUserService currentUserService,
        IPluginConfigurationService pluginConfigurationService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginConfigurationService = pluginConfigurationService;
    }

    public async Task<Result<Unit>> Handle(DeletePluginConfigRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pluginConfiguration = await _pluginConfigurationService.Get(_currentUserService.UserId, request.Name, cancellationToken);
            if (pluginConfiguration == null)
                throw new Exception("The config not found");

            var deleteResult = await _pluginConfigurationService.Delete(pluginConfiguration, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}