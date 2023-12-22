using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Abstractions;
using FlowSynx.Configuration;

namespace FlowSynx.Core.Features.Config.Query.List;

internal class ConfigListHandler : IRequestHandler<ConfigListRequest, Result<IEnumerable<ConfigListResponse>>>
{
    private readonly ILogger<ConfigListHandler> _logger;
    private readonly IConfigurationManager _configurationManager;

    public ConfigListHandler(ILogger<ConfigListHandler> logger, IConfigurationManager configurationManager)
    {
        _logger = logger;
        _configurationManager = configurationManager;
    }

    public async Task<Result<IEnumerable<ConfigListResponse>>> Handle(ConfigListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = _configurationManager.GetSettings();
            if (!string.IsNullOrEmpty(request.Type))
                result = result.Where(x => string.Equals(x.Type, request.Type, StringComparison.InvariantCultureIgnoreCase));

            var configurationItems = result as ConfigurationItem[] ?? result.ToArray();
            if (!configurationItems.Any())
            {
                _logger.LogWarning("No config item found!");
                return await Result<IEnumerable<ConfigListResponse>>.FailAsync("No config item found!");
            }

            var response = configurationItems.Select(x => new ConfigListResponse
            {
                Id = x.Id,
                Name = x.Name,
                Type = x.Type
            });

            return await Result<IEnumerable<ConfigListResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<ConfigListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}