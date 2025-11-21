using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.PluginConfig.Command.DeletePluginConfig;

internal class DeletePluginConfigHandler : IRequestHandler<DeletePluginConfigRequest, Result<Unit>>
{
    private readonly ILogger<DeletePluginConfigHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ILocalization _localization;
    private readonly ICurrentUserService _currentUserService;

    public DeletePluginConfigHandler(
        ILogger<DeletePluginConfigHandler> logger, 
        ICurrentUserService currentUserService, 
        IPluginConfigurationService pluginConfigurationService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginConfigurationService = pluginConfigurationService;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(
        DeletePluginConfigRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var configId = Guid.Parse(request.ConfigId);
            var pluginConfiguration = await _pluginConfigurationService.Get(_currentUserService.UserId(), configId, 
                cancellationToken);
            if (pluginConfiguration == null)
            {
                var message = string.Format(_localization.Get("Feature_PluginConfig_Delete_ConfigIdNotFound", configId));
                throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, message);
            }

            await _pluginConfigurationService.Delete(pluginConfiguration, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_PluginConfig_Delete_DeletedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}
