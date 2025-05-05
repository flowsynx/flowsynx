using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.PluginConfig.Command.DeletePluginConfig;

internal class DeletePluginConfigHandler : IRequestHandler<DeletePluginConfigRequest, Result<Unit>>
{
    private readonly ILogger<DeletePluginConfigHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;

    public DeletePluginConfigHandler(
        ILogger<DeletePluginConfigHandler> logger, 
        ICurrentUserService currentUserService, 
        IPluginConfigurationService pluginConfigurationService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginConfigurationService = pluginConfigurationService;
    }

    public async Task<Result<Unit>> Handle(
        DeletePluginConfigRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, 
                    Resources.Authentication_Access_Denied);

            var configId = Guid.Parse(request.Id);
            var pluginConfiguration = await _pluginConfigurationService.Get(_currentUserService.UserId, configId, 
                cancellationToken);
            if (pluginConfiguration == null)
            {
                var message = string.Format(Resources.Feature_PluginConfig_Delete_ConfigIdNotFound, configId);
                throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, message);
            }

            await _pluginConfigurationService.Delete(pluginConfiguration, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.Feature_PluginConfig_Delete_DeletedSuccessfully);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}