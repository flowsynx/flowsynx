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

namespace FlowSynx.Application.Features.PluginConfig.Command.AddPluginConfig;

internal class AddPluginConfigHandler : IRequestHandler<AddPluginConfigRequest, Result<AddPluginConfigResponse>>
{
    private readonly ILogger<AddPluginConfigHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginSpecificationsService _pluginSpecificationsService;
    private readonly ILocalization _localization;

    public AddPluginConfigHandler(
        ILogger<AddPluginConfigHandler> logger, 
        IPluginService pluginService, 
        IPluginConfigurationService pluginConfigurationService, 
        ICurrentUserService currentUserService, 
        IPluginSpecificationsService pluginSpecificationsService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginSpecificationsService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _pluginService = pluginService;
        _pluginConfigurationService = pluginConfigurationService;
        _currentUserService = currentUserService;
        _pluginSpecificationsService = pluginSpecificationsService;
        _localization = localization;
    }

    public async Task<Result<AddPluginConfigResponse>> Handle(
        AddPluginConfigRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var pluginEntity = await _pluginService.Get(_currentUserService.UserId(), request.Type, 
                request.Version, cancellationToken);
            if (pluginEntity is null)
            {
                var message = _localization.Get("Features_PluginConfig_Add_PluginCouldNotBeFound", 
                    request.Type, request.Version);
                var errorMessage = new ErrorMessage((int)ErrorCode.PluginTypeNotFound, message);
                _logger.LogError(errorMessage.ToString());
                return await Result<AddPluginConfigResponse>.FailAsync(errorMessage.ToString());
            }

            var isPluginSpecificationsValid = _pluginSpecificationsService.Validate(request.Specifications, 
                pluginEntity.Specifications);
            if (!isPluginSpecificationsValid.Valid)
                return await Result<AddPluginConfigResponse>.FailAsync(isPluginSpecificationsValid.Messages!);
            
            var isPluginConfigurationExist = await _pluginConfigurationService.IsExist(_currentUserService.UserId(), 
                request.Name, cancellationToken);
            if (isPluginConfigurationExist)
            {
                var message = _localization.Get("Features_PluginConfig_Add_PluginConfigAlreadyExists", request.Name);
                var errorMessage = new ErrorMessage((int)ErrorCode.PluginTypeNotFound, message);
                _logger.LogWarning("The plugin config '{ConfigName}' already exists.", request.Name);
                return await Result<AddPluginConfigResponse>.FailAsync(errorMessage.ToString());
            }

            var pluginConfiguration = new PluginConfigurationEntity
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId(),
                Name = request.Name,
                Type = request.Type,
                Version = request.Version,
                Specifications = request.Specifications.ToPluginConfigurationSpecifications(),
            };
            await _pluginConfigurationService.Add(pluginConfiguration, cancellationToken);
            var response = new AddPluginConfigResponse 
            { 
                Id = pluginConfiguration.Id, 
                Name = pluginConfiguration.Name 
            };

            return await Result<AddPluginConfigResponse>.SuccessAsync(response,
                _localization.Get("Feature_PluginConfig_Add_AddedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, "FlowSynx exception caught while adding plugin configuration.");
            return await Result<AddPluginConfigResponse>.FailAsync(ex.Message);
        }
    }
}
