using FlowSynx.Application.Extensions;
using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Domain.Plugin;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.PluginConfig.Command.UpdatePluginConfig;

internal class UpdatePluginConfigHandler : IRequestHandler<UpdatePluginConfigRequest, Result<Unit>>
{
    private readonly ILogger<UpdatePluginConfigHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginService _pluginService;
    private readonly IPluginSpecificationsService _pluginSpecificationsService;
    private readonly ILocalization _localization;

    public UpdatePluginConfigHandler(
        ILogger<UpdatePluginConfigHandler> logger, 
        ICurrentUserService currentUserService,
        IPluginConfigurationService pluginConfigurationService, 
        IPluginService pluginService, 
        IPluginSpecificationsService pluginSpecificationsService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginConfigurationService = pluginConfigurationService;
        _pluginService = pluginService;
        _pluginSpecificationsService = pluginSpecificationsService;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(
        UpdatePluginConfigRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var configId = Guid.Parse(request.ConfigId);
            var pluginConfiguration = await _pluginConfigurationService.Get(_currentUserService.UserId(), 
                configId, cancellationToken);
            if (pluginConfiguration == null)
            {
                var message = _localization.Get("Feature_PluginConfig_Update_ConfigIdNotFound", request.ConfigId);
                throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, message);
            }

            if (!string.Equals(request.Name, pluginConfiguration.Name, StringComparison.OrdinalIgnoreCase))
            {
                var ispluginConfigExist = await _pluginConfigurationService.IsExist(_currentUserService.UserId(), 
                    request.Name, cancellationToken);
                if (ispluginConfigExist)
                {
                    var message = _localization.Get("Features_PluginConfig_Update_PluginConfigAlreadyExists", 
                        request.Name);
                    var errorMessage = new ErrorMessage((int)ErrorCode.PluginConfigurationIsAlreadyExist, message);
                    _logger.LogWarning(errorMessage.ToString());
                    throw new FlowSynxException(errorMessage);
                }
            }

            var pluginEntity = await _pluginService.Get(_currentUserService.UserId(), request.Type, 
                request.Version, cancellationToken);
            if (pluginEntity is null)
            {
                var message = _localization.Get("Features_PluginConfig_Update_PluginCouldNotBeFound", 
                    request.Type, request.Version);
                throw new FlowSynxException((int)ErrorCode.PluginTypeNotFound, message);
            }

            var isPluginSpecificationsValid = _pluginSpecificationsService.Validate(request.Specifications, 
                pluginEntity.Specifications);
            if (!isPluginSpecificationsValid.Valid)
                return await Result<Unit>.FailAsync(isPluginSpecificationsValid.Messages!);

            pluginConfiguration.Name = request.Name;
            pluginConfiguration.Type = request.Type;
            pluginConfiguration.Version = request.Version;
            pluginConfiguration.Specifications = request.Specifications.ToPluginConfigurationSpecifications();

            await _pluginConfigurationService.Update(pluginConfiguration, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_PluginConfig_Update_UpdatedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, "FlowSynx exception caught while updating plugin config '{ConfigId}'.", request.ConfigId);
            return await Result<Unit>.FailAsync(ex.Message);
        }
    }
}
