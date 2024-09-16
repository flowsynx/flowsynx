using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Abstractions;
using FlowSynx.Configuration;
using FlowSynx.Configuration.Options;

namespace FlowSynx.Core.Features.Config.Query.List;

internal class ConfigListHandler : IRequestHandler<ConfigListRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<ConfigListHandler> _logger;
    private readonly IConfigurationManager _configurationManager;

    public ConfigListHandler(ILogger<ConfigListHandler> logger, IConfigurationManager configurationManager)
    {
        _logger = logger;
        _configurationManager = configurationManager;
    }

    public async Task<Result<IEnumerable<object>>> Handle(ConfigListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var listOptions = new ConfigurationListOptions
            {
                Fields = request.Fields,
                CaseSensitive = request.CaseSensitive,
                Filter = request.Filter,
                Sort = request.Sort,
                Limit = request.Limit
            };

            var response = _configurationManager.List(listOptions);
            return await Result<IEnumerable<object>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}