using FlowSynx.Application.Extensions;
using FlowSynx.Application.Features.PluginConfig.Command.Add;
using FlowSynx.Application.Models;
using FlowSynx.Application.PluginHost;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Plugin;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.PluginConfig.Command.Update;

internal class UpdatePluginConfigHandler : IRequestHandler<UpdatePluginConfigRequest, Result<Unit>>
{
    private readonly ILogger<UpdatePluginConfigHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginService _pluginService;
    private readonly IPluginSpecificationsService _pluginSpecificationsService;

    public UpdatePluginConfigHandler(ILogger<UpdatePluginConfigHandler> logger, ICurrentUserService currentUserService,
        IPluginConfigurationService pluginConfigurationService, IPluginService pluginService, 
        IPluginSpecificationsService pluginSpecificationsService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        _logger = logger;
        _currentUserService = currentUserService;
        _pluginConfigurationService = pluginConfigurationService;
        _pluginService = pluginService;
        _pluginSpecificationsService = pluginSpecificationsService;
    }

    public async Task<Result<Unit>> Handle(UpdatePluginConfigRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAthenticationIsRequired, Resources.Authentication_Access_Denied);

            var configId = Guid.Parse(request.Id);
            var pluginConfiguration = await _pluginConfigurationService.Get(_currentUserService.UserId, configId, cancellationToken);
            if (pluginConfiguration == null)
            {
                var message = string.Format(Resources.Feature_PluginConfig_Update_ConfigIdNotFound, request.Id);
                throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, message);
            }

            if (!string.Equals(request.Name, pluginConfiguration.Name, StringComparison.OrdinalIgnoreCase))
            {
                var ispluginConfigExist = await _pluginConfigurationService.IsExist(_currentUserService.UserId, request.Name, cancellationToken);
                if (ispluginConfigExist)
                {
                    var message = string.Format(Resources.Features_PluginConfig_Update_PluginConfigAlreadyExists, request.Name);
                    var exception = new FlowSynxException((int)ErrorCode.PluginConfigurationIsAlreadyExist, message);
                    _logger.LogWarning(exception.ToString());
                    throw exception;
                }
            }

            var isTypeExist = await _pluginService.IsExist(_currentUserService.UserId, request.Type, request.Version, cancellationToken);
            if (!isTypeExist)
            {
                var message = string.Format(Resources.Features_PluginConfig_Update_PluginCouldNotBeFound, request.Type, request.Version);
                throw new FlowSynxException((int)ErrorCode.PluginTypeNotFound, message);
            }

            var pluginEntity = await _pluginService.Get(_currentUserService.UserId, request.Type, request.Version, cancellationToken);
            var isPluginSpecificationsValid = _pluginSpecificationsService.Validate(request.Specifications, pluginEntity?.Specifications);
            if (!isPluginSpecificationsValid.Valid)
                return await Result<Unit>.FailAsync(isPluginSpecificationsValid.Messages!);

            pluginConfiguration.Name = request.Name;
            pluginConfiguration.Type = request.Type;
            pluginConfiguration.Version = request.Version;
            pluginConfiguration.Specifications = request.Specifications.ToPluginConfigurationSpecifications();

            await _pluginConfigurationService.Update(pluginConfiguration, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.Feature_PluginConfig_Update_UpdatedSuccessfully);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}