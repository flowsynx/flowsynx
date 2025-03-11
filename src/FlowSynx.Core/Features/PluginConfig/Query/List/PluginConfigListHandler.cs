using FlowSynx.Core.Services;
using FlowSynx.Core.Wrapper;
using FlowSynx.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Features.PluginConfig.Query.List;

internal class PluginConfigListHandler : IRequestHandler<PluginConfigListRequest, Result<IEnumerable<PluginConfigListResponse>>>
{
    private readonly ILogger<PluginConfigListHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ICurrentUserService _currentUserService;

    public PluginConfigListHandler(ILogger<PluginConfigListHandler> logger, 
        IPluginConfigurationService pluginConfigurationService, ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IEnumerable<PluginConfigListResponse>>> Handle(PluginConfigListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var pluginConfigs = await _pluginConfigurationService.All(_currentUserService.UserId, cancellationToken);
            var response = pluginConfigs.Select(config => new PluginConfigListResponse
            {
                Id = config.Id,
                Name = config.Name,
                Type = config.Type,
                ModifiedTime = config.LastModifiedOn
            });
            _logger.LogInformation("Plugin Config List is got successfully.");
            return await Result<IEnumerable<PluginConfigListResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<PluginConfigListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}