using FlowSynx.Core.Features.PluginConfig.Query.List;
using FlowSynx.Core.Wrapper;
using FlowSynx.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Features.Config.Query.List;

internal class PluginConfigListHandler : IRequestHandler<PluginConfigListRequest, Result<IEnumerable<PluginConfigListResponse>>>
{
    private readonly ILogger<PluginConfigListHandler> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;

    public PluginConfigListHandler(ILogger<PluginConfigListHandler> logger, IPluginConfigurationService pluginConfigurationService)
    {
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
    }

    public async Task<Result<IEnumerable<PluginConfigListResponse>>> Handle(PluginConfigListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId))
                ArgumentNullException.ThrowIfNull(request.UserId);

            var pluginConfigs = await _pluginConfigurationService.All(request.UserId, cancellationToken);
            var response = pluginConfigs.Select(config => new PluginConfigListResponse
            {
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