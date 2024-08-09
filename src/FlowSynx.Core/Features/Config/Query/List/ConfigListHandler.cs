using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Abstractions;
using FlowSynx.Configuration;
using FlowSynx.Configuration.Options;

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
            var searchOptions = new ConfigurationSearchOptions()
            {
                Include = request.Include,
                Exclude = request.Exclude,
                MinimumAge = request.MinAge,
                MaximumAge = request.MaxAge,
                CaseSensitive = request.CaseSensitive ?? false
            };

            var listOptions = new ConfigurationListOptions()
            {
                Sorting = request.Sorting,
                MaxResult = request.MaxResults
            };

            var result = _configurationManager.List(searchOptions, listOptions);
            var response = result.Select(x => new ConfigListResponse
            {
                Id = x.Id,
                Name = x.Name,
                Type = x.Type,
                ModifiedTime = x.ModifiedTime,
            });

            return await Result<IEnumerable<ConfigListResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<ConfigListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}