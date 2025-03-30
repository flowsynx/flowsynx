using FlowSynx.Application.Extensions;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Entities.PluginConfig;
using FlowSynx.Domain.Interfaces;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.PluginConfig.Command.Add;

internal class AddPluginConfigHandler : IRequestHandler<AddPluginConfigRequest, Result<AddPluginConfigResponse>>
{
    private readonly ILogger<AddPluginConfigHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginSpecificationsService _pluginSpecificationsService;

    public AddPluginConfigHandler(ILogger<AddPluginConfigHandler> logger, IPluginService pluginService, 
        IPluginConfigurationService pluginConfigurationService, ICurrentUserService currentUserService, 
        IPluginSpecificationsService pluginSpecificationsService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginSpecificationsService);
        _logger = logger;
        _pluginService = pluginService;
        _pluginConfigurationService = pluginConfigurationService;
        _currentUserService = currentUserService;
        _pluginSpecificationsService = pluginSpecificationsService;
    }

    public async Task<Result<AddPluginConfigResponse>> Handle(AddPluginConfigRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAthenticationIsRequired, "Access is denied. Authentication is required.");

            var isTypeExist = await _pluginService.IsExist(request.Type, cancellationToken);
            if (!isTypeExist)
            {
                var typeNotExistMessage = string.Format(Resources.AddConfigValidatorTypeValueIsNotValid, request.Name);
                _logger.LogWarning(typeNotExistMessage);
                return await Result<AddPluginConfigResponse>.FailAsync(typeNotExistMessage);
            }
            var isPluginSpecificationsValid = await _pluginSpecificationsService.Validate(request.Type, request.Specifications, cancellationToken);
            if (!isPluginSpecificationsValid.Valid)
            {
                _logger.LogWarning(isPluginSpecificationsValid.Message);
                return await Result<AddPluginConfigResponse>.FailAsync(isPluginSpecificationsValid.Message ?? "");
            }

            var pluginConfiguration = new PluginConfigurationEntity
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId,
                Name = request.Name,
                Type = request.Type,
                Specifications = request.Specifications.ToPluginConfigurationSpecifications(),
            };

            var ispluginConfigurationExist = await _pluginConfigurationService.IsExist(_currentUserService.UserId, request.Name, cancellationToken);
            if (ispluginConfigurationExist)
            {
                var pluginConfigurationNotExistMessage = string.Format(Resources.AddConfigHandlerItemIsAlreadyExist, request.Name);
                _logger.LogWarning(pluginConfigurationNotExistMessage);
                return await Result<AddPluginConfigResponse>.FailAsync(pluginConfigurationNotExistMessage);
            }

            await _pluginConfigurationService.Add(pluginConfiguration, cancellationToken);
            var response = new AddPluginConfigResponse 
            { 
                Id = pluginConfiguration.Id, 
                Name = pluginConfiguration.Name 
            };

            return await Result<AddPluginConfigResponse>.SuccessAsync(response, Resources.AddConfigHandlerSuccessfullyAdded);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<AddPluginConfigResponse>.FailAsync(ex.ToString());
        }
    }
}