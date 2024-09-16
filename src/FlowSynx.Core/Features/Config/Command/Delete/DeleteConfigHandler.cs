using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Configuration;
using FlowSynx.Configuration.Options;
using FlowSynx.Core.Features.List.Query;

namespace FlowSynx.Core.Features.Config.Command.Delete;

internal class DeleteConfigHandler : IRequestHandler<DeleteConfigRequest, Result<IEnumerable<DeleteConfigResponse>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IConfigurationManager _configurationManager;

    public DeleteConfigHandler(ILogger<ListHandler> logger, IConfigurationManager configurationManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        _logger = logger;
        _configurationManager = configurationManager;
    }

    public async Task<Result<IEnumerable<DeleteConfigResponse>>> Handle(DeleteConfigRequest request, CancellationToken cancellationToken)
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

            var result = _configurationManager.Delete(listOptions);
            var response = result.Select(c=> new DeleteConfigResponse
            {
                Id = c.Id
            });

            return await Result<IEnumerable<DeleteConfigResponse>>.SuccessAsync(response, Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<DeleteConfigResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}