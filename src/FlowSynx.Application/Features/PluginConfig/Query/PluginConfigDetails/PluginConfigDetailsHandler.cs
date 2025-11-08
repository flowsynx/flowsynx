using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.PluginConfig.Query.PluginConfigDetails;

internal class PluginConfigDetailsHandler : IRequestHandler<PluginConfigDetailsRequest, Result<PluginConfigDetailsResponse>>
{
    private readonly ILogger<PluginConfigDetailsHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public PluginConfigDetailsHandler(
        ILogger<PluginConfigDetailsHandler> logger, 
        IPluginConfigurationService pluginConfigurationService, 
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<PluginConfigDetailsResponse>> Handle(PluginConfigDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var configId = Guid.Parse(request.ConfigId);
            var pluginConfig = await _pluginConfigurationService.Get(_currentUserService.UserId(), configId, cancellationToken);
            if (pluginConfig is null)
            {
                var message = _localization.Get("Feature_PluginConfig_DetailsNotFound", configId);
                throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, message);
            }

            var response = new PluginConfigDetailsResponse
            {
                Id = pluginConfig.Id,
                Name = pluginConfig.Name,
                Type = pluginConfig.Type,
                Version = pluginConfig.Version,
                Specifications = pluginConfig.Specifications,
            };
            _logger.LogInformation(_localization.Get("Feature_PluginConfig_DetailesRetrievedSuccessfully", configId));
            return await Result<PluginConfigDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<PluginConfigDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}
