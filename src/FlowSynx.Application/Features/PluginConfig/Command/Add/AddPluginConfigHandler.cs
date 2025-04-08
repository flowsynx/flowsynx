using FlowSynx.Application.Extensions;
using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Plugin;
using FlowSynx.Domain.PluginConfig;
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

            var isTypeExist = await _pluginService.IsExist(_currentUserService.UserId, request.Type, request.Version, cancellationToken);
            if (!isTypeExist)
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.PluginTypeNotFound, $"The plugin type '{request.Type}' with version '{request.Version}' is not exist.");
                _logger.LogError(errorMessage.ToString());
                return await Result<AddPluginConfigResponse>.FailAsync(errorMessage.ToString());
            }

            var pluginEntity = await _pluginService.Get(_currentUserService.UserId, request.Type, request.Version, cancellationToken);
            var isPluginSpecificationsValid = _pluginSpecificationsService.Validate(request.Specifications, pluginEntity.Specifications);
            if (!isPluginSpecificationsValid.Valid)
            {
                return await Result<AddPluginConfigResponse>.FailAsync(isPluginSpecificationsValid.Messages);
            }

            var pluginConfiguration = new PluginConfigurationEntity
            {
                Id = Guid.NewGuid(),
                PluginId = pluginEntity.Id,
                UserId = _currentUserService.UserId,
                Name = request.Name,
                Type = request.Type,
                Version = request.Version,
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