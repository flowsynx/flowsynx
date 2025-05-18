using FlowSynx.Application.Localizations;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.PluginConfig.Query.PluginConfigList;

internal class PluginConfigListHandler : IRequestHandler<PluginConfigListRequest, Result<IEnumerable<PluginConfigListResponse>>>
{
    private readonly ILogger<PluginConfigListHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public PluginConfigListHandler(
        ILogger<PluginConfigListHandler> logger, 
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

    public async Task<Result<IEnumerable<PluginConfigListResponse>>> Handle(PluginConfigListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var pluginConfigs = await _pluginConfigurationService.All(_currentUserService.UserId, cancellationToken);
            var response = pluginConfigs.Select(config => new PluginConfigListResponse
            {
                Id = config.Id,
                Name = config.Name,
                Type = config.Type,
                Version = config.Version,
                ModifiedTime = config.LastModifiedOn
            });
            _logger.LogInformation(_localization.Get("Feature_PluginConfig_ListRetrievedSuccessfully"));
            return await Result<IEnumerable<PluginConfigListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<PluginConfigListResponse>>.FailAsync(ex.ToString());
        }
    }
}