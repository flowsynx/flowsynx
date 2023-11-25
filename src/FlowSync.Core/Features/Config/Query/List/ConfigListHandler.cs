using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using FlowSync.Core.Configuration;

namespace FlowSync.Core.Features.Config.Query.List;

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

            var response = result.Select(x => new ConfigListResponse
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