using FlowSynx.Application.Extensions;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Interfaces;
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
            var configId = Guid.Parse(request.Id);
            var pluginConfiguration = await _pluginConfigurationService.Get(_currentUserService.UserId, configId, cancellationToken);
            if (pluginConfiguration == null)
                throw new Exception("The config not found");

            var isTypeExist = await _pluginService.IsExist(request.Type, cancellationToken);
            if (!isTypeExist)
            {
                var typeNotExistMessage = string.Format(Resources.AddConfigValidatorTypeValueIsNotValid, request.Name);
                _logger.LogWarning(typeNotExistMessage);
                return await Result<Unit>.FailAsync(typeNotExistMessage);
            }
            var isPluginSpecificationsValid = await _pluginSpecificationsService.Validate(request.Type, request.Specifications, cancellationToken);
            if (!isPluginSpecificationsValid.Valid)
            {
                _logger.LogWarning(isPluginSpecificationsValid.Message);
                return await Result<Unit>.FailAsync(isPluginSpecificationsValid.Message ?? "");
            }

            pluginConfiguration.Name = request.Name;
            pluginConfiguration.Type = request.Type;
            pluginConfiguration.Specifications = request.Specifications.ToPluginConfigurationSpecifications();

            await _pluginConfigurationService.Update(pluginConfiguration, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}